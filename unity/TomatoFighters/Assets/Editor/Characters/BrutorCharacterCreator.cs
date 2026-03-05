using TomatoFighters.Characters.Passives;
using TomatoFighters.Combat;
using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a complete Brutor player prefab: loads existing Brutor-specific
    /// ScriptableObjects, creates DefenseConfig, wires AttackData into ComboSteps,
    /// defines hitbox shapes, and delegates to <see cref="PlayerPrefabCreator"/>.
    /// Run via menu: <b>TomatoFighters > Characters > Create Brutor</b>.
    /// </summary>
    public static class BrutorCharacterCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Brutor.prefab";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/MovementConfigs";
        private const string COMBO_FOLDER = "Assets/ScriptableObjects/ComboDefinitions";
        private const string ATTACKS_FOLDER = "Assets/ScriptableObjects/Attacks/Brutor";
        private const string DEFENSE_CONFIG_FOLDER = "Assets/ScriptableObjects/DefenseConfigs";
        private const string BRUTOR_CONFIG_PATH = CONFIG_FOLDER + "/Brutor_MovementConfig.asset";
        private const string BRUTOR_COMBO_PATH = COMBO_FOLDER + "/Brutor_ComboDefinition.asset";
        private const string BRUTOR_DEFENSE_PATH = DEFENSE_CONFIG_FOLDER + "/Brutor_DefenseConfig.asset";
        private const string PASSIVE_CONFIG_FOLDER = "Assets/ScriptableObjects/Passives";
        private const string PASSIVE_CONFIG_PATH = PASSIVE_CONFIG_FOLDER + "/PassiveConfig.asset";
        private const string CONTROLLER_PATH = "Assets/Animations/Brutor/Brutor_Override.overrideController";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("TomatoFighters/Characters/Create Brutor")]
        public static void CreateBrutor()
        {
            PlayerPrefabCreator.EnsureFolderExists("Assets/Prefabs/Player");
            PlayerPrefabCreator.EnsureFolderExists(CONFIG_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(COMBO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);

            PlayerPrefabCreator.EnsureFolderExists(PASSIVE_CONFIG_FOLDER);

            var movementConfig = LoadBrutorMovementConfig();
            var comboDef = LoadBrutorComboDefinition();
            var defenseConfig = CreateOrLoadBrutorDefenseConfig();
            var passiveConfig = CreateOrLoadPassiveConfig();
            WireAttackDataIntoComboSteps(comboDef);

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            if (controller == null)
                Debug.LogWarning("[BrutorCreator] Override controller not found. Run 'Build Animations > All Characters' first.");

            var config = new CharacterPrefabConfig
            {
                prefabPath = PREFAB_PATH,
                characterType = CharacterType.Brutor,
                movementConfig = movementConfig,
                comboDefinition = comboDef,
                animatorController = controller,
                inputActions = inputActions,
                defenseConfig = defenseConfig,
                passiveConfig = passiveConfig,
                baseAttack = 14f, // ATK 0.7 × base 20
                useTimerFallback = true,
                fallbackActiveDuration = 0.35f,
                hitboxes = new[]
                {
                    // Forward punch — shield bash workhorse
                    new HitboxDefinition
                    {
                        hitboxId = "Jab",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(0.8f, 0.6f),
                        offset = new Vector2(0.5f, 0.5f)
                    },
                    // Wide low arc — sweep attack
                    new HitboxDefinition
                    {
                        hitboxId = "Sweep",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.3f, 0.5f),
                        offset = new Vector2(0.4f, 0.2f)
                    },
                    // Vertical launcher
                    new HitboxDefinition
                    {
                        hitboxId = "Uppercut",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(0.7f, 1.0f),
                        offset = new Vector2(0.4f, 0.8f)
                    },
                    // Overhead wide slam
                    new HitboxDefinition
                    {
                        hitboxId = "Slam",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.2f, 0.8f),
                        offset = new Vector2(0.3f, 0.3f)
                    }
                }
            };

            PlayerPrefabCreator.CreatePlayerPrefab(config);
            Debug.Log("[BrutorCreator] Brutor prefab created successfully at " + PREFAB_PATH);
        }

        private static PassiveConfig CreateOrLoadPassiveConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PassiveConfig>(PASSIVE_CONFIG_PATH);
            if (existing != null)
                return existing;

            var config = ScriptableObject.CreateInstance<PassiveConfig>();
            AssetDatabase.CreateAsset(config, PASSIVE_CONFIG_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[BrutorCreator] Created PassiveConfig at " + PASSIVE_CONFIG_PATH);
            return config;
        }

        private static MovementConfig LoadBrutorMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(BRUTOR_CONFIG_PATH);
            if (existing != null)
                return existing;

            Debug.LogWarning("[BrutorCreator] Brutor_MovementConfig not found at " + BRUTOR_CONFIG_PATH);
            return null;
        }

        private static ComboDefinition LoadBrutorComboDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ComboDefinition>(BRUTOR_COMBO_PATH);
            if (existing != null)
                return existing;

            Debug.LogWarning("[BrutorCreator] Brutor_ComboDefinition not found at " + BRUTOR_COMBO_PATH);
            return null;
        }

        private static DefenseConfig CreateOrLoadBrutorDefenseConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DefenseConfig>(BRUTOR_DEFENSE_PATH);
            var config = existing;

            if (config == null)
            {
                PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);
                config = ScriptableObject.CreateInstance<DefenseConfig>();
            }

            // Brutor: widest deflect/clash (face-tanking), shortest dodge (big body)
            config.deflectWindowDuration = 0.20f;
            config.clashWindowStart = 0.0f;
            config.clashWindowEnd = 0.5f;
            config.dodgeIFrameStart = 0.08f;
            config.dodgeIFrameEnd = 0.20f;

            // Wire BrutorDefenseBonus
            if (config.defenseBonus == null)
            {
                var bonusGuids = AssetDatabase.FindAssets("t:BrutorDefenseBonus");
                if (bonusGuids.Length > 0)
                {
                    config.defenseBonus = AssetDatabase.LoadAssetAtPath<DefenseBonus>(
                        AssetDatabase.GUIDToAssetPath(bonusGuids[0]));
                }
                else
                {
                    var bonus = ScriptableObject.CreateInstance<BrutorDefenseBonus>();
                    string bonusPath = DEFENSE_CONFIG_FOLDER + "/BrutorDefenseBonus.asset";
                    AssetDatabase.CreateAsset(bonus, bonusPath);
                    config.defenseBonus = bonus;
                    Debug.Log("[BrutorCreator] Created BrutorDefenseBonus SO.");
                }
            }

            if (existing == null)
                AssetDatabase.CreateAsset(config, BRUTOR_DEFENSE_PATH);
            else
                EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log("[BrutorCreator] Created/Updated Brutor_DefenseConfig.");
            return config;
        }

        /// <summary>
        /// Loads Brutor AttackData SOs and assigns them to the corresponding ComboSteps.
        /// Also ensures hitboxId is set on each AttackData.
        /// </summary>
        private static void WireAttackDataIntoComboSteps(ComboDefinition comboDef)
        {
            if (comboDef == null || comboDef.steps == null) return;

            // Step index -> (attack SO name, hitboxId)
            var mapping = new (string soName, string hitboxId)[]
            {
                ("BrutorShieldBash1",  "Jab"),      // 0: light opener
                ("BrutorShieldBash2",  "Jab"),      // 1: light follow-up
                ("BrutorSweep",        "Sweep"),    // 2: light finisher
                ("BrutorLauncher",     "Uppercut"), // 3: branch heavy
                ("BrutorLauncherSlam", "Slam"),     // 4: branch finisher
                ("BrutorOverheadSlam", "Slam"),     // 5: heavy opener
                ("BrutorGroundPound",  "Slam"),     // 6: heavy finisher
            };

            bool dirty = false;
            for (int i = 0; i < mapping.Length && i < comboDef.steps.Length; i++)
            {
                var (soName, hitboxId) = mapping[i];
                string path = $"{ATTACKS_FOLDER}/{soName}.asset";
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);

                if (attack == null)
                {
                    Debug.LogWarning($"[BrutorCreator] AttackData '{soName}' not found at {path}.");
                    continue;
                }

                if (comboDef.steps[i].attackData != attack)
                {
                    comboDef.steps[i].attackData = attack;
                    dirty = true;
                }

                if (attack.hitboxId != hitboxId)
                {
                    attack.hitboxId = hitboxId;
                    EditorUtility.SetDirty(attack);
                    Debug.Log($"[BrutorCreator] {soName} -> hitboxId='{hitboxId}'");
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(comboDef);
                AssetDatabase.SaveAssets();
                Debug.Log("[BrutorCreator] Wired AttackData SOs into ComboSteps.");
            }
        }
    }
}
