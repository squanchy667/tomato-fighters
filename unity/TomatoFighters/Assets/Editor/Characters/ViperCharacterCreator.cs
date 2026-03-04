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
    /// Creates a complete Viper player prefab: loads existing Viper-specific
    /// ScriptableObjects, creates MovementConfig and DefenseConfig, wires AttackData
    /// into ComboSteps, defines hitbox shapes, and delegates to <see cref="PlayerPrefabCreator"/>.
    /// Run via menu: <b>TomatoFighters > Characters > Create Viper</b>.
    /// </summary>
    public static class ViperCharacterCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Viper.prefab";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/MovementConfigs";
        private const string COMBO_FOLDER = "Assets/ScriptableObjects/ComboDefinitions";
        private const string ATTACKS_FOLDER = "Assets/ScriptableObjects/Attacks/Viper";
        private const string DEFENSE_CONFIG_FOLDER = "Assets/ScriptableObjects/DefenseConfigs";
        private const string VIPER_CONFIG_PATH = CONFIG_FOLDER + "/Viper_MovementConfig.asset";
        private const string VIPER_COMBO_PATH = COMBO_FOLDER + "/Viper_ComboDefinition.asset";
        private const string VIPER_DEFENSE_PATH = DEFENSE_CONFIG_FOLDER + "/Viper_DefenseConfig.asset";
        private const string CONTROLLER_PATH = "Assets/Animations/Viper/Viper_Controller.controller";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("TomatoFighters/Characters/Create Viper")]
        public static void CreateViper()
        {
            PlayerPrefabCreator.EnsureFolderExists("Assets/Prefabs/Player");
            PlayerPrefabCreator.EnsureFolderExists(CONFIG_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(COMBO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);

            var movementConfig = CreateOrLoadViperMovementConfig();
            var comboDef = LoadViperComboDefinition();
            var defenseConfig = CreateOrLoadViperDefenseConfig();
            WireAttackDataIntoComboSteps(comboDef);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            if (controller == null)
                Debug.LogWarning("[ViperCreator] AnimatorController not found. Run 'Build Animations' first.");

            var config = new CharacterPrefabConfig
            {
                prefabPath = PREFAB_PATH,
                characterType = CharacterType.Viper,
                movementConfig = movementConfig,
                comboDefinition = comboDef,
                animatorController = controller,
                inputActions = inputActions,
                defenseConfig = defenseConfig,
                baseAttack = 18f, // ATK 1.8 × base 10
                useTimerFallback = true,
                fallbackActiveDuration = 0.2f,
                hitboxes = new[]
                {
                    // Long narrow ranged reach — projectile shots
                    new HitboxDefinition
                    {
                        hitboxId = "Lunge",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.8f, 0.35f),
                        offset = new Vector2(1.0f, 0.5f)
                    },
                    // Short wide burst — rapid burst attack
                    new HitboxDefinition
                    {
                        hitboxId = "Sweep",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.0f, 0.6f),
                        offset = new Vector2(0.5f, 0.4f)
                    }
                }
            };

            PlayerPrefabCreator.CreatePlayerPrefab(config);
            Debug.Log("[ViperCreator] Viper prefab created successfully at " + PREFAB_PATH);
        }

        private static MovementConfig CreateOrLoadViperMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(VIPER_CONFIG_PATH);
            if (existing != null)
                return existing;

            // Viper: SPD 1.1 — mobile ranged character
            var config = ScriptableObject.CreateInstance<MovementConfig>();
            config.moveSpeed = 8.8f;
            config.depthSpeed = 5.5f;
            config.groundAcceleration = 50f;
            config.airAcceleration = 25f;
            config.jumpForce = 13f;
            config.jumpGravity = 35f;
            config.coyoteTime = 0.1f;
            config.jumpBufferTime = 0.12f;
            config.dashSpeed = 18f;
            config.dashDuration = 0.15f;
            config.dashCooldown = 0.5f;
            config.dashHasIFrames = true;
            config.runSpeedMultiplier = 1.5f;

            AssetDatabase.CreateAsset(config, VIPER_CONFIG_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[ViperCreator] Created Viper_MovementConfig.");
            return config;
        }

        private static ComboDefinition LoadViperComboDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ComboDefinition>(VIPER_COMBO_PATH);
            if (existing != null)
                return existing;

            Debug.LogWarning("[ViperCreator] Viper_ComboDefinition not found at " + VIPER_COMBO_PATH);
            return null;
        }

        private static DefenseConfig CreateOrLoadViperDefenseConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DefenseConfig>(VIPER_DEFENSE_PATH);
            var config = existing;

            if (config == null)
            {
                PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);
                config = ScriptableObject.CreateInstance<DefenseConfig>();
            }

            // Viper: balanced-evasive — generous clash for testing
            config.deflectWindowDuration = 0.14f;
            config.clashWindowStart = 0.0f;
            config.clashWindowEnd = 0.4f;
            config.dodgeIFrameStart = 0.04f;
            config.dodgeIFrameEnd = 0.30f;

            // Wire ViperDefenseBonus
            if (config.defenseBonus == null)
            {
                var bonusGuids = AssetDatabase.FindAssets("t:ViperDefenseBonus");
                if (bonusGuids.Length > 0)
                {
                    config.defenseBonus = AssetDatabase.LoadAssetAtPath<DefenseBonus>(
                        AssetDatabase.GUIDToAssetPath(bonusGuids[0]));
                }
                else
                {
                    var bonus = ScriptableObject.CreateInstance<ViperDefenseBonus>();
                    string bonusPath = DEFENSE_CONFIG_FOLDER + "/ViperDefenseBonus.asset";
                    AssetDatabase.CreateAsset(bonus, bonusPath);
                    config.defenseBonus = bonus;
                    Debug.Log("[ViperCreator] Created ViperDefenseBonus SO.");
                }
            }

            if (existing == null)
                AssetDatabase.CreateAsset(config, VIPER_DEFENSE_PATH);
            else
                EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log("[ViperCreator] Created/Updated Viper_DefenseConfig.");
            return config;
        }

        /// <summary>
        /// Loads Viper AttackData SOs and assigns them to the corresponding ComboSteps.
        /// Also ensures hitboxId is set on each AttackData.
        /// </summary>
        private static void WireAttackDataIntoComboSteps(ComboDefinition comboDef)
        {
            if (comboDef == null || comboDef.steps == null) return;

            // Step index -> (attack SO name, hitboxId)
            var mapping = new (string soName, string hitboxId)[]
            {
                ("ViperShot1",        "Lunge"), // 0: light opener
                ("ViperShot2",        "Lunge"), // 1: light follow-up
                ("ViperRapidBurst",   "Sweep"), // 2: light finisher
                ("ViperQuickCharged", "Lunge"), // 3: branch heavy
                ("ViperChargedShot",  "Lunge"), // 4: heavy opener
                ("ViperPiercingShot", "Lunge"), // 5: heavy finisher
            };

            bool dirty = false;
            for (int i = 0; i < mapping.Length && i < comboDef.steps.Length; i++)
            {
                var (soName, hitboxId) = mapping[i];
                string path = $"{ATTACKS_FOLDER}/{soName}.asset";
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);

                if (attack == null)
                {
                    Debug.LogWarning($"[ViperCreator] AttackData '{soName}' not found at {path}.");
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
                    Debug.Log($"[ViperCreator] {soName} -> hitboxId='{hitboxId}'");
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(comboDef);
                AssetDatabase.SaveAssets();
                Debug.Log("[ViperCreator] Wired AttackData SOs into ComboSteps.");
            }
        }
    }
}
