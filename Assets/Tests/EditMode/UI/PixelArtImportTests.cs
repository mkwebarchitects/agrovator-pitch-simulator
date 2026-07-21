using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    public sealed class PixelArtImportTests
    {
        private static readonly string[] JudgeNames =
        {
            "Idle", "Blink", "Talk", "Think", "Smile", "Interested", "Confused",
            "Concerned", "Impressed", "Encouraging", "Celebrating",
        };

        private static readonly string[] ConfidenceNames =
        {
            "Getting Started", "Listening", "Curious", "Interested", "Convinced",
        };

        [TestCase("Assets/Art/Characters/judge-aya-sheet.png", 1408, 160, 11)]
        [TestCase("Assets/Art/Environment/pitch-room.png", 1280, 720, 1)]
        [TestCase("Assets/Art/UI/dialogue-panel.png", 768, 384, 1)]
        [TestCase("Assets/Art/UI/confidence-icons.png", 480, 96, 5)]
        public void PixelArt_UsesCrispDeterministicImportSettings(
            string path,
            int expectedWidth,
            int expectedHeight,
            int expectedSpriteCount)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f));

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(expectedWidth));
            Assert.That(texture.height, Is.EqualTo(expectedHeight));

            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            Assert.That(sprites, Has.Length.EqualTo(expectedSpriteCount));
        }

        // The default-platform assertion above is not what ships. A WebGL build
        // resolves each texture through its WebGL platform entry, so assert the
        // format Unity will actually bake rather than the default-platform flag.
        [TestCase("Assets/Art/Characters/judge-aya-sheet.png")]
        [TestCase("Assets/Art/Environment/pitch-room.png")]
        [TestCase("Assets/Art/UI/dialogue-panel.png")]
        [TestCase("Assets/Art/UI/confidence-icons.png")]
        public void PixelArt_ShipsUncompressedOnWebGLAndStandalone(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            foreach (var platform in new[] { "WebGL", "Standalone" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);

                // A non-overridden entry inherits the default platform, so its
                // stored textureCompression value is inert. Only an *enabled*
                // override can reintroduce block compression.
                if (settings.overridden)
                {
                    Assert.That(settings.textureCompression,
                        Is.EqualTo(TextureImporterCompression.Uncompressed),
                        $"{path} overrides {platform} with block compression.");
                    Assert.That(settings.crunchedCompression, Is.False, $"{path} / {platform}");
                }

                var format = importer.GetAutomaticFormat(platform);
                Assert.That(format,
                    Is.EqualTo(TextureImporterFormat.RGBA32).Or.EqualTo(TextureImporterFormat.RGB24),
                    $"{path} bakes to {format} on {platform}; flat-colour art with hard " +
                    "edges must stay uncompressed or it fringes.");
            }
        }

        [Test]
        public void MultiSpriteSheets_HaveExactNamedEqualCells()
        {
            AssertSheet("Assets/Art/Characters/judge-aya-sheet.png", JudgeNames, 128f, 160f);
            AssertSheet("Assets/Art/UI/confidence-icons.png", ConfidenceNames, 96f, 96f);
        }

        [Test]
        public void DialoguePanel_HasNineSliceBorder()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(
                "Assets/Art/UI/dialogue-panel.png");
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.spriteBorder, Is.EqualTo(new Vector4(24f, 24f, 24f, 24f)));
        }

        [Test]
        public void AuthoredReactionCues_AllResolveToExpectedSemanticStates()
        {
            // Read from the guided content the learner actually plays; the
            // retired dialogue scenario it used to read is gone.
            var json = File.ReadAllText("Assets/Content/Scenarios/guided-pitch-builder.en.json");
            var cues = Regex.Matches(json, "\\\"ReactionCue\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            Assert.That(cues, Is.EqualTo(new[]
            {
                "Concerned", "Curious", "Impressed",
            }), "Encouraging is Aya's resting face rather than an authored cue.");

            var expected = new Dictionary<string, JudgeReaction>
            {
                ["Concerned"] = JudgeReaction.Concerned,
                ["Curious"] = JudgeReaction.Interested,
                ["Impressed"] = JudgeReaction.Impressed,
            };
            foreach (var cue in cues)
            {
                Assert.That(JudgeReactionMapper.Parse(cue), Is.EqualTo(expected[cue]), cue);
            }
            Assert.That(JudgeReactionMapper.Parse("truly-unknown"),
                Is.EqualTo(JudgeReaction.Encouraging));
        }

        [Test]
        public void ImporterSource_UsesModernSpriteDataProviderApiWithoutObsoleteSheetAccess()
        {
            var source = File.ReadAllText(
                "Assets/Scripts/UI/Editor/PixelArtImportPostprocessor.cs");
            Assert.That(source, Does.Contain("ISpriteEditorDataProvider"));
            Assert.That(source, Does.Contain("SpriteRect"));
            Assert.That(source, Does.Not.Contain(".spritesheet"));
            Assert.That(source, Does.Not.Contain("SpriteMetaData"));
        }

        [Test]
        public void CleanReimport_ReconstructsSheetsAndPreservesExactMetaBytes()
        {
            var paths = new[]
            {
                "Assets/Art/Characters/judge-aya-sheet.png",
                "Assets/Art/UI/confidence-icons.png",
            };
            var snapshots = paths.ToDictionary(
                path => path + ".meta",
                path => File.ReadAllBytes(path + ".meta"));
            try
            {
                foreach (var path in paths)
                {
                    AssetDatabase.ImportAsset(path,
                        ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }

                AssertSheet(paths[0], JudgeNames, 128f, 160f);
                AssertSheet(paths[1], ConfidenceNames, 96f, 96f);
            }
            finally
            {
                foreach (var pair in snapshots)
                {
                    File.WriteAllBytes(pair.Key, pair.Value);
                    var restored = File.ReadAllBytes(pair.Key);
                    Assert.That(restored, Is.EqualTo(pair.Value), pair.Key);
                    Assert.That(Sha256(restored), Is.EqualTo(Sha256(pair.Value)), pair.Key);
                }
            }
        }

        private static byte[] Sha256(byte[] value)
        {
            using (var hash = SHA256.Create())
            {
                return hash.ComputeHash(value);
            }
        }

        private static void AssertSheet(string path, string[] expectedNames, float width, float height)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(sprite => sprite.rect.x)
                .ToArray();
            Assert.That(sprites.Select(sprite => sprite.name), Is.EqualTo(expectedNames));
            Assert.That(sprites.All(sprite => Mathf.Approximately(sprite.rect.width, width)), Is.True);
            Assert.That(sprites.All(sprite => Mathf.Approximately(sprite.rect.height, height)), Is.True);
        }
    }
}
