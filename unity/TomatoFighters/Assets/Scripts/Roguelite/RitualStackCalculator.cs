namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Pure math calculator for ritual effect magnitudes.
    /// Applies the stacking formula:
    ///   finalEffect = baseValue × levelMultiplier × (stackingMultiplier ^ currentStacks) × ritualPower
    ///
    /// <para>No Unity dependencies — fully unit-testable as a static class.</para>
    /// </summary>
    public static class RitualStackCalculator
    {
        // ── Level multiplier constants ──────────────────────────────────────
        public const float LEVEL_1_MULT = 1.0f;
        public const float LEVEL_2_MULT = 1.5f;
        public const float LEVEL_3_MULT = 2.0f;

        /// <summary>
        /// Computes the final effect magnitude for a ritual.
        /// </summary>
        /// <param name="baseValue">Raw effect magnitude from <see cref="RitualLevelData.baseValue"/>.</param>
        /// <param name="level">Ritual level (1–3). Invalid values default to level 1 with a warning.</param>
        /// <param name="currentStacks">Number of active stacks. Negative values are clamped to 0.</param>
        /// <param name="stackingMultiplier">Per-stack multiplier from <see cref="RitualLevelData.stackingMultiplier"/>.</param>
        /// <param name="ritualPower">External power multiplier (defaults to 1.0 until T030 TrinketSystem).</param>
        /// <returns>The computed final effect value.</returns>
        public static float Compute(float baseValue, int level, int currentStacks,
                                     float stackingMultiplier, float ritualPower = 1.0f)
        {
            if (currentStacks < 0)
                currentStacks = 0;

            float levelMult = GetLevelMultiplier(level);
            float stackMult = Pow(stackingMultiplier, currentStacks);

            return baseValue * levelMult * stackMult * ritualPower;
        }

        /// <summary>
        /// Returns the fixed level multiplier for the given level (1–3).
        /// Returns 1.0 for invalid levels.
        /// </summary>
        public static float GetLevelMultiplier(int level)
        {
            return level switch
            {
                1 => LEVEL_1_MULT,
                2 => LEVEL_2_MULT,
                3 => LEVEL_3_MULT,
                _ => LEVEL_1_MULT
            };
        }

        /// <summary>Integer exponentiation for float base — avoids Mathf dependency.</summary>
        private static float Pow(float baseVal, int exponent)
        {
            if (exponent <= 0) return 1f;

            float result = 1f;
            for (int i = 0; i < exponent; i++)
                result *= baseVal;
            return result;
        }
    }
}
