using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates AttackData assets for Brutor, Slasher, and Viper.
    /// Mystica's base attacks exist from T005 (CreateMysticaAttacks.cs);
    /// only her empowered bolt finisher is added here.
    /// Run once via menu: <b>Tools &gt; TomatoFighters &gt; Create All Character Attacks</b>.
    /// </summary>
    public static class CreateAllCharacterAttacks
    {
        private const string BASE_FOLDER = "Assets/ScriptableObjects/Attacks";

        [MenuItem("Tools/TomatoFighters/Create All Character Attacks")]
        public static void Execute()
        {
            int created = 0;
            created += CreateBrutorAttacks();
            created += CreateSlasherAttacks();
            created += CreateViperAttacks();
            created += CreateMysticaExtras();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CreateAllCharacterAttacks] Done. Created {created} new attack assets.");
        }

        // ── Brutor (7 attacks) ─────────────────────────────────────────────

        private static int CreateBrutorAttacks()
        {
            string folder = $"{BASE_FOLDER}/Brutor";
            EnsureFolderExists(folder);
            int count = 0;

            // L1: Shield Bash 1
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "BrutorShieldBash1",
                attackId           = "brutor_shield_bash_1",
                attackName         = "Shield Bash 1",
                damageMultiplier   = 0.8f,
                knockbackForce     = new Vector2(2.5f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 4,
                hitboxActiveFrames = 5,
                totalFrames        = 20,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L2: Shield Bash 2
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "BrutorShieldBash2",
                attackId           = "brutor_shield_bash_2",
                attackName         = "Shield Bash 2",
                damageMultiplier   = 0.9f,
                knockbackForce     = new Vector2(3.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 4,
                hitboxActiveFrames = 5,
                totalFrames        = 22,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L3: Sweep Finisher — causesWallBounce
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "BrutorSweep",
                attackId           = "brutor_sweep",
                attackName         = "Shield Sweep",
                damageMultiplier   = 1.3f,
                knockbackForce     = new Vector2(4.0f, 0.5f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 5,
                hitboxActiveFrames = 6,
                totalFrames        = 26,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesWallBounce   = true
            });

            // L→H branch: Uppercut Launcher — causesLaunch
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "BrutorLauncher",
                attackId           = "brutor_launcher",
                attackName         = "Uppercut Launcher",
                damageMultiplier   = 1.2f,
                knockbackForce     = new Vector2(1.0f, 0f),
                launchForce        = new Vector2(0f, 5.0f),
                hitboxStartFrame   = 5,
                hitboxActiveFrames = 5,
                totalFrames        = 24,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesLaunch       = true
            });

            // L→H→H follow-up: Air Slam
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "BrutorLauncherSlam",
                attackId           = "brutor_launcher_slam",
                attackName         = "Air Slam",
                damageMultiplier   = 1.5f,
                knockbackForce     = new Vector2(3.0f, -2.0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 6,
                hitboxActiveFrames = 6,
                totalFrames        = 28,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                isAirAttack        = true
            });

            // H1: Overhead Slam — isOTGCapable
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "BrutorOverheadSlam",
                attackId           = "brutor_overhead_slam",
                attackName         = "Overhead Slam",
                damageMultiplier   = 1.2f,
                knockbackForce     = new Vector2(2.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 6,
                hitboxActiveFrames = 6,
                totalFrames        = 24,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                isOTGCapable       = true
            });

            // H2: Ground Pound Finisher
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "BrutorGroundPound",
                attackId           = "brutor_ground_pound",
                attackName         = "Ground Pound",
                damageMultiplier   = 1.5f,
                knockbackForce     = new Vector2(5.0f, 1.0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 7,
                hitboxActiveFrames = 7,
                totalFrames        = 30,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            Debug.Log($"[CreateAllCharacterAttacks] Brutor: {count} attacks created.");
            return count;
        }

        // ── Slasher (8 attacks) ────────────────────────────────────────────

        private static int CreateSlasherAttacks()
        {
            string folder = $"{BASE_FOLDER}/Slasher";
            EnsureFolderExists(folder);
            int count = 0;

            // L1: Quick Slash 1
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherSlash1",
                attackId           = "slasher_slash_1",
                attackName         = "Quick Slash 1",
                damageMultiplier   = 0.6f,
                knockbackForce     = new Vector2(1.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 2,
                hitboxActiveFrames = 3,
                totalFrames        = 12,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L2: Quick Slash 2
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherSlash2",
                attackId           = "slasher_slash_2",
                attackName         = "Quick Slash 2",
                damageMultiplier   = 0.7f,
                knockbackForce     = new Vector2(1.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 2,
                hitboxActiveFrames = 3,
                totalFrames        = 13,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L3: Cross Slash
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherSlash3",
                attackId           = "slasher_slash_3",
                attackName         = "Cross Slash",
                damageMultiplier   = 0.8f,
                knockbackForce     = new Vector2(1.5f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 2,
                hitboxActiveFrames = 4,
                totalFrames        = 14,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L4: Spinning Finisher — causesLaunch
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherSpinFinisher",
                attackId           = "slasher_spin_finisher",
                attackName         = "Spinning Finisher",
                damageMultiplier   = 1.2f,
                knockbackForce     = new Vector2(2.0f, 1.0f),
                launchForce        = new Vector2(0f, 3.0f),
                hitboxStartFrame   = 3,
                hitboxActiveFrames = 5,
                totalFrames        = 18,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesLaunch       = true
            });

            // L2→H branch: Lunge Thrust — causesWallBounce
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherLunge",
                attackId           = "slasher_lunge",
                attackName         = "Lunge Thrust",
                damageMultiplier   = 1.0f,
                knockbackForce     = new Vector2(3.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 3,
                hitboxActiveFrames = 4,
                totalFrames        = 16,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesWallBounce   = true
            });

            // H1: Heavy Slash
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherHeavySlash",
                attackId           = "slasher_heavy_slash",
                attackName         = "Heavy Slash",
                damageMultiplier   = 1.0f,
                knockbackForce     = new Vector2(2.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 3,
                hitboxActiveFrames = 4,
                totalFrames        = 16,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // H2: Piercing Lunge Finisher — causesWallBounce
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherLungeFinisher",
                attackId           = "slasher_lunge_finisher",
                attackName         = "Piercing Lunge",
                damageMultiplier   = 1.4f,
                knockbackForce     = new Vector2(4.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 4,
                hitboxActiveFrames = 5,
                totalFrames        = 20,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesWallBounce   = true
            });

            // H1→L branch: Quick Re-entry Slash (re-enters light chain)
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "SlasherQuickSlash",
                attackId           = "slasher_quick_slash",
                attackName         = "Quick Re-entry Slash",
                damageMultiplier   = 0.7f,
                knockbackForce     = new Vector2(1.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 2,
                hitboxActiveFrames = 3,
                totalFrames        = 12,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            Debug.Log($"[CreateAllCharacterAttacks] Slasher: {count} attacks created.");
            return count;
        }

        // ── Viper (6 attacks) ──────────────────────────────────────────────

        private static int CreateViperAttacks()
        {
            string folder = $"{BASE_FOLDER}/Viper";
            EnsureFolderExists(folder);
            int count = 0;

            // L1: Quick Shot 1
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "ViperShot1",
                attackId           = "viper_shot_1",
                attackName         = "Quick Shot 1",
                damageMultiplier   = 0.7f,
                knockbackForce     = new Vector2(0.5f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 2,
                hitboxActiveFrames = 3,
                totalFrames        = 14,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L2: Quick Shot 2
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "ViperShot2",
                attackId           = "viper_shot_2",
                attackName         = "Quick Shot 2",
                damageMultiplier   = 0.8f,
                knockbackForce     = new Vector2(0.5f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 2,
                hitboxActiveFrames = 3,
                totalFrames        = 15,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L3: Rapid Burst Finisher
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "ViperRapidBurst",
                attackId           = "viper_rapid_burst",
                attackName         = "Rapid Burst",
                damageMultiplier   = 1.1f,
                knockbackForce     = new Vector2(1.0f, 0.5f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 3,
                hitboxActiveFrames = 5,
                totalFrames        = 18,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // L2→H branch: Quick Charged Shot
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "ViperQuickCharged",
                attackId           = "viper_quick_charged",
                attackName         = "Quick Charged Shot",
                damageMultiplier   = 1.2f,
                knockbackForce     = new Vector2(2.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 3,
                hitboxActiveFrames = 4,
                totalFrames        = 16,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // H1: Charged Shot
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "ViperChargedShot",
                attackId           = "viper_charged_shot",
                attackName         = "Charged Shot",
                damageMultiplier   = 1.2f,
                knockbackForce     = new Vector2(2.0f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 5,
                hitboxActiveFrames = 5,
                totalFrames        = 20,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            // H2: Piercing Shot Finisher
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "ViperPiercingShot",
                attackId           = "viper_piercing_shot",
                attackName         = "Piercing Shot",
                damageMultiplier   = 1.5f,
                knockbackForce     = new Vector2(1.5f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 5,
                hitboxActiveFrames = 6,
                totalFrames        = 22,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            Debug.Log($"[CreateAllCharacterAttacks] Viper: {count} attacks created.");
            return count;
        }

        // ── Mystica extras (1 attack — empowered bolt finisher) ────────────

        private static int CreateMysticaExtras()
        {
            string folder = $"{BASE_FOLDER}/Mystica";
            EnsureFolderExists(folder);
            int count = 0;

            // H2: Empowered Arcane Bolt Finisher
            count += CreateAttack(folder, new AttackParams
            {
                fileName           = "MysticaEmpoweredBolt",
                attackId           = "mystica_empowered_bolt",
                attackName         = "Empowered Arcane Bolt",
                damageMultiplier   = 1.6f,
                knockbackForce     = new Vector2(3.0f, 1.5f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 7,
                hitboxActiveFrames = 7,
                totalFrames        = 26,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal
            });

            Debug.Log($"[CreateAllCharacterAttacks] Mystica extras: {count} attacks created.");
            return count;
        }

        // ── Shared helpers ─────────────────────────────────────────────────

        private struct AttackParams
        {
            public string fileName;
            public string attackId;
            public string attackName;
            public float damageMultiplier;
            public Vector2 knockbackForce;
            public Vector2 launchForce;
            public int hitboxStartFrame;
            public int hitboxActiveFrames;
            public int totalFrames;
            public float animationSpeed;
            public TelegraphType telegraphType;
            public bool causesWallBounce;
            public bool causesLaunch;
            public bool isOTGCapable;
            public bool isAirAttack;
            public float clashWindowStart;
            public float clashWindowEnd;
        }

        /// <returns>1 if created, 0 if skipped (already exists).</returns>
        private static int CreateAttack(string folder, AttackParams p)
        {
            string path = $"{folder}/{p.fileName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (existing != null)
            {
                Debug.Log($"[CreateAllCharacterAttacks] {p.fileName} already exists, skipping.");
                return 0;
            }

            var attack = ScriptableObject.CreateInstance<AttackData>();

            attack.attackId           = p.attackId;
            attack.attackName         = p.attackName;
            attack.damageMultiplier   = p.damageMultiplier;
            attack.knockbackForce     = p.knockbackForce;
            attack.launchForce        = p.launchForce;
            attack.hitboxStartFrame   = p.hitboxStartFrame;
            attack.hitboxActiveFrames = p.hitboxActiveFrames;
            attack.totalFrames        = p.totalFrames;
            attack.animationSpeed     = p.animationSpeed;
            attack.telegraphType      = p.telegraphType;
            attack.causesWallBounce   = p.causesWallBounce;
            attack.causesLaunch       = p.causesLaunch;
            attack.isOTGCapable       = p.isOTGCapable;
            attack.isAirAttack        = p.isAirAttack;
            attack.clashWindowStart   = p.clashWindowStart;
            attack.clashWindowEnd     = p.clashWindowEnd;

            AssetDatabase.CreateAsset(attack, path);
            Debug.Log($"[CreateAllCharacterAttacks] Created {p.fileName} at {path}");
            return 1;
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
