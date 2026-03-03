using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Sets the <see cref="AttackData.hitboxId"/> field on all AttackData assets
    /// based on their attackId. Maps each attack to a reusable hitbox shape name.
    /// Run via menu: <b>Tools > TomatoFighters > Assign All Hitbox IDs</b>.
    /// </summary>
    public static class AssignAllHitboxIds
    {
        // attackId → hitboxId mapping for all 26 attacks
        private static readonly Dictionary<string, string> HitboxMap = new()
        {
            // ── Mystica (5) ──────────────────────────────────────────────
            { "mystica_strike_1",       "Burst" },      // Magic Burst 1 — small circle
            { "mystica_strike_2",       "Burst" },      // Magic Burst 2 — small circle
            { "mystica_strike_3",       "BigBurst" },   // Magic Burst 3 — wide circle finisher
            { "mystica_arcane_bolt",    "Bolt" },       // Arcane Bolt — extended narrow box
            { "mystica_empowered_bolt", "Bolt" },       // Empowered Bolt — extended narrow box

            // ── Brutor (7) ───────────────────────────────────────────────
            { "brutor_shield_bash_1",   "Jab" },        // Shield Bash 1
            { "brutor_shield_bash_2",   "Jab" },        // Shield Bash 2
            { "brutor_sweep",           "Sweep" },      // Shield Sweep — wide horizontal
            { "brutor_launcher",        "Uppercut" },   // Uppercut Launcher — tall vertical
            { "brutor_launcher_slam",   "Slam" },       // Air Slam — circular impact
            { "brutor_overhead_slam",   "Slam" },       // Overhead Slam — circular impact
            { "brutor_ground_pound",    "Slam" },       // Ground Pound — circular impact

            // ── Slasher (8) ──────────────────────────────────────────────
            { "slasher_slash_1",        "Jab" },        // Quick Slash 1
            { "slasher_slash_2",        "Jab" },        // Quick Slash 2
            { "slasher_slash_3",        "Sweep" },      // Cross Slash — wide
            { "slasher_spin_finisher",  "Slam" },       // Spinning Finisher — circular
            { "slasher_lunge",          "Lunge" },      // Lunge Thrust — extended narrow
            { "slasher_heavy_slash",    "Sweep" },      // Heavy Slash — wide
            { "slasher_lunge_finisher", "Lunge" },      // Piercing Lunge — extended narrow
            { "slasher_quick_slash",    "Jab" },        // Quick Re-entry Slash

            // ── Viper (6) ────────────────────────────────────────────────
            { "viper_shot_1",           "Lunge" },      // Quick Shot 1 — ranged
            { "viper_shot_2",           "Lunge" },      // Quick Shot 2 — ranged
            { "viper_rapid_burst",      "Sweep" },      // Rapid Burst — spread
            { "viper_quick_charged",    "Lunge" },      // Quick Charged Shot — ranged
            { "viper_charged_shot",     "Lunge" },      // Charged Shot — ranged
            { "viper_piercing_shot",    "Lunge" },      // Piercing Shot — ranged
        };

        [MenuItem("Tools/TomatoFighters/Assign All Hitbox IDs")]
        public static void Execute()
        {
            string[] guids = AssetDatabase.FindAssets("t:AttackData");
            int updated = 0;
            int skipped = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);
                if (attack == null) continue;

                if (string.IsNullOrEmpty(attack.attackId))
                {
                    Debug.LogWarning($"[AssignHitboxIds] '{path}' has no attackId, skipping.");
                    skipped++;
                    continue;
                }

                if (HitboxMap.TryGetValue(attack.attackId, out string hitboxId))
                {
                    if (attack.hitboxId == hitboxId)
                    {
                        skipped++;
                        continue;
                    }

                    string old = string.IsNullOrEmpty(attack.hitboxId) ? "(empty)" : attack.hitboxId;
                    attack.hitboxId = hitboxId;
                    EditorUtility.SetDirty(attack);
                    Debug.Log($"[AssignHitboxIds] {attack.attackId} → hitboxId='{hitboxId}' (was '{old}')");
                    updated++;
                }
                else
                {
                    Debug.LogWarning(
                        $"[AssignHitboxIds] No mapping for attackId='{attack.attackId}' ({path}). " +
                        "Add it to the HitboxMap dictionary.");
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AssignHitboxIds] Done. Updated {updated}, skipped {skipped}.");
        }
    }
}
