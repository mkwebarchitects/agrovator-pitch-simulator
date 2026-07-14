using UnityEditor;
using UnityEngine;

namespace Agrovator.PitchSimulator.UI.Editor
{
    public sealed class PixelArtImportPostprocessor : AssetPostprocessor
    {
        internal const string JudgePath = "Assets/Art/Characters/judge-aya-sheet.png";
        internal const string EnvironmentPath = "Assets/Art/Environment/pitch-room.png";
        internal const string DialoguePanelPath = "Assets/Art/UI/dialogue-panel.png";
        internal const string ConfidencePath = "Assets/Art/UI/confidence-icons.png";

        private static readonly string[] JudgeNames =
        {
            "Idle", "Blink", "Talk", "Think", "Smile", "Interested", "Confused",
            "Concerned", "Impressed", "Encouraging", "Celebrating",
        };

        private static readonly string[] ConfidenceNames =
        {
            "Getting Started", "Listening", "Curious", "Interested", "Convinced",
        };

        private void OnPreprocessTexture()
        {
            if (!IsManaged(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = assetPath != EnvironmentPath;

            if (assetPath == JudgePath)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritesheet = BuildEqualCells(1408, 160, JudgeNames);
            }
            else if (assetPath == ConfidencePath)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritesheet = BuildEqualCells(480, 96, ConfidenceNames);
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
                path == DialoguePanelPath || path == ConfidencePath;
        }

        private static SpriteMetaData[] BuildEqualCells(int width, int height, string[] names)
        {
            var cellWidth = width / names.Length;
            var cells = new SpriteMetaData[names.Length];
            for (var index = 0; index < names.Length; index++)
            {
                cells[index] = new SpriteMetaData
                {
                    name = names[index],
                    rect = new Rect(index * cellWidth, 0f, cellWidth, height),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                };
            }
            return cells;
        }
    }
}
