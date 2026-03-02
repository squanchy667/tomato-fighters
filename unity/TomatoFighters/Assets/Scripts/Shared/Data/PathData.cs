using System;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Data container for one of the 12 character upgrade paths.
    ///
    /// <para>Holds three tiers of stat bonuses (<see cref="PathTierBonuses"/>) and one
    /// ability unlock ID per tier. Bonuses are stored as <b>incremental deltas</b> —
    /// each tier struct contains only what that tier adds. Call
    /// <see cref="GetStatBonusArray"/> to get the cumulative total up to a given tier,
    /// ready to drop into <c>StatModifierInput.pathBonuses</c>.</para>
    ///
    /// <para>Tier 3 is available only when this is the character's Main path.
    /// <c>PathSystem</c> enforces this constraint in behavior — no flag is needed here.</para>
    ///
    /// <para>Referenced via <see cref="TomatoFighters.Shared.Interfaces.IPathProvider"/>
    /// from all three pillars. Never cache directly — always query through the interface.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Data/Path Data", fileName = "NewPathData")]
    public class PathData : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Which of the 12 paths this asset represents.")]
        public PathType pathType;

        [Tooltip("Which character this path belongs to.")]
        public CharacterType character;

        [TextArea(2, 4)]
        [Tooltip("Designer-facing description shown in PathSelectionUI. Not used at runtime.")]
        public string description;

        // ── Tier 1 ───────────────────────────────────────────────────────────

        [Header("Tier 1")]
        [Tooltip("Stat deltas granted when this path is selected (Main or Secondary).")]
        public PathTierBonuses tier1Bonuses;

        [Tooltip("Ability ID unlocked at Tier 1. Checked by IPathProvider.IsPathAbilityUnlocked. " +
                 "Format: '{PathName}_{AbilityName}' e.g. 'Warden_Provoke'.")]
        public string tier1AbilityId;

        // ── Tier 2 ───────────────────────────────────────────────────────────

        [Header("Tier 2")]
        [Tooltip("Stat deltas added on top of Tier 1 when the area boss is defeated.")]
        public PathTierBonuses tier2Bonuses;

        [Tooltip("Ability ID unlocked at Tier 2.")]
        public string tier2AbilityId;

        // ── Tier 3 — Main Path Only ───────────────────────────────────────────
        // PathSystem (T018) enforces that Tier 3 is only granted when this is the Main path.
        // No flag is needed here — the constraint lives in behavior, not data.

        [Header("Tier 3 — Main Path Only")]
        [Tooltip("Stat deltas added on top of Tier 2 when the island boss is defeated. " +
                 "Main path only — PathSystem enforces this, no flag needed here.")]
        public PathTierBonuses tier3Bonuses;

        [Tooltip("Signature ability ID unlocked at Tier 3. Main path only.")]
        public string tier3AbilityId;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns a cumulative stat bonus array for the given tier (1–3), ready to
        /// assign to <c>StatModifierInput.pathBonuses</c>.
        ///
        /// <para>The array is indexed by <c>(int)StatType</c>. Tier 2 includes Tier 1
        /// bonuses; Tier 3 includes Tier 1 + Tier 2 bonuses (incremental accumulation).</para>
        ///
        /// <para><c>StatType.CancelWindow</c> is never set by paths — that index remains 0.</para>
        /// </summary>
        /// <param name="tier">Active tier: 1, 2, or 3. Values outside [1,3] return all zeros.</param>
        /// <returns>Float array of length <c>Enum.GetValues(typeof(StatType)).Length</c>.</returns>
        public float[] GetStatBonusArray(int tier)
        {
            // Avoid referencing StatModifierInput (Paths pillar) from Shared.
            // StatType enum length is the ground truth for array sizing.
            int count = Enum.GetValues(typeof(StatType)).Length;
            var bonuses = new float[count];

            if (tier >= 1) Accumulate(tier1Bonuses, bonuses);
            if (tier >= 2) Accumulate(tier2Bonuses, bonuses);
            if (tier >= 3) Accumulate(tier3Bonuses, bonuses);

            return bonuses;
        }

        /// <summary>
        /// Returns the ability ID unlocked at the given tier, or an empty string if the
        /// tier is out of range.
        /// </summary>
        /// <param name="tier">1, 2, or 3.</param>
        public string GetAbilityIdForTier(int tier) => tier switch
        {
            1 => tier1AbilityId,
            2 => tier2AbilityId,
            3 => tier3AbilityId,
            _ => string.Empty,
        };

        // ── Internal ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds all non-zero fields from <paramref name="tier"/> into <paramref name="bonuses"/>.
        /// Explicit per-stat mapping ensures correct StatType indices regardless of enum reordering.
        /// CancelWindow is intentionally omitted — no path grants cancel-window bonuses.
        /// </summary>
        private static void Accumulate(PathTierBonuses tier, float[] bonuses)
        {
            bonuses[(int)StatType.Health]          += tier.healthBonus;
            bonuses[(int)StatType.Defense]         += tier.defenseBonus;
            bonuses[(int)StatType.Attack]          += tier.attackBonus;
            bonuses[(int)StatType.RangedAttack]    += tier.rangedAttackBonus;
            bonuses[(int)StatType.ThrowableAttack] += tier.throwableAttackBonus;
            bonuses[(int)StatType.Speed]           += tier.speedBonus;
            bonuses[(int)StatType.Mana]            += tier.manaBonus;
            bonuses[(int)StatType.ManaRegen]       += tier.manaRegenBonus;
            bonuses[(int)StatType.CritChance]      += tier.critChanceBonus;
            bonuses[(int)StatType.StunRate]        += tier.stunRateBonus;
            // StatType.CancelWindow — no path grants cancel-window bonuses; omitted intentionally
        }
    }
}
