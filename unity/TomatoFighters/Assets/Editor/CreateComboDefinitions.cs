using TomatoFighters.Combat;
using TomatoFighters.Shared.Data;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates or updates ComboDefinition and ComboInteractionConfig assets for all characters.
    /// Safe to re-run — overwrites existing assets in place (preserves GUIDs and prefab references).
    /// Run via menu: <b>Tools &gt; TomatoFighters &gt; Create Combo Definitions</b>.
    /// Depends on attack assets existing (run Create All Character Attacks first).
    /// </summary>
    public static class CreateComboDefinitions
    {
        private const string COMBO_FOLDER = "Assets/ScriptableObjects/ComboDefinitions";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/ComboInteractionConfigs";
        private const string ATTACKS_FOLDER = "Assets/ScriptableObjects/Attacks";

        private static bool hadErrors;

        [MenuItem("Tools/TomatoFighters/Create Combo Definitions")]
        public static void Execute()
        {
            hadErrors = false;

            EnsureFolderExists(COMBO_FOLDER);
            EnsureFolderExists(CONFIG_FOLDER);

            CreateBrutorCombo();
            CreateSlasherCombo();
            CreateMysticaCombo();
            CreateViperCombo();

            CreateInteractionConfig("Brutor_ComboInteractionConfig", new InteractionParams
            {
                cancelPriority            = CancelPriority.DashOverJump,
                dashCancelResetsCombo     = true,
                jumpCancelResetsCombo     = false,
                resetOnStagger            = true,
                resetOnDeath              = true,
                lockMovementDuringAttack  = true,
                lockMovementDuringFinisher = true
            });

            CreateInteractionConfig("Slasher_ComboInteractionConfig", new InteractionParams
            {
                cancelPriority            = CancelPriority.DashOverJump,
                dashCancelResetsCombo     = false,
                jumpCancelResetsCombo     = false,
                resetOnStagger            = true,
                resetOnDeath              = true,
                lockMovementDuringAttack  = true,
                lockMovementDuringFinisher = true
            });

            CreateInteractionConfig("Mystica_ComboInteractionConfig", new InteractionParams
            {
                cancelPriority            = CancelPriority.JumpOverDash,
                dashCancelResetsCombo     = true,
                jumpCancelResetsCombo     = false,
                resetOnStagger            = true,
                resetOnDeath              = true,
                lockMovementDuringAttack  = true,
                lockMovementDuringFinisher = true
            });

            CreateInteractionConfig("Viper_ComboInteractionConfig", new InteractionParams
            {
                cancelPriority            = CancelPriority.DashOverJump,
                dashCancelResetsCombo     = true,
                jumpCancelResetsCombo     = false,
                resetOnStagger            = true,
                resetOnDeath              = true,
                lockMovementDuringAttack  = false,
                lockMovementDuringFinisher = true
            });

            AssetDatabase.SaveAssets();
            WirePlayerPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (hadErrors)
            {
                Debug.LogError(
                    "[CreateComboDefinitions] Finished WITH ERRORS — some AttackData refs are missing! " +
                    "Run 'Tools > TomatoFighters > Create All Character Attacks' first, then re-run this.");
            }
            else
            {
                Debug.Log("[CreateComboDefinitions] Done. All combo definitions and interaction configs are up to date.");
            }
        }

        // ── Brutor (7 steps) ─────────────────────────────────────────────
        //
        // Index 0: L1 ShieldBash1     → nextL=1, nextH=3
        // Index 1: L2 ShieldBash2     → nextL=2, nextH=-1
        // Index 2: L3 Sweep Finisher  → isFinisher
        // Index 3: Launcher (L1→H)    → nextH=4
        // Index 4: LauncherSlam       → isFinisher
        // Index 5: H1 OverheadSlam    → nextH=6
        // Index 6: H2 GroundPound     → isFinisher

        private static void CreateBrutorCombo()
        {
            string path = $"{COMBO_FOLDER}/Brutor_ComboDefinition.asset";
            string af = $"{ATTACKS_FOLDER}/Brutor";

            var def = LoadOrCreate<ComboDefinition>(path);
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 5;
            def.defaultComboWindow = 0.3f;

            def.steps = new ComboStep[]
            {
                // 0: L1 Shield Bash 1
                MakeStep(
                    LoadAttack(af, "BrutorShieldBash1"), AttackType.Light,
                    "attack_light_1", nextL: 1, nextH: 3,
                    dashCancel: true, jumpCancel: false),

                // 1: L2 Shield Bash 2
                MakeStep(
                    LoadAttack(af, "BrutorShieldBash2"), AttackType.Light,
                    "attack_light_2", nextL: 2, nextH: -1,
                    dashCancel: true, jumpCancel: false),

                // 2: L3 Shield Sweep Finisher
                MakeStep(
                    LoadAttack(af, "BrutorSweep"), AttackType.Light,
                    "finisher_light", isFinisher: true,
                    dashCancel: true, jumpCancel: true),

                // 3: Uppercut Launcher (L1→H branch)
                MakeStep(
                    LoadAttack(af, "BrutorLauncher"), AttackType.Heavy,
                    "attack_branch_heavy", nextL: -1, nextH: 4,
                    dashCancel: false, jumpCancel: false),

                // 4: Air Slam (Launcher follow-up finisher)
                MakeStep(
                    LoadAttack(af, "BrutorLauncherSlam"), AttackType.Heavy,
                    "finisher_heavy", isFinisher: true,
                    dashCancel: true, jumpCancel: true),

                // 5: H1 Overhead Slam
                MakeStep(
                    LoadAttack(af, "BrutorOverheadSlam"), AttackType.Heavy,
                    "attack_heavy_1", nextL: -1, nextH: 6,
                    dashCancel: true, jumpCancel: false),

                // 6: H2 Ground Pound Finisher
                MakeStep(
                    LoadAttack(af, "BrutorGroundPound"), AttackType.Heavy,
                    "finisher_heavy_2", isFinisher: true,
                    dashCancel: true, jumpCancel: true),
            };

            SaveAsset(def, path);
            Debug.Log($"[CreateComboDefinitions] Brutor_ComboDefinition (7 steps) at {path}");
        }

        // ── Slasher (8 steps) ──────────────────────────────────────────────
        //
        // Index 0: L1 Slash1       → nextL=1, nextH=-1
        // Index 1: L2 Slash2       → nextL=2, nextH=4
        // Index 2: L3 Slash3       → nextL=3, nextH=-1
        // Index 3: L4 SpinFinisher → isFinisher
        // Index 4: Lunge (L2→H)   → standalone, no follow-up
        // Index 5: H1 HeavySlash   → nextH=6, nextL=7
        // Index 6: H2 LungeFinisher→ isFinisher
        // Index 7: QuickSlash (H→L)→ nextL=1 (re-enters at L2)

        private static void CreateSlasherCombo()
        {
            string path = $"{COMBO_FOLDER}/Slasher_ComboDefinition.asset";
            string af = $"{ATTACKS_FOLDER}/Slasher";

            var def = LoadOrCreate<ComboDefinition>(path);
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 5;
            def.defaultComboWindow = 0.25f;

            def.steps = new ComboStep[]
            {
                // 0: L1 Quick Slash 1
                MakeStep(
                    LoadAttack(af, "SlasherSlash1"), AttackType.Light,
                    "attack_light_1", nextL: 1, nextH: -1,
                    dashCancel: true, jumpCancel: true),

                // 1: L2 Quick Slash 2
                MakeStep(
                    LoadAttack(af, "SlasherSlash2"), AttackType.Light,
                    "attack_light_2", nextL: 2, nextH: 4,
                    dashCancel: true, jumpCancel: true),

                // 2: L3 Cross Slash
                MakeStep(
                    LoadAttack(af, "SlasherSlash3"), AttackType.Light,
                    "attack_light_3", nextL: 3, nextH: -1,
                    dashCancel: true, jumpCancel: true),

                // 3: L4 Spinning Finisher
                MakeStep(
                    LoadAttack(af, "SlasherSpinFinisher"), AttackType.Light,
                    "finisher_light", isFinisher: true,
                    dashCancel: true, jumpCancel: true),

                // 4: Lunge Thrust (L2→H branch, standalone)
                MakeStep(
                    LoadAttack(af, "SlasherLunge"), AttackType.Heavy,
                    "attack_branch_heavy", nextL: -1, nextH: -1,
                    dashCancel: true, jumpCancel: true),

                // 5: H1 Heavy Slash
                MakeStep(
                    LoadAttack(af, "SlasherHeavySlash"), AttackType.Heavy,
                    "attack_heavy_1", nextL: 7, nextH: 6,
                    dashCancel: true, jumpCancel: true),

                // 6: H2 Piercing Lunge Finisher
                MakeStep(
                    LoadAttack(af, "SlasherLungeFinisher"), AttackType.Heavy,
                    "finisher_heavy", isFinisher: true,
                    dashCancel: true, jumpCancel: true),

                // 7: Quick Re-entry Slash (H1→L, re-enters light chain at L2)
                MakeStep(
                    LoadAttack(af, "SlasherQuickSlash"), AttackType.Light,
                    "attack_branch_light", nextL: 1, nextH: -1,
                    dashCancel: true, jumpCancel: true),
            };

            SaveAsset(def, path);
            Debug.Log($"[CreateComboDefinitions] Slasher_ComboDefinition (8 steps) at {path}");
        }

        // ── Mystica (5 steps) ──────────────────────────────────────────────
        //
        // Index 0: L1 Strike1       → nextL=1
        // Index 1: L2 Strike2       → nextL=2
        // Index 2: L3 Strike3       → isFinisher (burst finisher)
        // Index 3: H1 Arcane Bolt   → nextH=4
        // Index 4: H2 Empowered Bolt→ isFinisher

        private static void CreateMysticaCombo()
        {
            string path = $"{COMBO_FOLDER}/Mystica_ComboDefinition.asset";
            string af = $"{ATTACKS_FOLDER}/Mystica";

            var def = LoadOrCreate<ComboDefinition>(path);
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 3;
            def.defaultComboWindow = 0.4f;

            def.steps = new ComboStep[]
            {
                // 0: L1 Magic Burst 1
                MakeStep(
                    LoadAttack(af, "MysticaStrike1"), AttackType.Light,
                    "attack_light_1", nextL: 1, nextH: -1,
                    dashCancel: false, jumpCancel: true),

                // 1: L2 Magic Burst 2
                MakeStep(
                    LoadAttack(af, "MysticaStrike2"), AttackType.Light,
                    "attack_light_2", nextL: 2, nextH: -1,
                    dashCancel: false, jumpCancel: true),

                // 2: L3 Magic Burst 3 (burst finisher)
                MakeStep(
                    LoadAttack(af, "MysticaStrike3"), AttackType.Light,
                    "finisher_light", isFinisher: true,
                    dashCancel: true, jumpCancel: true),

                // 3: H1 Arcane Bolt
                MakeStep(
                    LoadAttack(af, "MysticaArcaneBolt"), AttackType.Heavy,
                    "attack_heavy_1", nextL: -1, nextH: 4,
                    dashCancel: false, jumpCancel: true),

                // 4: H2 Empowered Arcane Bolt (finisher)
                MakeStep(
                    LoadAttack(af, "MysticaEmpoweredBolt"), AttackType.Heavy,
                    "finisher_heavy", isFinisher: true,
                    dashCancel: true, jumpCancel: true),
            };

            SaveAsset(def, path);
            Debug.Log($"[CreateComboDefinitions] Mystica_ComboDefinition (5 steps) at {path}");
        }

        // ── Viper (6 steps) ────────────────────────────────────────────────
        //
        // Index 0: L1 Shot1          → nextL=1
        // Index 1: L2 Shot2          → nextL=2, nextH=3
        // Index 2: L3 Rapid Burst    → isFinisher
        // Index 3: Quick Charged (L2→H) → standalone
        // Index 4: H1 Charged Shot   → nextH=5
        // Index 5: H2 Piercing Shot  → isFinisher

        private static void CreateViperCombo()
        {
            string path = $"{COMBO_FOLDER}/Viper_ComboDefinition.asset";
            string af = $"{ATTACKS_FOLDER}/Viper";

            var def = LoadOrCreate<ComboDefinition>(path);
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 4;
            def.defaultComboWindow = 0.3f;

            def.steps = new ComboStep[]
            {
                // 0: L1 Quick Shot 1
                MakeStep(
                    LoadAttack(af, "ViperShot1"), AttackType.Light,
                    "attack_light_1", nextL: 1, nextH: -1,
                    dashCancel: true, jumpCancel: false),

                // 1: L2 Quick Shot 2
                MakeStep(
                    LoadAttack(af, "ViperShot2"), AttackType.Light,
                    "attack_light_2", nextL: 2, nextH: 3,
                    dashCancel: true, jumpCancel: false),

                // 2: L3 Rapid Burst (finisher)
                MakeStep(
                    LoadAttack(af, "ViperRapidBurst"), AttackType.Light,
                    "finisher_light", isFinisher: true,
                    dashCancel: true, jumpCancel: false),

                // 3: Quick Charged Shot (L2→H branch, standalone)
                MakeStep(
                    LoadAttack(af, "ViperQuickCharged"), AttackType.Heavy,
                    "attack_branch_heavy", nextL: -1, nextH: -1,
                    dashCancel: true, jumpCancel: false),

                // 4: H1 Charged Shot
                MakeStep(
                    LoadAttack(af, "ViperChargedShot"), AttackType.Heavy,
                    "attack_heavy_1", nextL: -1, nextH: 5,
                    dashCancel: true, jumpCancel: false),

                // 5: H2 Piercing Shot (finisher)
                MakeStep(
                    LoadAttack(af, "ViperPiercingShot"), AttackType.Heavy,
                    "finisher_heavy", isFinisher: true,
                    dashCancel: true, jumpCancel: false),
            };

            SaveAsset(def, path);
            Debug.Log($"[CreateComboDefinitions] Viper_ComboDefinition (6 steps) at {path}");
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static ComboStep MakeStep(
            AttackData attackData, AttackType attackType, string animTrigger,
            int nextL = -1, int nextH = -1,
            bool dashCancel = false, bool jumpCancel = false,
            bool isFinisher = false, float comboWindow = 0f)
        {
            return new ComboStep
            {
                attackData          = attackData,
                attackType          = attackType,
                animationTrigger    = animTrigger,
                damageMultiplier    = 1.0f,
                comboWindowDuration = comboWindow,
                nextOnLight         = nextL,
                nextOnHeavy         = nextH,
                canDashCancelOnHit  = dashCancel,
                canJumpCancelOnHit  = jumpCancel,
                isFinisher          = isFinisher
            };
        }

        private static AttackData LoadAttack(string folder, string fileName)
        {
            string path = $"{folder}/{fileName}.asset";
            var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (attack == null)
            {
                hadErrors = true;
                Debug.LogError(
                    $"[CreateComboDefinitions] AttackData not found at {path}! " +
                    "Run 'Tools > TomatoFighters > Create All Character Attacks' FIRST.");
            }
            return attack;
        }

        /// <summary>
        /// Loads an existing asset at the path (preserving its GUID) or creates a new instance.
        /// </summary>
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[CreateComboDefinitions] Updating existing asset at {path}");
                return existing;
            }
            return ScriptableObject.CreateInstance<T>();
        }

        /// <summary>
        /// If the asset is new (not yet on disk), creates it. If it already existed, marks it dirty so changes are saved.
        /// </summary>
        private static void SaveAsset(ScriptableObject asset, string path)
        {
            if (!AssetDatabase.Contains(asset))
            {
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }
        }

        private struct InteractionParams
        {
            public CancelPriority cancelPriority;
            public bool dashCancelResetsCombo;
            public bool jumpCancelResetsCombo;
            public bool resetOnStagger;
            public bool resetOnDeath;
            public bool lockMovementDuringAttack;
            public bool lockMovementDuringFinisher;
        }

        private static void CreateInteractionConfig(string fileName, InteractionParams p)
        {
            string path = $"{CONFIG_FOLDER}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ComboInteractionConfig>(path) != null)
            {
                Debug.Log($"[CreateComboDefinitions] {fileName} already exists, skipping.");
                return;
            }

            var config = ScriptableObject.CreateInstance<ComboInteractionConfig>();
            config.cancelPriority            = p.cancelPriority;
            config.dashCancelResetsCombo     = p.dashCancelResetsCombo;
            config.jumpCancelResetsCombo     = p.jumpCancelResetsCombo;
            config.resetOnStagger            = p.resetOnStagger;
            config.resetOnDeath              = p.resetOnDeath;
            config.lockMovementDuringAttack  = p.lockMovementDuringAttack;
            config.lockMovementDuringFinisher = p.lockMovementDuringFinisher;

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"[CreateComboDefinitions] Created {fileName} at {path}");
        }

        /// <summary>
        /// Finds the Player prefab, reads which ComboDefinition is assigned,
        /// and wires the matching ComboInteractionConfig via SerializedObject
        /// so the prefab reference is saved correctly.
        /// </summary>
        private static void WirePlayerPrefab()
        {
            const string prefabPath = "Assets/Prefabs/Player/Player.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CreateComboDefinitions] Player prefab not found at {prefabPath}, skipping auto-wire.");
                return;
            }

            var controller = prefab.GetComponent<ComboController>();
            if (controller == null)
            {
                Debug.LogWarning("[CreateComboDefinitions] No ComboController on Player prefab, skipping auto-wire.");
                return;
            }

            // Use SerializedObject to read/write private [SerializeField] fields on the prefab
            var so = new SerializedObject(controller);
            var defProp = so.FindProperty("comboDefinition");
            var configProp = so.FindProperty("interactionConfig");

            if (defProp.objectReferenceValue == null)
            {
                Debug.LogWarning("[CreateComboDefinitions] No ComboDefinition assigned on Player prefab. Assign one first.");
                return;
            }

            // Extract character name from definition asset name (e.g. "Mystica_ComboDefinition" → "Mystica")
            string defName = defProp.objectReferenceValue.name;
            string characterPrefix = defName.Split('_')[0];

            string configPath = $"{CONFIG_FOLDER}/{characterPrefix}_ComboInteractionConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<ComboInteractionConfig>(configPath);

            if (config == null)
            {
                Debug.LogError(
                    $"[CreateComboDefinitions] Could not find {configPath} to match {defName}!");
                return;
            }

            if (configProp.objectReferenceValue == config)
            {
                Debug.Log($"[CreateComboDefinitions] Player prefab already has {config.name} assigned.");
                return;
            }

            configProp.objectReferenceValue = config;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab);
            Debug.Log($"[CreateComboDefinitions] Wired {config.name} to Player prefab (matched from {defName}).");
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
