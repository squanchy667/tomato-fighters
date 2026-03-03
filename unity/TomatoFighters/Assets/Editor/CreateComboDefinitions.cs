using TomatoFighters.Combat;
using TomatoFighters.Shared.Data;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates ComboDefinition and ComboInteractionConfig assets for all characters.
    /// Brutor's ComboDefinition already exists from T004 — only his interaction config is created.
    /// Run once via menu: <b>Tools &gt; TomatoFighters &gt; Create Combo Definitions</b>.
    /// Depends on attack assets existing (run Create All Character Attacks first).
    /// </summary>
    public static class CreateComboDefinitions
    {
        private const string COMBO_FOLDER = "Assets/ScriptableObjects/ComboDefinitions";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/ComboInteractionConfigs";
        private const string ATTACKS_FOLDER = "Assets/ScriptableObjects/Attacks";

        [MenuItem("Tools/TomatoFighters/Create Combo Definitions")]
        public static void Execute()
        {
            EnsureFolderExists(COMBO_FOLDER);
            EnsureFolderExists(CONFIG_FOLDER);

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
            AssetDatabase.Refresh();
            Debug.Log("[CreateComboDefinitions] Done. Created combo definitions and interaction configs.");
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
            if (AssetDatabase.LoadAssetAtPath<ComboDefinition>(path) != null)
            {
                Debug.Log("[CreateComboDefinitions] Slasher_ComboDefinition already exists, skipping.");
                return;
            }

            string af = $"{ATTACKS_FOLDER}/Slasher";

            var def = ScriptableObject.CreateInstance<ComboDefinition>();
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

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[CreateComboDefinitions] Created Slasher_ComboDefinition (8 steps) at {path}");
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
            if (AssetDatabase.LoadAssetAtPath<ComboDefinition>(path) != null)
            {
                Debug.Log("[CreateComboDefinitions] Mystica_ComboDefinition already exists, skipping.");
                return;
            }

            string af = $"{ATTACKS_FOLDER}/Mystica";

            var def = ScriptableObject.CreateInstance<ComboDefinition>();
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

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[CreateComboDefinitions] Created Mystica_ComboDefinition (5 steps) at {path}");
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
            if (AssetDatabase.LoadAssetAtPath<ComboDefinition>(path) != null)
            {
                Debug.Log("[CreateComboDefinitions] Viper_ComboDefinition already exists, skipping.");
                return;
            }

            string af = $"{ATTACKS_FOLDER}/Viper";

            var def = ScriptableObject.CreateInstance<ComboDefinition>();
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

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[CreateComboDefinitions] Created Viper_ComboDefinition (6 steps) at {path}");
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
                Debug.LogWarning(
                    $"[CreateComboDefinitions] AttackData not found at {path}. " +
                    "Run 'Create All Character Attacks' first.");
            }
            return attack;
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
