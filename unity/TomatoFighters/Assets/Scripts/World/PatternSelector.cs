using System.Collections.Generic;

namespace TomatoFighters.World
{
    /// <summary>
    /// Pure logic for selecting an attack pattern from a list of candidates.
    /// Extracted from EnemyAI for testability — no MonoBehaviour dependencies.
    /// </summary>
    public static class PatternSelector
    {
        /// <summary>
        /// Selects a pattern using weighted random, filtered by range and cooldown.
        /// Returns null if no patterns are defined.
        /// </summary>
        /// <param name="patterns">Available attack patterns.</param>
        /// <param name="distToTarget">Current distance to the target.</param>
        /// <param name="cooldowns">Map of pattern → last-used time.</param>
        /// <param name="currentTime">Current game time (Time.time).</param>
        /// <param name="randomValue">A random value in [0,1) for weighted selection.</param>
        public static EnemyAttackPattern Select(
            EnemyAttackPattern[] patterns,
            float distToTarget,
            Dictionary<EnemyAttackPattern, float> cooldowns,
            float currentTime,
            float randomValue)
        {
            if (patterns == null || patterns.Length == 0)
                return null;

            // Filter by range and cooldown
            float totalWeight = 0f;
            var candidates = new List<EnemyAttackPattern>();

            for (int i = 0; i < patterns.Length; i++)
            {
                var p = patterns[i];
                if (p == null) continue;
                if (distToTarget < p.minRange || distToTarget > p.maxRange) continue;
                if (!IsReady(p, cooldowns, currentTime)) continue;

                candidates.Add(p);
                totalWeight += p.selectionWeight;
            }

            // If all filtered out, pick shortest remaining cooldown in range
            if (candidates.Count == 0)
                return SelectShortestCooldown(patterns, distToTarget, cooldowns, currentTime);

            // Weighted random selection
            float roll = randomValue * totalWeight;
            float cumulative = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].selectionWeight;
                if (roll <= cumulative)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        /// <summary>Whether a pattern's cooldown has expired.</summary>
        public static bool IsReady(
            EnemyAttackPattern pattern,
            Dictionary<EnemyAttackPattern, float> cooldowns,
            float currentTime)
        {
            if (!cooldowns.TryGetValue(pattern, out float lastUsed))
                return true;
            return currentTime - lastUsed >= pattern.patternCooldown;
        }

        private static EnemyAttackPattern SelectShortestCooldown(
            EnemyAttackPattern[] patterns,
            float distToTarget,
            Dictionary<EnemyAttackPattern, float> cooldowns,
            float currentTime)
        {
            EnemyAttackPattern best = null;
            float bestRemaining = float.MaxValue;

            for (int i = 0; i < patterns.Length; i++)
            {
                var p = patterns[i];
                if (p == null) continue;
                if (distToTarget < p.minRange || distToTarget > p.maxRange) continue;

                float remaining = 0f;
                if (cooldowns.TryGetValue(p, out float lastUsed))
                    remaining = p.patternCooldown - (currentTime - lastUsed);
                if (remaining < 0f) remaining = 0f;

                if (remaining < bestRemaining)
                {
                    bestRemaining = remaining;
                    best = p;
                }
            }

            return best;
        }
    }
}
