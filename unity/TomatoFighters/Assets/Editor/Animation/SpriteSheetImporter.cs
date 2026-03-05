using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Animation
{
    /// <summary>
    /// <b>Animation Pipeline — Step 1 of 2 (Editor).</b>
    /// Reads Animation Forge metadata.json and auto-configures sprite sheet import settings
    /// for ALL animations found — no hardcoded animation names.
    /// Run via menu: <b>TomatoFighters &gt; Import Sprite Sheets</b>.
    ///
    /// <para><b>What it does for each animation in metadata.json:</b></para>
    /// <list type="number">
    ///   <item>Locates the PNG at <c>Sprites/{character}_{animName}.png</c></item>
    ///   <item>Sets TextureImporter to: Sprite Mode = Multiple, PPU from metadata,
    ///     uncompressed, max 8192px</item>
    ///   <item>Grid-slices into individual frames using frame_w/frame_h/cols/rows,
    ///     with custom pivot (typically bottom-center)</item>
    ///   <item>Names frames with zero-padded indices (<c>idle_00</c>, <c>idle_01</c>, ...)
    ///     so lexicographic sort matches frame order</item>
    /// </list>
    ///
    /// <para><b>Frame layout:</b> Left-to-right, top-to-bottom in the sheet. Unity sprite
    /// rects have origin at bottom-left, so Y is flipped:
    /// <c>Y = texHeight - (row + 1) * frameHeight</c>.</para>
    ///
    /// <para><b>Run this BEFORE</b> <see cref="AnimationBuilder"/> (Step 2), which needs
    /// the sliced sprites as input.</para>
    ///
    /// <para><b>Adding new animations:</b> Just drop the PNG into the Sprites folder and add
    /// the entry to metadata.json. This script picks it up automatically.</para>
    ///
    /// <para><b>Note:</b> Uses the deprecated <c>TextureImporter.spritesheet</c> API because
    /// <c>ISpriteEditorDataProvider</c> requires an assembly not exposed in Unity 6's
    /// built-in 2D Sprite module.</para>
    /// </summary>
    /// <seealso cref="AnimationForgeMetadata"/>
    /// <seealso cref="AnimationBuilder"/>
    public static class SpriteSheetImporter
    {
        [MenuItem("TomatoFighters/Import Sprite Sheets/All Characters")]
        public static void ImportAllSpriteSheets()
        {
            foreach (var kvp in AnimationForgeMetadata.Characters)
            {
                Debug.Log($"[SpriteSheetImporter] Importing {kvp.Key}...");
                ImportSpriteSheets(kvp.Value.sourceFolder);
            }

            foreach (var kvp in AnimationForgeMetadata.EnemyCharacters)
            {
                if (string.IsNullOrEmpty(kvp.Value.sourceFolder)) continue; // TestDummy has no source
                Debug.Log($"[SpriteSheetImporter] Importing enemy {kvp.Key}...");
                ImportSpriteSheets(kvp.Value.sourceFolder);
            }
        }

        [MenuItem("TomatoFighters/Import Sprite Sheets/Mystica")]
        public static void ImportMysticaSpriteSheets()
        {
            ImportSpriteSheets(AnimationForgeMetadata.Characters["Mystica"].sourceFolder);
        }

        [MenuItem("TomatoFighters/Import Sprite Sheets/Slasher")]
        public static void ImportSlasherSpriteSheets()
        {
            ImportSpriteSheets(AnimationForgeMetadata.Characters["Slasher"].sourceFolder);
        }

        [MenuItem("TomatoFighters/Import Sprite Sheets/Brutor")]
        public static void ImportBrutorSpriteSheets()
        {
            ImportSpriteSheets(AnimationForgeMetadata.Characters["Brutor"].sourceFolder);
        }

        [MenuItem("TomatoFighters/Import Sprite Sheets/Viper")]
        public static void ImportViperSpriteSheets()
        {
            ImportSpriteSheets(AnimationForgeMetadata.Characters["Viper"].sourceFolder);
        }

        /// <summary>
        /// Imports sprite sheets from any source folder containing metadata.json + Sprites/.
        /// </summary>
        public static void ImportSpriteSheets(string sourceFolder)
        {
            var metadata = AnimationForgeMetadata.Load(sourceFolder);
            if (metadata == null) return;

            string spritesFolder = $"{sourceFolder}/Sprites";
            int count = 0;
            foreach (var kvp in metadata.animations)
            {
                string animName = kvp.Key;
                var entry = kvp.Value;

                string assetPath = AnimationForgeMetadata.GetSheetPath(spritesFolder, metadata.characterName, animName);
                if (ConfigureSpriteSheet(animName, entry, assetPath))
                    count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpriteSheetImporter] Done — configured {count}/{metadata.animations.Count} sprite sheets for '{metadata.characterName}'.");
        }

        /// <summary>
        /// Configures a single sprite sheet's TextureImporter and grid-slices it.
        /// Returns true on success.
        /// </summary>
        private static bool ConfigureSpriteSheet(string animName, AnimationForgeMetadata.AnimationEntry entry, string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[SpriteSheetImporter] Sprite sheet not found: {assetPath}");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = entry.ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;

            // Grid-slice into individual frames
            var spriteSheet = new SpriteMetaData[entry.frameCount];
            int frameIndex = 0;

            // Zero-pad frame indices so lexicographic sort matches frame order
            // (e.g., idle_02 before idle_10, not idle_10 before idle_2)
            int padWidth = entry.frameCount.ToString().Length;

            // Frames are laid out left-to-right, top-to-bottom in the sheet.
            // Unity sprite rects have origin at bottom-left.
            int texHeight = entry.rows * entry.frameHeight;

            for (int row = 0; row < entry.rows && frameIndex < entry.frameCount; row++)
            {
                for (int col = 0; col < entry.cols && frameIndex < entry.frameCount; col++)
                {
                    var smd = new SpriteMetaData();
                    smd.name = $"{animName}_{frameIndex.ToString().PadLeft(padWidth, '0')}";
                    smd.alignment = (int)SpriteAlignment.Custom;
                    smd.pivot = new Vector2(entry.pivot[0], entry.pivot[1]);
                    smd.rect = new Rect(
                        col * entry.frameWidth,
                        texHeight - (row + 1) * entry.frameHeight,
                        entry.frameWidth,
                        entry.frameHeight);
                    spriteSheet[frameIndex] = smd;
                    frameIndex++;
                }
            }

#pragma warning disable CS0618 // spritesheet is obsolete but ISpriteEditorDataProvider requires assembly ref not available in built-in 2D Sprite module
            importer.spritesheet = spriteSheet;
#pragma warning restore CS0618

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Debug.Log($"[SpriteSheetImporter] {animName}: {entry.frameCount} frames ({entry.cols}x{entry.rows}), {entry.frameWidth}x{entry.frameHeight}px, PPU={entry.ppu}");
            return true;
        }
    }
}
