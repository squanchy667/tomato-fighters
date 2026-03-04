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
    /// Creates a complete Slasher player prefab: loads/creates Slasher-specific
    /// ScriptableObjects, wires AttackData into ComboSteps, defines hitbox shapes,
    /// and delegates to <see cref="PlayerPrefabCreator"/> for the actual build.
    /// Run via menu: <b>TomatoFighters > Characters > Create Slasher</b>.
    /// </summary>
    public static class SlasherCharacterCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Slasher.prefab";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/MovementConfigs";
        private const string COMBO_FOLDER = "Assets/ScriptableObjects/ComboDefinitions";
        private const string ATTACKS_FOLDER = "Assets/ScriptableObjects/Attacks/Slasher";
        private const string DEFENSE_CONFIG_FOLDER = "Assets/ScriptableObjects/DefenseConfigs";
        private const string SLASHER_CONFIG_PATH = CONFIG_FOLDER + "/Slasher_MovementConfig.asset";
        private const string SLASHER_COMBO_PATH = COMBO_FOLDER + "/Slasher_ComboDefinition.asset";
        private const string SLASHER_DEFENSE_PATH = DEFENSE_CONFIG_FOLDER + "/Slasher_DefenseConfig.asset";
        private const string PASSIVE_CONFIG_FOLDER = "Assets/ScriptableObjects/Passives";
        private const string PASSIVE_CONFIG_PATH = PASSIVE_CONFIG_FOLDER + "/PassiveConfig.asset";
        private const string CONTROLLER_PATH = "Assets/Animations/Slasher/Slasher_Override.overrideController";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("TomatoFighters/Characters/Create Slasher")]
        public static void CreateSlasher()
        {
            PlayerPrefabCreator.EnsureFolderExists("Assets/Prefabs/Player");
            PlayerPrefabCreator.EnsureFolderExists(CONFIG_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(COMBO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);

            PlayerPrefabCreator.EnsureFolderExists(PASSIVE_CONFIG_FOLDER);

            var movementConfig = CreateOrLoadSlasherMovementConfig();
            var comboDef = CreateOrLoadSlasherComboDefinition();
            var defenseConfig = CreateOrLoadSlasherDefenseConfig();
            var passiveConfig = CreateOrLoadPassiveConfig();
            WireAttackDataIntoComboSteps(comboDef);

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            if (controller == null)
                Debug.LogWarning("[SlasherCreator] Override controller not found. Run 'Build Animations > All Characters' first.");

            var config = new CharacterPrefabConfig
            {
                prefabPath = PREFAB_PATH,
                characterType = CharacterType.Slasher,
                movementConfig = movementConfig,
                comboDefinition = comboDef,
                animatorController = controller,
                inputActions = inputActions,
                defenseConfig = defenseConfig,
                passiveConfig = passiveConfig,
                baseAttack = 20f, // ATK 2.0 × base 10
                useTimerFallback = true,
                fallbackActiveDuration = 0.25f,
                hitboxes = new[]
                {
                    // Standard forward slash — light chain workhorse
                    new HitboxDefinition
                    {
                        hitboxId = "Slash",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.0f, 0.6f),
                        offset = new Vector2(0.6f, 0.5f)
                    },
                    // Wider arc for heavy and finisher slashes
                    new HitboxDefinition
                    {
                        hitboxId = "WideSlash",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.3f, 0.8f),
                        offset = new Vector2(0.5f, 0.5f)
                    },
                    // Long forward reach for lunge attacks
                    new HitboxDefinition
                    {
                        hitboxId = "Lunge",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.8f, 0.4f),
                        offset = new Vector2(1.0f, 0.5f)
                    },
                    // Circular AoE for spin finisher
                    new HitboxDefinition
                    {
                        hitboxId = "Spin",
                        shape = HitboxShape.Circle,
                        circleRadius = 0.9f,
                        offset = new Vector2(0f, 0.5f)
                    }
                }
            };

            PlayerPrefabCreator.CreatePlayerPrefab(config);
            Debug.Log("[SlasherCreator] Slasher prefab created successfully at " + PREFAB_PATH);
        }

        private static PassiveConfig CreateOrLoadPassiveConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PassiveConfig>(PASSIVE_CONFIG_PATH);
            if (existing != null)
                return existing;

            var config = ScriptableObject.CreateInstance<PassiveConfig>();
            AssetDatabase.CreateAsset(config, PASSIVE_CONFIG_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[SlasherCreator] Created PassiveConfig at " + PASSIVE_CONFIG_PATH);
            return config;
        }

        private static MovementConfig CreateOrLoadSlasherMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(SLASHER_CONFIG_PATH);
            if (existing != null)
                return existing;

            // Slasher: SPD 1.3 — fastest ground character, aggressive mobility
            var config = ScriptableObject.CreateInstance<MovementConfig>();
            config.moveSpeed = 10f;
            config.depthSpeed = 6.5f;
            config.groundAcceleration = 75f;
            config.airAcceleration = 40f;
            config.jumpForce = 13f;
            config.jumpGravity = 36f;
            config.coyoteTime = 0.1f;
            config.jumpBufferTime = 0.12f;
            config.dashSpeed = 24f;
            config.dashDuration = 0.12f;
            config.dashCooldown = 0.4f;
            config.dashHasIFrames = true;
            config.runSpeedMultiplier = 1.6f;

            AssetDatabase.CreateAsset(config, SLASHER_CONFIG_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[SlasherCreator] Created Slasher_MovementConfig.");
            return config;
        }

        private static ComboDefinition CreateOrLoadSlasherComboDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ComboDefinition>(SLASHER_COMBO_PATH);
            if (existing != null)
                return existing;

            // Slasher combo tree:
            // Light chain: Slash1(0) → Slash2(1) → Slash3(2, finisher)
            // Heavy chain: HeavySlash(3) → Lunge(4) → LungeFinisher(5, finisher)
            // Branch: L1→H = QuickSlash(6), L2→H = SpinFinisher(7)
            var def = ScriptableObject.CreateInstance<ComboDefinition>();
            def.defaultComboWindow = 0.3f;
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 3;

            def.steps = new ComboStep[]
            {
                // 0: SlasherSlash1 — fast opening jab
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light1",
                    damageMultiplier = 1.0f,
                    nextOnLight = 1, nextOnHeavy = 6,
                    isFinisher = false
                },
                // 1: SlasherSlash2 — follow-up cross
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light2",
                    damageMultiplier = 1.1f,
                    nextOnLight = 2, nextOnHeavy = 7,
                    canDashCancelOnHit = true,
                    isFinisher = false
                },
                // 2: SlasherSlash3 — light chain finisher
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "LightFinisher",
                    damageMultiplier = 1.5f,
                    nextOnLight = -1, nextOnHeavy = -1,
                    isFinisher = true
                },
                // 3: SlasherHeavySlash — powerful opening heavy
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Heavy1",
                    damageMultiplier = 1.8f,
                    comboWindowDuration = 0.5f,
                    nextOnLight = -1, nextOnHeavy = 4,
                    canDashCancelOnHit = true, canJumpCancelOnHit = true,
                    isFinisher = false
                },
                // 4: SlasherLunge — forward rush attack
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Heavy2",
                    damageMultiplier = 1.6f,
                    comboWindowDuration = 0.4f,
                    nextOnLight = -1, nextOnHeavy = 5,
                    canDashCancelOnHit = true,
                    isFinisher = false
                },
                // 5: SlasherLungeFinisher — big thrust finish
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "HeavyFinisher",
                    damageMultiplier = 2.5f,
                    nextOnLight = -1, nextOnHeavy = -1,
                    isFinisher = true
                },
                // 6: SlasherQuickSlash — fast poke from L1→H, chains back into L2
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Heavy1",
                    damageMultiplier = 1.3f,
                    comboWindowDuration = 0.35f,
                    nextOnLight = 1, nextOnHeavy = -1,
                    canDashCancelOnHit = true,
                    isFinisher = false
                },
                // 7: SlasherSpinFinisher — AoE finisher from L2→H
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "HeavyFinisher",
                    damageMultiplier = 2.2f,
                    nextOnLight = -1, nextOnHeavy = -1,
                    isFinisher = true
                }
            };

            AssetDatabase.CreateAsset(def, SLASHER_COMBO_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[SlasherCreator] Created Slasher_ComboDefinition.");
            return def;
        }

        private static DefenseConfig CreateOrLoadSlasherDefenseConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DefenseConfig>(SLASHER_DEFENSE_PATH);
            var config = existing;

            if (config == null)
            {
                PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);
                config = ScriptableObject.CreateInstance<DefenseConfig>();
            }

            // Slasher: tighter deflect/clash windows but rewarded with guaranteed crit
            config.deflectWindowDuration = 0.12f;
            config.clashWindowStart = 0.0f;
            config.clashWindowEnd = 0.4f;
            config.dodgeIFrameStart = 0.04f;
            config.dodgeIFrameEnd = 0.25f;

            // Wire SlasherDefenseBonus if it exists
            if (config.defenseBonus == null)
            {
                var bonusGuids = AssetDatabase.FindAssets("t:SlasherDefenseBonus");
                if (bonusGuids.Length > 0)
                {
                    config.defenseBonus = AssetDatabase.LoadAssetAtPath<DefenseBonus>(
                        AssetDatabase.GUIDToAssetPath(bonusGuids[0]));
                }
                else
                {
                    var bonus = ScriptableObject.CreateInstance<SlasherDefenseBonus>();
                    string bonusPath = DEFENSE_CONFIG_FOLDER + "/SlasherDefenseBonus.asset";
                    AssetDatabase.CreateAsset(bonus, bonusPath);
                    config.defenseBonus = bonus;
                    Debug.Log("[SlasherCreator] Created SlasherDefenseBonus SO.");
                }
            }

            if (existing == null)
                AssetDatabase.CreateAsset(config, SLASHER_DEFENSE_PATH);
            else
                EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log("[SlasherCreator] Created/Updated Slasher_DefenseConfig.");
            return config;
        }

        /// <summary>
        /// Loads Slasher AttackData SOs and assigns them to the corresponding ComboSteps.
        /// Also ensures hitboxId is set on each AttackData.
        /// </summary>
        private static void WireAttackDataIntoComboSteps(ComboDefinition comboDef)
        {
            if (comboDef == null || comboDef.steps == null) return;

            // Step index → (attack SO name, hitboxId, clashStart, clashEnd)
            // Clash windows: 0/0 = no clash (light attacks). Heavy attacks get clash before hitbox.
            var mapping = new (string soName, string hitboxId, float clashStart, float clashEnd)[]
            {
                ("SlasherSlash1",        "Slash",     0f,   0f),     // 0: light — no clash
                ("SlasherSlash2",        "Slash",     0f,   0f),     // 1: light — no clash
                ("SlasherSlash3",        "WideSlash", 0f,   0f),     // 2: light finisher — no clash
                ("SlasherHeavySlash",    "WideSlash", 0f,   0.35f),  // 3: heavy opener — hitbox at frame 3 (~50ms)
                ("SlasherLunge",         "Lunge",     0f,   0.35f),  // 4: heavy lunge — hitbox at frame 3 (~50ms)
                ("SlasherLungeFinisher", "Lunge",     0f,   0.4f),   // 5: heavy finisher — hitbox at frame 4 (~67ms)
                ("SlasherQuickSlash",    "Slash",     0f,   0.25f),  // 6: quick poke (L1→H) — hitbox at frame 2 (~33ms)
                ("SlasherSpinFinisher",  "Spin",      0f,   0.35f),  // 7: spin AoE (L2→H) — hitbox at frame 3 (~50ms)
            };

            bool dirty = false;
            for (int i = 0; i < mapping.Length && i < comboDef.steps.Length; i++)
            {
                var (soName, hitboxId, clashStart, clashEnd) = mapping[i];
                string path = $"{ATTACKS_FOLDER}/{soName}.asset";
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);

                if (attack == null)
                {
                    Debug.LogWarning($"[SlasherCreator] AttackData '{soName}' not found at {path}.");
                    continue;
                }

                if (comboDef.steps[i].attackData != attack)
                {
                    comboDef.steps[i].attackData = attack;
                    dirty = true;
                }

                bool attackDirty = false;

                if (attack.hitboxId != hitboxId)
                {
                    attack.hitboxId = hitboxId;
                    attackDirty = true;
                }

                if (attack.clashWindowStart != clashStart || attack.clashWindowEnd != clashEnd)
                {
                    attack.clashWindowStart = clashStart;
                    attack.clashWindowEnd = clashEnd;
                    attackDirty = true;
                }

                if (attackDirty)
                {
                    EditorUtility.SetDirty(attack);
                    Debug.Log($"[SlasherCreator] {soName} → hitboxId='{hitboxId}', clash=[{clashStart:F2}s, {clashEnd:F2}s]");
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(comboDef);
                AssetDatabase.SaveAssets();
                Debug.Log("[SlasherCreator] Wired AttackData SOs into ComboSteps.");
            }
        }
    }
}
