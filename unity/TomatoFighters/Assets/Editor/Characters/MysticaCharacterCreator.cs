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
    /// Creates a complete Mystica player prefab: loads/creates Mystica-specific
    /// ScriptableObjects, wires AttackData into ComboSteps, defines hitbox shapes,
    /// and delegates to <see cref="PlayerPrefabCreator"/> for the actual build.
    /// Run via menu: <b>TomatoFighters > Characters > Create Mystica</b>.
    /// </summary>
    public static class MysticaCharacterCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Mystica.prefab";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/MovementConfigs";
        private const string COMBO_FOLDER = "Assets/ScriptableObjects/ComboDefinitions";
        private const string ATTACKS_FOLDER = "Assets/ScriptableObjects/Attacks/Mystica";
        private const string MYSTICA_CONFIG_PATH = CONFIG_FOLDER + "/Mystica_MovementConfig.asset";
        private const string MYSTICA_COMBO_PATH = COMBO_FOLDER + "/Mystica_ComboDefinition.asset";
        private const string DEFENSE_CONFIG_FOLDER = "Assets/ScriptableObjects/DefenseConfigs";
        private const string MYSTICA_DEFENSE_PATH = DEFENSE_CONFIG_FOLDER + "/Mystica_DefenseConfig.asset";
        private const string CONTROLLER_PATH = "Assets/Animations/Mystica/Mystica_Controller.controller";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("TomatoFighters/Characters/Create Mystica")]
        public static void CreateMystica()
        {
            PlayerPrefabCreator.EnsureFolderExists("Assets/Prefabs/Player");
            PlayerPrefabCreator.EnsureFolderExists(CONFIG_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(COMBO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);

            var movementConfig = CreateOrLoadMysticaMovementConfig();
            var comboDef = CreateOrLoadMysticaComboDefinition();
            var defenseConfig = CreateOrLoadMysticaDefenseConfig();
            WireAttackDataIntoComboSteps(comboDef);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            if (controller == null)
                Debug.LogWarning("[MysticaCreator] AnimatorController not found. Run 'Build Animations' first.");

            var config = new CharacterPrefabConfig
            {
                prefabPath = PREFAB_PATH,
                characterType = CharacterType.Mystica,
                movementConfig = movementConfig,
                comboDefinition = comboDef,
                animatorController = controller,
                inputActions = inputActions,
                defenseConfig = defenseConfig,
                baseAttack = 10f,
                useTimerFallback = true,
                fallbackActiveDuration = 0.3f,
                hitboxes = new[]
                {
                    new HitboxDefinition
                    {
                        hitboxId = "Burst",
                        shape = HitboxShape.Circle,
                        circleRadius = 0.5f,
                        offset = new Vector2(0.5f, 0.5f)
                    },
                    new HitboxDefinition
                    {
                        hitboxId = "BigBurst",
                        shape = HitboxShape.Circle,
                        circleRadius = 0.8f,
                        offset = new Vector2(0.4f, 0.5f)
                    },
                    new HitboxDefinition
                    {
                        hitboxId = "Bolt",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.6f, 0.35f),
                        offset = new Vector2(1.1f, 0.5f)
                    }
                }
            };

            PlayerPrefabCreator.CreatePlayerPrefab(config);
            Debug.Log("[MysticaCreator] Mystica prefab created successfully.");
        }

        private static MovementConfig CreateOrLoadMysticaMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(MYSTICA_CONFIG_PATH);
            if (existing != null)
                return existing;

            var config = ScriptableObject.CreateInstance<MovementConfig>();
            config.moveSpeed = 8f;
            config.depthSpeed = 5f;
            config.groundAcceleration = 65f;
            config.airAcceleration = 35f;
            config.jumpForce = 14f;
            config.jumpGravity = 38f;
            config.coyoteTime = 0.1f;
            config.jumpBufferTime = 0.12f;
            config.dashSpeed = 20f;
            config.dashDuration = 0.14f;
            config.dashCooldown = 0.5f;
            config.dashHasIFrames = true;
            config.runSpeedMultiplier = 1.5f;

            AssetDatabase.CreateAsset(config, MYSTICA_CONFIG_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[MysticaCreator] Created Mystica_MovementConfig.");
            return config;
        }

        private static ComboDefinition CreateOrLoadMysticaComboDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ComboDefinition>(MYSTICA_COMBO_PATH);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<ComboDefinition>();
            def.defaultComboWindow = 0.5f;
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 3;

            def.steps = new ComboStep[]
            {
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light1",
                    damageMultiplier = 1.0f,
                    nextOnLight = 1, nextOnHeavy = -1,
                    isFinisher = false
                },
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light2",
                    damageMultiplier = 1.1f,
                    nextOnLight = 2, nextOnHeavy = -1,
                    canDashCancelOnHit = true,
                    isFinisher = false
                },
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "LightFinisher",
                    damageMultiplier = 1.4f,
                    nextOnLight = -1, nextOnHeavy = -1,
                    isFinisher = true
                },
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Heavy1",
                    damageMultiplier = 1.8f,
                    comboWindowDuration = 0.6f,
                    nextOnLight = -1, nextOnHeavy = 4,
                    canDashCancelOnHit = true, canJumpCancelOnHit = true,
                    isFinisher = false
                },
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "HeavyFinisher",
                    damageMultiplier = 2.5f,
                    nextOnLight = -1, nextOnHeavy = -1,
                    isFinisher = true
                },
            };

            AssetDatabase.CreateAsset(def, MYSTICA_COMBO_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[MysticaCreator] Created Mystica_ComboDefinition.");
            return def;
        }

        private static DefenseConfig CreateOrLoadMysticaDefenseConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DefenseConfig>(MYSTICA_DEFENSE_PATH);
            var config = existing;

            if (config == null)
            {
                PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);
                config = ScriptableObject.CreateInstance<DefenseConfig>();
            }

            // Mystica: widest dodge (primary survival), generous clash for testing
            config.deflectWindowDuration = 0.10f;
            config.clashWindowStart = 0.0f;
            config.clashWindowEnd = 0.3f;
            config.dodgeIFrameStart = 0.03f;
            config.dodgeIFrameEnd = 0.35f;

            // Wire MysticaDefenseBonus if it exists
            if (config.defenseBonus == null)
            {
                var bonusGuids = AssetDatabase.FindAssets("t:MysticaDefenseBonus");
                if (bonusGuids.Length > 0)
                {
                    config.defenseBonus = AssetDatabase.LoadAssetAtPath<DefenseBonus>(
                        AssetDatabase.GUIDToAssetPath(bonusGuids[0]));
                }
                else
                {
                    var bonus = ScriptableObject.CreateInstance<MysticaDefenseBonus>();
                    string bonusPath = DEFENSE_CONFIG_FOLDER + "/MysticaDefenseBonus.asset";
                    AssetDatabase.CreateAsset(bonus, bonusPath);
                    config.defenseBonus = bonus;
                    Debug.Log("[MysticaCreator] Created MysticaDefenseBonus SO.");
                }
            }

            if (existing == null)
                AssetDatabase.CreateAsset(config, MYSTICA_DEFENSE_PATH);
            else
                EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log("[MysticaCreator] Created/Updated Mystica_DefenseConfig.");
            return config;
        }

        /// <summary>
        /// Loads Mystica AttackData SOs and assigns them to the corresponding ComboSteps.
        /// Also ensures hitboxId is set on each AttackData.
        /// </summary>
        private static void WireAttackDataIntoComboSteps(ComboDefinition comboDef)
        {
            if (comboDef == null || comboDef.steps == null) return;

            // Step index → (attack SO name, hitboxId)
            var mapping = new (string soName, string hitboxId)[]
            {
                ("MysticaStrike1",       "Burst"),
                ("MysticaStrike2",       "Burst"),
                ("MysticaStrike3",       "BigBurst"),
                ("MysticaArcaneBolt",    "Bolt"),
                ("MysticaEmpoweredBolt", "Bolt"),
            };

            bool dirty = false;
            for (int i = 0; i < mapping.Length && i < comboDef.steps.Length; i++)
            {
                var (soName, hitboxId) = mapping[i];
                string path = $"{ATTACKS_FOLDER}/{soName}.asset";
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);

                if (attack == null)
                {
                    Debug.LogWarning($"[MysticaCreator] AttackData '{soName}' not found at {path}.");
                    continue;
                }

                // Wire into combo step
                if (comboDef.steps[i].attackData != attack)
                {
                    comboDef.steps[i].attackData = attack;
                    dirty = true;
                }

                // Ensure hitboxId is set
                if (attack.hitboxId != hitboxId)
                {
                    attack.hitboxId = hitboxId;
                    EditorUtility.SetDirty(attack);
                    Debug.Log($"[MysticaCreator] {soName} → hitboxId='{hitboxId}'");
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(comboDef);
                AssetDatabase.SaveAssets();
                Debug.Log("[MysticaCreator] Wired AttackData SOs into ComboSteps.");
            }
        }
    }
}
