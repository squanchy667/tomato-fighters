using System.Collections.Generic;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Pure C# class that picks N random rituals from an available pool,
    /// weighted by category and filtering out maxed rituals.
    ///
    /// <para>Extracted from the UI for testability — no MonoBehaviour dependency.
    /// WaveManager, BossAI, or other systems can reuse this for different reward pools.</para>
    /// </summary>
    public class RitualPoolSelector
    {
        /// <summary>
        /// Delegate for generating random floats in [0, maxExclusive). Injected for testability.
        /// </summary>
        public delegate float RandomProvider(float maxExclusive);

        private readonly RandomProvider _random;

        /// <summary>
        /// Creates a selector with a custom random provider for deterministic testing.
        /// </summary>
        public RitualPoolSelector(RandomProvider random)
        {
            _random = random;
        }

        /// <summary>
        /// Creates a selector using Unity's <c>UnityEngine.Random.Range</c>.
        /// </summary>
        public RitualPoolSelector()
        {
            _random = (max) => UnityEngine.Random.Range(0f, max);
        }

        /// <summary>
        /// Selects up to <paramref name="count"/> rituals from the <paramref name="availablePool"/>,
        /// weighted by category weights from <paramref name="config"/>, and filtering out
        /// any rituals present in <paramref name="maxedRituals"/>.
        /// </summary>
        /// <param name="availablePool">All ritual SOs that could appear as rewards.</param>
        /// <param name="config">Provides category weights via <see cref="RewardConfig.GetCategoryWeight"/>.</param>
        /// <param name="maxedRituals">Set of rituals already at max level — excluded from selection.</param>
        /// <param name="count">Number of rituals to select.</param>
        /// <returns>List of selected rituals. May contain fewer than <paramref name="count"/> if the pool is too small.</returns>
        public List<RitualData> Select(
            IReadOnlyList<RitualData> availablePool,
            RewardConfig config,
            HashSet<RitualData> maxedRituals,
            int count)
        {
            var results = new List<RitualData>();
            if (availablePool == null || availablePool.Count == 0 || count <= 0)
                return results;

            // Build filtered candidate list with weights
            var candidates = new List<WeightedCandidate>();
            foreach (var ritual in availablePool)
            {
                if (ritual == null) continue;
                if (maxedRituals != null && maxedRituals.Contains(ritual)) continue;

                float weight = config != null ? config.GetCategoryWeight(ritual.category) : 1f;
                if (weight <= 0f) continue;

                candidates.Add(new WeightedCandidate { ritual = ritual, weight = weight });
            }

            // Weighted random selection without replacement
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                float totalWeight = 0f;
                foreach (var c in candidates)
                    totalWeight += c.weight;

                float roll = _random(totalWeight);
                float cumulative = 0f;
                int selectedIndex = candidates.Count - 1; // fallback to last

                for (int j = 0; j < candidates.Count; j++)
                {
                    cumulative += candidates[j].weight;
                    if (roll < cumulative)
                    {
                        selectedIndex = j;
                        break;
                    }
                }

                results.Add(candidates[selectedIndex].ritual);
                candidates.RemoveAt(selectedIndex);
            }

            return results;
        }

        private struct WeightedCandidate
        {
            public RitualData ritual;
            public float weight;
        }
    }
}
