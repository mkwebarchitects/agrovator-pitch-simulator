using System.Linq;
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
