using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Agrovator.PitchSimulator.UI.Editor
{
    public sealed class PixelArtImportPostprocessor : AssetPostprocessor
    {
        internal const string JudgePath = "Assets/Art/Characters/judge-aya-sheet.png";
        internal const string EnvironmentPath = "Assets/Art/Environment/pitch-room.png";
        internal const string DialoguePanelPath = "Assets/Art/UI/dialogue-panel.png";
        internal const string ConfidencePath = "Assets/Art/UI/confidence-icons.png";
        internal const string PartIconPath = "Assets/Art/UI/part-icons.png";

        private static readonly string[] JudgeNames =
        {
            "Idle", "Blink", "Talk", "Think", "Smile", "Interested", "Confused",
            "Concerned", "Impressed", "Encouraging", "Celebrating",
        };

        // Cell names match PitchPart so the builder can resolve an icon by part
        // rather than by index, which no rename can silently reorder.
        private static readonly string[] PartIconNames =
        {
            "Problem", "Evidence", "Solution", "Value",
        };

        private static readonly string[] ConfidenceNames =
        {
            "Getting Started", "Listening", "Curious", "Interested", "Convinced",
        };

        // Bump whenever OnPreprocessTexture changes what it writes. Unity only
        // reruns a postprocessor over already-imported assets when this changes,
        // so skipping the bump leaves the committed .meta files stale against the
        // policy below - the same trap PitchSimulatorProjectBuilder.GeneratorVersion
        // guards for generated scenes.
        public override uint GetVersion()
        {
            return 3;
        }

        private void OnPreprocessTexture()
        {
            if (!IsManaged(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            // pitch-room.png is a 1280x720 illustration displayed at roughly 1:1,
            // not low-resolution pixel art. Point sampling gains it nothing there
            // and drops or duplicates source pixels at any non-integer scale. The
            // genuine sprite sheets keep exact sampling.
            importer.filterMode = assetPath == EnvironmentPath
                ? FilterMode.Bilinear
                : FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = assetPath != EnvironmentPath;

            if (assetPath == JudgePath)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                ConfigureEqualCells(importer, 1408, 160, JudgeNames);
            }
            else if (assetPath == ConfidencePath)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                ConfigureEqualCells(importer, 480, 96, ConfidenceNames);
            }
            else if (assetPath == PartIconPath)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                ConfigureEqualCells(importer, 384, 96, PartIconNames);
            }
            else
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = assetPath == DialoguePanelPath
                    ? new Vector4(24f, 24f, 24f, 24f)
                    : Vector4.zero;
            }
        }

        private static bool IsManaged(string path)
        {
            return path == JudgePath || path == EnvironmentPath ||
                path == DialoguePanelPath || path == ConfidencePath ||
                path == PartIconPath;
        }

        private static void ConfigureEqualCells(
            TextureImporter importer,
            int width,
            int height,
            string[] names)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existingIds = provider.GetSpriteRects()
                .GroupBy(sprite => sprite.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID);
            var cellWidth = width / names.Length;
            var cells = new SpriteRect[names.Length];
            for (var index = 0; index < names.Length; index++)
            {
                cells[index] = new SpriteRect
                {
                    name = names[index],
                    rect = new Rect(index * cellWidth, 0f, cellWidth, height),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                    spriteID = existingIds.TryGetValue(names[index], out var existingId)
                        ? existingId
                        : GUID.Generate(),
                };
            }
            provider.SetSpriteRects(cells);
            var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider.SetNameFileIdPairs(cells.Select(
                cell => new SpriteNameFileIdPair(cell.name, cell.spriteID)));
            provider.Apply();
        }
    }
}
