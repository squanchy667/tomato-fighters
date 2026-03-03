using System;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// The stat increments granted by one tier of a path upgrade.
    ///
    /// <para>Values are <b>deltas</b>, not cumulative totals. Each tier struct stores
    /// only the bonus that tier adds. Use <see cref="PathData.GetStatBonusArray"/> to
    /// retrieve the cumulative total up to a given tier.</para>
    ///
    /// <para>All fields default to zero — leave unset fields at 0 rather than explicitly
    /// zeroing them in the Inspector.</para>
    /// </summary>
    [Serializable]
    public struct PathTierBonuses
    {
        // ── Vitals ───────────────────────────────────────────────────────────

        [Header("Vitals")]
        [Tooltip("Flat bonus added to max HP. Integer.")]
        public int healthBonus;

        [Tooltip("Flat bonus added to DEF (damage reduction per hit). Integer.")]
        public int defenseBonus;

        // ── Attack ───────────────────────────────────────────────────────────

        [Header("Attack")]
        [Tooltip("Additive bonus to melee ATK multiplier.")]
        public float attackBonus;

        [Tooltip("Additive bonus to ranged ATK multiplier. Viper-path bonuses only — " +
                 "leave at 0 for all Brutor / Slasher / Mystica paths.")]
        public float rangedAttackBonus;

        [Tooltip("Additive bonus to throwable-item ATK multiplier. " +
                 "Leave at 0 unless a path explicitly grants throwable power.")]
        public float throwableAttackBonus;

        // ── Mobility ─────────────────────────────────────────────────────────

        [Header("Mobility")]
        [Tooltip("Additive bonus to SPD (movement speed + dash distance multiplier).")]
        public float speedBonus;

        // ── Mana ─────────────────────────────────────────────────────────────

        [Header("Mana")]
        [Tooltip("Flat bonus added to max MNA. Integer.")]
        public int manaBonus;

        [Tooltip("Additive bonus to MNA regeneration per second.")]
        public float manaRegenBonus;

        // ── Combat ───────────────────────────────────────────────────────────

        [Header("Combat")]
        [Range(0f, 0.25f)]
        [Tooltip("Additive bonus to crit chance. Stored as decimal: 0.05 = 5%.")]
        public float critChanceBonus;

        [Tooltip("Additive bonus to StunRate (pressure-meter fill speed). " +
                 "Known as 'PRS' in design docs — always StunRate in code.")]
        public float stunRateBonus;
    }
}
