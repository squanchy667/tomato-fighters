using System.Collections.Generic;
using System.Linq;
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

        // ── Canonical State Registry ──
        // All state names the base controller must have. Used for validation and override mapping.

        /// <summary>Locomotion states driven by Speed float parameter.</summary>
        public static readonly HashSet<string> LocomotionStates = new HashSet<string> { "idle", "walk", "run" };

        /// <summary>Airborne states driven by IsGrounded bool parameter.</summary>
        public static readonly HashSet<string> AirborneStates = new HashSet<string> { "jump", "land" };

        /// <summary>Generic action states (trigger-driven, non-attack).</summary>
        public static readonly HashSet<string> ActionStates = new HashSet<string> { "dash" };

        /// <summary>10 attack slots on the base controller (attack_1 through attack_10).</summary>
        public static readonly string[] AttackSlots = Enumerable.Range(1, 10).Select(i => $"attack_{i}").ToArray();

        /// <summary>Defense states on the base controller.</summary>
        public static readonly HashSet<string> DefenseStates = new HashSet<string> { "block", "guard" };

        /// <summary>Reaction states on the base controller.</summary>
        public static readonly HashSet<string> ReactionStates = new HashSet<string> { "hurt", "death" };

        /// <summary>All canonical states that appear on the base controller (attacks + defense + reaction + actions).</summary>
        public static readonly HashSet<string> AllCanonicalStates;

        /// <summary>
        /// Per-character mapping of canonical attack slot name to AttackData asset name.
        /// Used by AnimationBuilder (clip mapping) and AnimationEventStamper (event timing).
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, string>> AttackSlotMappings = new Dictionary<string, Dictionary<string, string>>
        {
            ["Brutor"] = new Dictionary<string, string>
            {
                ["attack_1"] = "BrutorShieldBash1",
                ["attack_2"] = "BrutorShieldBash2",
                ["attack_3"] = "BrutorSweep",
                ["attack_4"] = "BrutorLauncher",
                ["attack_5"] = "BrutorLauncherSlam",
                ["attack_6"] = "BrutorOverheadSlam",
                ["attack_7"] = "BrutorGroundPound",
            },
            ["Slasher"] = new Dictionary<string, string>
            {
                ["attack_1"] = "SlasherSlash1",
                ["attack_2"] = "SlasherSlash2",
                ["attack_3"] = "SlasherSlash3",
                ["attack_4"] = "SlasherSpinFinisher",
                ["attack_5"] = "SlasherHeavySlash",
                ["attack_6"] = "SlasherLunge",
                ["attack_7"] = "SlasherLungeFinisher",
                ["attack_8"] = "SlasherQuickSlash",
            },
            ["Mystica"] = new Dictionary<string, string>
            {
                ["attack_1"] = "MysticaStrike1",
                ["attack_2"] = "MysticaStrike2",
                ["attack_3"] = "MysticaStrike3",
                ["attack_4"] = "MysticaArcaneBolt",
                ["attack_5"] = "MysticaEmpoweredBolt",
            },
            ["Viper"] = new Dictionary<string, string>
            {
                ["attack_1"] = "ViperShot1",
                ["attack_2"] = "ViperShot2",
                ["attack_3"] = "ViperRapidBurst",
                ["attack_4"] = "ViperChargedShot",
                ["attack_5"] = "ViperPiercingShot",
                ["attack_6"] = "ViperQuickCharged",
            },
        };

        static AnimationForgeMetadata()
        {
            AllCanonicalStates = new HashSet<string>(LocomotionStates);
            AllCanonicalStates.UnionWith(AirborneStates);
            AllCanonicalStates.UnionWith(ActionStates);
            foreach (var slot in AttackSlots) AllCanonicalStates.Add(slot);
            AllCanonicalStates.UnionWith(DefenseStates);
            AllCanonicalStates.UnionWith(ReactionStates);
        }

        /// <summary>
        /// Checks if an animation name from metadata.json maps to any recognized state.
        /// Returns false for names that don't match any canonical state (validation ERROR candidates).
        /// </summary>
        public static bool IsRecognizedAnimationName(string animName)
        {
            return AllCanonicalStates.Contains(animName);
        }

        /// <summary>
        /// Validates a character's metadata animations against canonical states.
        /// Logs ERROR for unmapped metadata animations and WARNING for missing canonical states.
        /// Returns the set of canonical states found in the metadata.
        /// </summary>
        public static HashSet<string> ValidateMetadata(string characterName, MetadataRoot metadata)
        {
            var foundCanonical = new HashSet<string>();

            foreach (var animName in metadata.animations.Keys)
            {
                if (IsRecognizedAnimationName(animName))
                {
                    foundCanonical.Add(animName);
                }
                else
                {
                    Debug.LogError($"[AnimationForgeMetadata] {characterName}: metadata animation '{animName}' " +
                                   "does not map to any canonical state. Check naming or add mapping.");
                }
            }

            // Check which canonical attack/defense/reaction states are missing
            var requiredActionStates = new List<string>();
            requiredActionStates.AddRange(ActionStates);
            if (AttackSlotMappings.ContainsKey(characterName))
            {
                requiredActionStates.AddRange(AttackSlotMappings[characterName].Keys);
            }
            requiredActionStates.AddRange(DefenseStates);
            requiredActionStates.AddRange(ReactionStates);

            foreach (var state in requiredActionStates)
            {
                if (!foundCanonical.Contains(state))
                {
                    Debug.LogWarning($"[AnimationForgeMetadata] {characterName}: canonical state '{state}' " +
                                     "has no matching animation. Placeholder will be generated.");
                }
            }

            return foundCanonical;
        }

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
            },
            ["Brutor"] = new CharacterAnimConfig
            {
                sourceFolder = "Assets/animations/red_brute_animations",
                outputFolder = "Assets/Animations/Brutor"
            },
            ["Viper"] = new CharacterAnimConfig
            {
                sourceFolder = "Assets/animations/yellow_ranger_animations",
                outputFolder = "Assets/Animations/Viper"
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
