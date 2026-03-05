using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates all 12 PathData ScriptableObject assets with the stat values
    /// and ability IDs from CHARACTER-ARCHETYPES.md.
    ///
    /// <para>Run once via <b>TomatoFighters → Create All Path Assets</b> in the Unity
    /// menu bar. Assets are written to
    /// <c>Assets/ScriptableObjects/Paths/{Character}/{PathName}Path.asset</c>.</para>
    ///
    /// <para>Re-running overwrites existing assets — safe to re-run if values change.</para>
    /// </summary>
    public static class PathDataCreator
    {
        private const string ROOT = "Assets/ScriptableObjects/Paths";

        [MenuItem("TomatoFighters/Create All Path Assets")]
        public static void CreateAllPathAssets()
        {
            EnsureFolder(ROOT);

            CreateBrutorPaths();
            CreateSlasherPaths();
            CreateMysticaPaths();
            CreateViperPaths();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[T008] All 12 PathData assets created at Assets/ScriptableObjects/Paths/");
        }

        // ── Brutor ───────────────────────────────────────────────────────────

        private static void CreateBrutorPaths()
        {
            EnsureFolder($"{ROOT}/Brutor");

            // ── Warden (Aggro) ────────────────────────────────────────────────
            // Theme: threat generation, taunt, damage-from-sustained-aggro
            Save(new PathConfig
            {
                pathType  = PathType.Warden,
                character = CharacterType.Brutor,
                description = "Threat generation and taunt mechanics. Brutor becomes a magnet " +
                              "that punishes enemies for ignoring him. T3: Wrath State on sustained damage.",

                t1Bonuses = new PathTierBonuses { healthBonus = 20, stunRateBonus = 0.2f },
                t1AbilityId = "Warden_Provoke",

                t2Bonuses = new PathTierBonuses { healthBonus = 30, attackBonus = 0.1f, stunRateBonus = 0.3f },
                t2AbilityId = "Warden_AggroAura",

                t3Bonuses = new PathTierBonuses { healthBonus = 40, attackBonus = 0.2f, stunRateBonus = 0.5f },
                t3AbilityId = "Warden_WrathOfTheWarden",
            }, "Brutor", "WardenPath");

            // ── Bulwark (Singular Defense) ────────────────────────────────────
            // Theme: personal invulnerability, counter-attacks, self-sustain
            Save(new PathConfig
            {
                pathType  = PathType.Bulwark,
                character = CharacterType.Brutor,
                description = "Personal invulnerability and counter-attacks. Block everything, " +
                              "counter-strike automatically. T3: Fortress — store and release damage.",

                t1Bonuses = new PathTierBonuses { healthBonus = 30, defenseBonus = 5 },
                t1AbilityId = "Bulwark_IronGuard",

                t2Bonuses = new PathTierBonuses { healthBonus = 40, defenseBonus = 8 },
                t2AbilityId = "Bulwark_Retaliation",

                t3Bonuses = new PathTierBonuses { healthBonus = 50, defenseBonus = 12, critChanceBonus = 0.05f },
                t3AbilityId = "Bulwark_Fortress",
            }, "Brutor", "BulwarkPath");

            // ── Guardian (Team Defense) ───────────────────────────────────────
            // Theme: AOE protection, damage redirection, team healing
            Save(new PathConfig
            {
                pathType  = PathType.Guardian,
                character = CharacterType.Brutor,
                description = "AOE protection and team healing. Redirect damage, heal allies, " +
                              "create safe zones. T3: Aegis Dome — protective shield for the team.",

                t1Bonuses = new PathTierBonuses { healthBonus = 25, defenseBonus = 3, manaBonus = 20 },
                t1AbilityId = "Guardian_ShieldLink",

                t2Bonuses = new PathTierBonuses { healthBonus = 35, defenseBonus = 5, manaBonus = 30 },
                t2AbilityId = "Guardian_RallyingPresence",

                t3Bonuses = new PathTierBonuses { healthBonus = 45, defenseBonus = 8, manaBonus = 40, manaRegenBonus = 2f },
                t3AbilityId = "Guardian_AegisDome",
            }, "Brutor", "GuardianPath");
        }

        // ── Slasher ──────────────────────────────────────────────────────────

        private static void CreateSlasherPaths()
        {
            EnsureFolder($"{ROOT}/Slasher");

            // ── Executioner (Mono/Single-Target) ──────────────────────────────
            // Theme: single-target burst, crit amplification, finisher damage
            Save(new PathConfig
            {
                pathType  = PathType.Executioner,
                character = CharacterType.Slasher,
                description = "Single-target burst and crit amplification. Mark priority targets, " +
                              "amplify damage on low-HP enemies. T3: Deathblow — 500–1000% ATK execution.",

                t1Bonuses = new PathTierBonuses { attackBonus = 0.3f, critChanceBonus = 0.05f },
                t1AbilityId = "Executioner_MarkForDeath",

                t2Bonuses = new PathTierBonuses { attackBonus = 0.4f, critChanceBonus = 0.08f },
                t2AbilityId = "Executioner_ExecutionThreshold",

                t3Bonuses = new PathTierBonuses { attackBonus = 0.5f, critChanceBonus = 0.12f, stunRateBonus = 0.3f },
                t3AbilityId = "Executioner_Deathblow",
            }, "Slasher", "ExecutionerPath");

            // ── Reaper (Multi-Target / AOE) ───────────────────────────────────
            // Theme: cleave, chain hits, crowd clearing
            Save(new PathConfig
            {
                pathType  = PathType.Reaper,
                character = CharacterType.Slasher,
                description = "Cleave and crowd clearing. All attacks hit in a wider arc. " +
                              "T3: Whirlwind — 4-second AOE spin that launches all nearby enemies.",

                t1Bonuses = new PathTierBonuses { healthBonus = 15, attackBonus = 0.2f },
                t1AbilityId = "Reaper_CleavingStrikes",

                t2Bonuses = new PathTierBonuses { healthBonus = 20, attackBonus = 0.3f, speedBonus = 0.1f },
                t2AbilityId = "Reaper_ChainSlash",

                t3Bonuses = new PathTierBonuses { healthBonus = 30, attackBonus = 0.4f, speedBonus = 0.2f },
                t3AbilityId = "Reaper_Whirlwind",
            }, "Slasher", "ReaperPath");

            // ── Shadow (Dexterity / Evasion) ──────────────────────────────────
            // Theme: dodge through enemies, i-frame extensions, hit-and-run
            Save(new PathConfig
            {
                pathType  = PathType.Shadow,
                character = CharacterType.Slasher,
                description = "Evasion and i-frame extensions. Dash charges, afterimage baits, " +
                              "backstabs. T3: Thousand Cuts — teleport through 6 enemies with guaranteed crits.",

                t1Bonuses = new PathTierBonuses { speedBonus = 0.2f, critChanceBonus = 0.05f },
                t1AbilityId = "Shadow_PhaseDash",

                t2Bonuses = new PathTierBonuses { healthBonus = 10, speedBonus = 0.3f, critChanceBonus = 0.08f },
                t2AbilityId = "Shadow_Afterimage",

                t3Bonuses = new PathTierBonuses { healthBonus = 20, speedBonus = 0.4f, critChanceBonus = 0.12f },
                t3AbilityId = "Shadow_ThousandCuts",
            }, "Slasher", "ShadowPath");
        }

        // ── Mystica ──────────────────────────────────────────────────────────

        private static void CreateMysticaPaths()
        {
            EnsureFolder($"{ROOT}/Mystica");

            // ── Sage (Healer) ─────────────────────────────────────────────────
            // Theme: sustain healing, burst healing, revival
            Save(new PathConfig
            {
                pathType  = PathType.Sage,
                character = CharacterType.Mystica,
                description = "Sustain healing and revival. Mending Aura regenerates ally HP. " +
                              "T3: Resurrection — revive a downed ally once per run.",

                t1Bonuses = new PathTierBonuses { healthBonus = 30, manaBonus = 20, manaRegenBonus = 2f },
                t1AbilityId = "Sage_MendingAura",

                t2Bonuses = new PathTierBonuses { healthBonus = 40, manaBonus = 30, manaRegenBonus = 3f },
                t2AbilityId = "Sage_PurifyingPresence",

                t3Bonuses = new PathTierBonuses { healthBonus = 50, manaBonus = 40, manaRegenBonus = 4f },
                t3AbilityId = "Sage_Resurrection",
            }, "Mystica", "SagePath");

            // ── Enchanter (Buffer) ────────────────────────────────────────────
            // Theme: stat buffs, elemental infusion, aura amplification
            Save(new PathConfig
            {
                pathType  = PathType.Enchanter,
                character = CharacterType.Mystica,
                description = "Stat buffs and elemental infusion. Empower allies, infuse attacks " +
                              "with the dominant ritual family. T3: Arcane Overdrive — double all buffs.",

                t1Bonuses = new PathTierBonuses { speedBonus = 0.1f, manaBonus = 20, manaRegenBonus = 2f },
                t1AbilityId = "Enchanter_Empower",

                t2Bonuses = new PathTierBonuses { healthBonus = 15, speedBonus = 0.1f, manaBonus = 30, manaRegenBonus = 3f },
                t2AbilityId = "Enchanter_ElementalInfusion",

                t3Bonuses = new PathTierBonuses { healthBonus = 25, speedBonus = 0.2f, manaBonus = 40, manaRegenBonus = 4f },
                t3AbilityId = "Enchanter_ArcaneOverdrive",
            }, "Mystica", "EnchanterPath");

            // ── Conjurer (Summoner) ───────────────────────────────────────────
            // Theme: summon minions, deploy totems, construct turrets
            Save(new PathConfig
            {
                pathType  = PathType.Conjurer,
                character = CharacterType.Mystica,
                description = "Summon minions and deploy totems. Build an army to compensate " +
                              "for low HP. T3: Summon Golem — massive tank that protects Mystica.",

                t1Bonuses = new PathTierBonuses { healthBonus = 20, attackBonus = 0.1f, manaBonus = 20 },
                t1AbilityId = "Conjurer_SummonSproutling",

                t2Bonuses = new PathTierBonuses { healthBonus = 30, attackBonus = 0.2f, manaBonus = 30 },
                t2AbilityId = "Conjurer_TotemPulse",

                t3Bonuses = new PathTierBonuses { healthBonus = 40, attackBonus = 0.3f, manaBonus = 40, manaRegenBonus = 2f },
                t3AbilityId = "Conjurer_SummonGolem",
            }, "Mystica", "ConjurerPath");
        }

        // ── Viper ────────────────────────────────────────────────────────────

        private static void CreateViperPaths()
        {
            EnsureFolder($"{ROOT}/Viper");

            // ── Marksman (Pure Ranged Damage) ─────────────────────────────────
            // Theme: raw ranged DPS, projectile enhancement, crit stacking
            Save(new PathConfig
            {
                pathType  = PathType.Marksman,
                character = CharacterType.Viper,
                description = "Raw ranged DPS and crit stacking. Piercing shots, Rapid Fire bursts. " +
                              "T3: Killshot — 800% RATK sniper shot that continues through all enemies on kill.",

                t1Bonuses = new PathTierBonuses { rangedAttackBonus = 0.3f, critChanceBonus = 0.05f },
                t1AbilityId = "Marksman_PiercingShots",

                t2Bonuses = new PathTierBonuses { healthBonus = 10, rangedAttackBonus = 0.4f, critChanceBonus = 0.08f },
                t2AbilityId = "Marksman_RapidVolleys",

                t3Bonuses = new PathTierBonuses { rangedAttackBonus = 0.5f, speedBonus = 0.1f, critChanceBonus = 0.12f },
                t3AbilityId = "Marksman_Killshot",
            }, "Viper", "MarksmanPath");

            // ── Trapper (Harpoon / Crowd Control) ────────────────────────────
            // Theme: stun, immobilize, pull, area denial
            Save(new PathConfig
            {
                pathType  = PathType.Trapper,
                character = CharacterType.Viper,
                description = "Crowd control and pressure building. Harpoon immobilizes, Trap Net " +
                              "snares. T3: Anchor Chain — pulls all enemies in radius and debuffs them.",

                t1Bonuses = new PathTierBonuses { healthBonus = 20, speedBonus = 0.1f, stunRateBonus = 0.2f },
                t1AbilityId = "Trapper_HarpoonShot",

                t2Bonuses = new PathTierBonuses { healthBonus = 30, speedBonus = 0.2f, stunRateBonus = 0.3f },
                t2AbilityId = "Trapper_TrapDeployment",

                t3Bonuses = new PathTierBonuses { healthBonus = 40, speedBonus = 0.2f, manaBonus = 15, stunRateBonus = 0.5f },
                t3AbilityId = "Trapper_AnchorChain",
            }, "Viper", "TrapperPath");

            // ── Arcanist (Mana Charge / Mana Attacks) ────────────────────────
            // Theme: mana as resource AND weapon, charge for devastating attacks
            Save(new PathConfig
            {
                pathType  = PathType.Arcanist,
                character = CharacterType.Viper,
                description = "Mana as a weapon. Charge for devastating Mana Blasts. High risk/reward. " +
                              "T3: Mana Overload — instant 100% charge with 2x damage for 10 seconds.",

                t1Bonuses = new PathTierBonuses { manaBonus = 30, manaRegenBonus = 3f },
                t1AbilityId = "Arcanist_ManaCharge",

                t2Bonuses = new PathTierBonuses { rangedAttackBonus = 0.2f, manaBonus = 40, manaRegenBonus = 4f },
                t2AbilityId = "Arcanist_ManaBlast",

                t3Bonuses = new PathTierBonuses { rangedAttackBonus = 0.3f, manaBonus = 50, manaRegenBonus = 5f, critChanceBonus = 0.05f },
                t3AbilityId = "Arcanist_ManaOverload",
            }, "Viper", "ArcanistPath");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates or overwrites a PathData asset at
        /// <c>Assets/ScriptableObjects/Paths/{subfolder}/{fileName}.asset</c>.
        /// </summary>
        private static void Save(PathConfig cfg, string subfolder, string fileName)
        {
            var data = ScriptableObject.CreateInstance<PathData>();
            data.pathType    = cfg.pathType;
            data.character   = cfg.character;
            data.description = cfg.description;

            data.tier1Bonuses   = cfg.t1Bonuses;
            data.tier1AbilityId = cfg.t1AbilityId;

            data.tier2Bonuses   = cfg.t2Bonuses;
            data.tier2AbilityId = cfg.t2AbilityId;

            data.tier3Bonuses   = cfg.t3Bonuses;
            data.tier3AbilityId = cfg.t3AbilityId;

            string path = $"{ROOT}/{subfolder}/{fileName}.asset";

            // Overwrite if already exists — safe to re-run
            var existing = AssetDatabase.LoadAssetAtPath<PathData>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(data, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(data);
                Debug.Log($"[T008] Updated: {path}");
            }
            else
            {
                AssetDatabase.CreateAsset(data, path);
                Debug.Log($"[T008] Created: {path}");
            }
        }

        /// <summary>Creates a folder at <paramref name="path"/> if it doesn't already exist.</summary>
        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int lastSlash = path.LastIndexOf('/');
                string parent = path[..lastSlash];
                string folderName = path[(lastSlash + 1)..];
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        // ── Inner config type (editor-only, keeps Save() call sites readable) ──

        private struct PathConfig
        {
            public PathType       pathType;
            public CharacterType  character;
            public string         description;
            public PathTierBonuses t1Bonuses;
            public string          t1AbilityId;
            public PathTierBonuses t2Bonuses;
            public string          t2AbilityId;
            public PathTierBonuses t3Bonuses;
            public string          t3AbilityId;
        }
    }
}
