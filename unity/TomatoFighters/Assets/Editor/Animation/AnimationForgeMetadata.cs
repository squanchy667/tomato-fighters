using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Animation
{
    /// <summary>
    /// Shared data model for Animation Forge's <c>metadata.json</c> export format.
    /// Used by <see cref="SpriteSheetImporter"/> (Step 1) and <see cref="AnimationBuilder"/> (Step 2).
    ///
    /// <para><b>Pipeline role:</b> Foundation layer — both editor tools deserialize through this
    /// class, so all animation data (frame dimensions, FPS, loop flag, pivot) lives in one place.</para>
    ///
    /// <para>Uses Newtonsoft.Json (via <c>com.unity.nuget.newtonsoft-json</c>) because
    /// Unity's <c>JsonUtility</c> doesn't support <c>Dictionary</c> deserialization,
    /// and we need to iterate over arbitrary animation names without hardcoding them.</para>
    ///
    /// <para><b>metadata.json format (from Animation Forge):</b></para>
    /// <code>
    /// {
    ///   "character_name": "tomato_fighter",
    ///   "animations": {
    ///     "idle":   { "frame_w": 464, "frame_h": 688, "cols": 8, "rows": 8, "n_frames": 59, "fps": 24, "loop": true,  "pivot": [0.5, 0.0], "ppu": 256 },
    ///     "walk":   { ... "loop": true },
    ///     "run":    { ... "loop": true },
    ///     "light1": { ... "loop": false }   ← action animations (future)
    ///   }
    /// }
    /// </code>
    ///
    /// <para><b>Key convention:</b> The <c>loop</c> field drives the entire wiring strategy:
    /// <c>true</c> = locomotion state (Speed-driven float transitions),
    /// <c>false</c> = action state (trigger-driven, returns to idle via Exit Time).</para>
    /// </summary>
    /// <seealso cref="SpriteSheetImporter"/>
    /// <seealso cref="AnimationBuilder"/>
    public static class AnimationForgeMetadata
    {
        private const string DEFAULT_SOURCE_FOLDER = "Assets/animations/tomato_fighter_animations";
        private const string METADATA_PATH = DEFAULT_SOURCE_FOLDER + "/metadata.json";
        public const string SPRITES_FOLDER = DEFAULT_SOURCE_FOLDER + "/Sprites";

        /// <summary>
        /// Per-character animation pipeline config: source folder (where metadata.json + Sprites live)
        /// and output folder (where .anim clips + controller are written).
        /// </summary>
        public struct CharacterAnimConfig
        {
            public string sourceFolder;
            public string outputFolder;
        }

        /// <summary>Registry of known character animation configs.</summary>
        public static readonly Dictionary<string, CharacterAnimConfig> Characters = new Dictionary<string, CharacterAnimConfig>
        {
            ["Mystica"] = new CharacterAnimConfig
            {
                sourceFolder = "Assets/animations/tomato_fighter_animations",
                outputFolder = "Assets/Animations/Mystica"
            },
            ["Slasher"] = new CharacterAnimConfig
            {
                sourceFolder = "Assets/animations/blue_warrior_animations",
                outputFolder = "Assets/Animations/Slasher"
            }
        };

        /// <summary>
        /// Loads metadata.json from the default source folder (tomato_fighter_animations).
        /// Returns null if the file is missing or unparseable.
        /// </summary>
        public static MetadataRoot Load()
        {
            return Load(DEFAULT_SOURCE_FOLDER);
        }

        /// <summary>
        /// Loads metadata.json from any source folder.
        /// Returns null if the file is missing or unparseable.
        /// </summary>
        public static MetadataRoot Load(string sourceFolder)
        {
            string metadataPath = $"{sourceFolder}/metadata.json";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(metadataPath);
            if (asset == null)
            {
                Debug.LogError($"[AnimationForgeMetadata] metadata.json not found at {metadataPath}");
                return null;
            }

            var root = JsonConvert.DeserializeObject<MetadataRoot>(asset.text);
            if (root == null || root.animations == null || root.animations.Count == 0)
            {
                Debug.LogError("[AnimationForgeMetadata] Failed to parse metadata.json or no animations found.");
                return null;
            }

            Debug.Log($"[AnimationForgeMetadata] Done — loaded '{root.characterName}' with {root.animations.Count} animations.");
            return root;
        }

        /// <summary>Returns the sprite sheet asset path using the default sprites folder.</summary>
        public static string GetSheetPath(string characterName, string animName)
        {
            return $"{SPRITES_FOLDER}/{characterName}_{animName}.png";
        }

        /// <summary>Returns the sprite sheet asset path for any source folder.</summary>
        public static string GetSheetPath(string spritesFolder, string characterName, string animName)
        {
            return $"{spritesFolder}/{characterName}_{animName}.png";
        }

        public class MetadataRoot
        {
            [JsonProperty("character_name")]
            public string characterName;

            [JsonProperty("animations")]
            public Dictionary<string, AnimationEntry> animations;
        }

        public class AnimationEntry
        {
            [JsonProperty("frame_w")]  public int frameWidth;
            [JsonProperty("frame_h")]  public int frameHeight;
            [JsonProperty("cols")]     public int cols;
            [JsonProperty("rows")]     public int rows;
            [JsonProperty("n_frames")] public int frameCount;
            [JsonProperty("fps")]      public int fps;
            [JsonProperty("loop")]     public bool loop;
            [JsonProperty("pivot")]    public float[] pivot;
            [JsonProperty("ppu")]      public int ppu;
        }
    }
}
