using System.IO;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creator Script that generates all 24 InspirationData ScriptableObjects.
    /// 4 characters x 3 paths x 2 per path (1 stat + 1 ability).
    /// </summary>
    public static class CreateInspirationAssets
    {
        private const string OUTPUT_DIR = "Assets/ScriptableObjects/Inspirations";

        [MenuItem("TomatoFighters/Create Inspiration Assets")]
        public static void CreateAll()
        {
            EnsureDirectory(OUTPUT_DIR);

            // ── Brutor ───────────────────────────────────────────────────────
            CreateStat("brutor_warden_stat",    "Warden's Fortitude",       "Warden training toughens the body.",
                CharacterType.Brutor, PathType.Warden,
                StatType.Health, ModifierType.Percent, 0.10f, 3);

            CreateAbility("brutor_warden_ability", "Extended Taunt",        "Taunt duration is extended.",
                CharacterType.Brutor, PathType.Warden,
                "warden_taunt_extended", 3);

            CreateStat("brutor_bulwark_stat",   "Bulwark's Armor",          "Bulwark training reinforces defenses.",
                CharacterType.Brutor, PathType.Bulwark,
                StatType.Defense, ModifierType.Percent, 0.15f, 4);

            CreateAbility("brutor_bulwark_ability", "Iron Reflect",         "Deflects send back a damaging pulse.",
                CharacterType.Brutor, PathType.Bulwark,
                "bulwark_iron_reflect", 4);

            CreateStat("brutor_guardian_stat",  "Guardian's Resilience",    "Guardian training balances toughness and vitality.",
                CharacterType.Brutor, PathType.Guardian,
                StatType.Defense, ModifierType.Percent, 0.08f, 5);

            CreateAbility("brutor_guardian_ability", "Shield Pulse",        "Shield emits a protective pulse on activation.",
                CharacterType.Brutor, PathType.Guardian,
                "guardian_shield_pulse", 5);

            // ── Slasher ──────────────────────────────────────────────────────
            CreateStat("slasher_executioner_stat", "Executioner's Edge",    "Executioner training sharpens strikes.",
                CharacterType.Slasher, PathType.Executioner,
                StatType.Attack, ModifierType.Percent, 0.12f, 3);

            CreateAbility("slasher_executioner_ability", "Mark Spread",     "Marked targets spread marks to nearby enemies on death.",
                CharacterType.Slasher, PathType.Executioner,
                "executioner_mark_spread", 3);

            CreateStat("slasher_reaper_stat",   "Reaper's Hunger",          "Reaper training hones killer instinct.",
                CharacterType.Slasher, PathType.Reaper,
                StatType.Attack, ModifierType.Percent, 0.08f, 4);

            CreateAbility("slasher_reaper_ability", "Cleave Lifesteal",    "Cleave attacks restore health on kill.",
                CharacterType.Slasher, PathType.Reaper,
                "reaper_cleave_lifesteal", 4);

            CreateStat("slasher_shadow_stat",   "Shadow's Swiftness",       "Shadow training quickens movement.",
                CharacterType.Slasher, PathType.Shadow,
                StatType.Speed, ModifierType.Percent, 0.10f, 3);

            CreateAbility("slasher_shadow_ability", "Phase Counter",       "Dodging triggers a counter-attack.",
                CharacterType.Slasher, PathType.Shadow,
                "shadow_phase_counter", 3);

            // ── Mystica ──────────────────────────────────────────────────────
            CreateStat("mystica_sage_stat",     "Sage's Flow",              "Sage training accelerates mana recovery.",
                CharacterType.Mystica, PathType.Sage,
                StatType.ManaRegen, ModifierType.Percent, 0.15f, 4);

            CreateAbility("mystica_sage_ability", "Heal Over Time",        "Healing spells leave a regeneration effect.",
                CharacterType.Mystica, PathType.Sage,
                "sage_heal_overtime", 4);

            CreateStat("mystica_enchanter_stat", "Enchanter's Reserve",    "Enchanter training expands mana pool.",
                CharacterType.Mystica, PathType.Enchanter,
                StatType.Mana, ModifierType.Percent, 0.10f, 3);

            CreateAbility("mystica_enchanter_ability", "Dual Buff",        "Buff spells apply to both self and nearest ally.",
                CharacterType.Mystica, PathType.Enchanter,
                "enchanter_dual_buff", 3);

            CreateStat("mystica_conjurer_stat", "Conjurer's Depth",        "Conjurer training deepens mana reserves.",
                CharacterType.Mystica, PathType.Conjurer,
                StatType.Mana, ModifierType.Percent, 0.08f, 5);

            CreateAbility("mystica_conjurer_ability", "Summon Evolve",     "Summoned creatures evolve after surviving long enough.",
                CharacterType.Mystica, PathType.Conjurer,
                "conjurer_summon_evolve", 5);

            // ── Viper ────────────────────────────────────────────────────────
            CreateStat("viper_marksman_stat",   "Marksman's Precision",     "Marksman training sharpens ranged attacks.",
                CharacterType.Viper, PathType.Marksman,
                StatType.RangedAttack, ModifierType.Percent, 0.12f, 3);

            CreateAbility("viper_marksman_ability", "Piercing Crit",       "Critical ranged hits pierce through to the next target.",
                CharacterType.Viper, PathType.Marksman,
                "marksman_piercing_crit", 3);

            CreateStat("viper_trapper_stat",    "Trapper's Evasion",        "Trapper training improves defensive mobility.",
                CharacterType.Viper, PathType.Trapper,
                StatType.Defense, ModifierType.Percent, 0.08f, 4);

            CreateAbility("viper_trapper_ability", "Net Poison",           "Trap nets apply a poison effect on capture.",
                CharacterType.Viper, PathType.Trapper,
                "trapper_net_poison", 4);

            CreateStat("viper_arcanist_stat",   "Arcanist's Attunement",    "Arcanist training expands mana capacity.",
                CharacterType.Viper, PathType.Arcanist,
                StatType.Mana, ModifierType.Percent, 0.10f, 3);

            CreateAbility("viper_arcanist_ability", "Charge Chain",        "Charged shots chain to a second nearby target.",
                CharacterType.Viper, PathType.Arcanist,
                "arcanist_charge_chain", 3);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateInspirationAssets] Created 24 InspirationData assets.");
        }

        private static void CreateStat(
            string id, string displayName, string description,
            CharacterType character, PathType path,
            StatType statType, ModifierType modType, float value,
            int permanentCost)
        {
            var asset = ScriptableObject.CreateInstance<InspirationData>();
            asset.inspirationId = id;
            asset.displayName = displayName;
            asset.description = description;
            asset.character = character;
            asset.path = path;
            asset.effectType = InspirationEffectType.StatModifier;
            asset.statType = statType;
            asset.modifierType = modType;
            asset.value = value;
            asset.permanentUnlockCost = permanentCost;

            SaveAsset(asset, id);
        }

        private static void CreateAbility(
            string id, string displayName, string description,
            CharacterType character, PathType path,
            string abilityModifierId, int permanentCost)
        {
            var asset = ScriptableObject.CreateInstance<InspirationData>();
            asset.inspirationId = id;
            asset.displayName = displayName;
            asset.description = description;
            asset.character = character;
            asset.path = path;
            asset.effectType = InspirationEffectType.AbilityModifier;
            asset.abilityModifierId = abilityModifierId;
            asset.permanentUnlockCost = permanentCost;

            SaveAsset(asset, id);
        }

        private static void SaveAsset(InspirationData asset, string id)
        {
            string path = $"{OUTPUT_DIR}/{id}.asset";

            // Delete existing to allow re-creation
            if (File.Exists(path))
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureDirectory(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                string folderName = Path.GetFileName(dir);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
