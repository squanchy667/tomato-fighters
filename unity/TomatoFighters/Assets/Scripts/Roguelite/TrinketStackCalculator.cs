using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Pure C# calculator that computes per-stat multipliers from a list of active trinkets.
    /// Flat modifiers are converted to multipliers using base stats (DD-1).
    /// Percentage modifiers stack multiplicatively: 1.1 × 1.1 = 1.21 (not 1.2).
    /// No Unity dependencies — fully unit-testable.
    /// </summary>
    public static class TrinketStackCalculator
    {
        private static readonly int STAT_COUNT = Enum.GetValues(typeof(StatType)).Length;

        /// <summary>
        /// Computes a multiplier array (one entry per <see cref="StatType"/>) from active trinkets.
        /// Only trinkets with <paramref name="isActive"/> = true contribute.
        /// </summary>
        /// <param name="activeTrinkets">Currently equipped trinkets with activation state.</param>
        /// <param name="baseStats">Character base stats for flat-to-multiplier conversion.</param>
        /// <returns>Float array indexed by <c>(int)StatType</c>. Neutral value is 1.0f.</returns>
        public static float[] CalculateMultipliers(
            List<ActiveTrinketEntry> activeTrinkets,
            CharacterBaseStats baseStats)
        {
            float[] multipliers = new float[STAT_COUNT];
            for (int i = 0; i < STAT_COUNT; i++)
                multipliers[i] = 1.0f;

            if (activeTrinkets == null) return multipliers;

            foreach (var entry in activeTrinkets)
            {
                if (!entry.IsActive) continue;

                int idx = (int)entry.Data.affectedStat;

                if (entry.Data.modifierType == ModifierType.Percent)
                {
                    multipliers[idx] *= (1f + entry.Data.modifierValue);
                }
                else // Flat
                {
                    float baseVal = baseStats.GetStat(entry.Data.affectedStat);
                    if (baseVal > 0f)
                        multipliers[idx] *= (baseVal + entry.Data.modifierValue) / baseVal;
                    // Zero or negative base: skip to avoid divide-by-zero
                }
            }

            return multipliers;
        }
    }

    /// <summary>
    /// Runtime record for a single equipped trinket — wraps the <see cref="TrinketData"/> SO
    /// with activation state for conditional triggers.
    /// </summary>
    public class ActiveTrinketEntry
    {
        /// <summary>The trinket definition ScriptableObject.</summary>
        public TrinketData Data { get; }

        /// <summary>
        /// Whether this trinket's modifier is currently active.
        /// Always-type trinkets are permanently true; conditional trinkets
        /// become true on trigger and false when the buff expires.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>Remaining buff duration in seconds. Only meaningful for conditional trinkets.</summary>
        public float RemainingTime { get; set; }

        public ActiveTrinketEntry(TrinketData data)
        {
            Data = data;
            IsActive = data.triggerType == TrinketTriggerType.Always;
            RemainingTime = 0f;
        }
    }
}
