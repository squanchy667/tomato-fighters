using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Animation
{
    /// <summary>
    /// Shared data model for Animation Forge's <c>metadata.json</c> export format.
    /// Used by <see cref="SpriteSheetImporter"/> and <see cref="AnimationBuilder"/>.
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
    /// <para><b>Convention:</b> <c>loop: true</c> = locomotion state (Speed-driven transitions).
    /// <c>loop: false</c> = action state (trigger-driven, returns to idle on exit).</para>
    /// </summary>
    public static class AnimationForgeMetadata
    {
        private const string METADATA_PATH = "Assets/animations/tomato_fighter_animations/metadata.json";
        public const string SPRITES_FOLDER = "Assets/animations/tomato_fighter_animations/Sprites";

        /// <summary>
        /// Loads and parses metadata.json, returning the character name and all animation entries.
        /// Returns null if the file is missing or unparseable.
        /// </summary>
        public static MetadataRoot Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(METADATA_PATH);
            if (asset == null)
            {
                Debug.LogError($"[AnimationForgeMetadata] metadata.json not found at {METADATA_PATH}");
                return null;
            }

            var root = JsonConvert.DeserializeObject<MetadataRoot>(asset.text);
            if (root == null || root.animations == null || root.animations.Count == 0)
            {
                Debug.LogError("[AnimationForgeMetadata] Failed to parse metadata.json or no animations found.");
                return null;
            }

            return root;
        }

        /// <summary>Returns the sprite sheet asset path for a given animation name.</summary>
        public static string GetSheetPath(string characterName, string animName)
        {
            return $"{SPRITES_FOLDER}/{characterName}_{animName}.png";
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
