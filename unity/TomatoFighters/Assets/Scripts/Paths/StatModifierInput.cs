using System;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Paths
{
    /// <summary>
    /// Bundles all modifier sources required by <see cref="CharacterStatCalculator"/>.
    ///
    /// <para>All arrays are indexed by <see cref="StatType"/> cast to int.
    /// Use <see cref="Default"/> to create a correctly initialized instance
    /// with no active modifiers (neutral values throughout).</para>
    ///
    /// <para>Note: <c>StatType.CancelWindow</c> is present in the arrays but has
    /// no corresponding base value in <see cref="CharacterBaseStats"/>. The calculator
    /// silently ignores that slot — leave it at the default neutral values.</para>
    /// </summary>
    public struct StatModifierInput
    {
        /// <summary>Total number of entries in each modifier array (one per StatType value).</summary>
        public static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        /// <summary>
        /// The character's base stat ScriptableObject (T006).
        /// Provides the starting values before any modifiers are applied.
        /// </summary>
        public CharacterBaseStats baseStats;

        /// <summary>
        /// Per-stat additive bonuses from the active Main and Secondary paths combined.
        /// Index with <c>(int)StatType.Attack</c> etc.
        /// Neutral value: 0f (no bonus).
        /// </summary>
        public float[] pathBonuses;

        /// <summary>
        /// Per-stat multiplicative modifiers from all active rituals.
        /// Index with <c>(int)StatType.Speed</c> etc.
        /// Neutral value: 1.0f (no modification).
        /// </summary>
        public float[] ritualMultipliers;

        /// <summary>
        /// Per-stat multiplicative modifiers from all equipped trinkets.
        /// Index with <c>(int)StatType.CritChance</c> etc.
        /// Neutral value: 1.0f (no modification).
        /// </summary>
        public float[] trinketMultipliers;

        /// <summary>
        /// Per-stat multiplicative modifiers from permanent Soul Tree progression.
        /// These persist across runs and are applied last in the formula.
        /// Neutral value: 1.0f (no modification).
        /// </summary>
        public float[] soulTreeBonuses;

        /// <summary>
        /// Creates a <see cref="StatModifierInput"/> with no active modifiers.
        /// All path bonuses are 0f; all multipliers are 1.0f.
        ///
        /// <para>Pass the result to <see cref="CharacterStatCalculator.Calculate"/>
        /// to get the character's unmodified base stats as <see cref="FinalStats"/>.</para>
        /// </summary>
        /// <param name="baseStats">The character's base stat ScriptableObject.</param>
        public static StatModifierInput Default(CharacterBaseStats baseStats)
        {
            int count = StatCount;

            // pathBonuses default to 0f — C# zero-initializes float arrays
            var pathBonuses = new float[count];

            var ritualMultipliers  = new float[count];
            var trinketMultipliers = new float[count];
            var soulTreeBonuses    = new float[count];

            for (int i = 0; i < count; i++)
            {
                ritualMultipliers[i]  = 1.0f;
                trinketMultipliers[i] = 1.0f;
                soulTreeBonuses[i]    = 1.0f;
            }

            return new StatModifierInput
            {
                baseStats          = baseStats,
                pathBonuses        = pathBonuses,
                ritualMultipliers  = ritualMultipliers,
                trinketMultipliers = trinketMultipliers,
                soulTreeBonuses    = soulTreeBonuses,
            };
        }
    }
}
