using System;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Per-category weight for ritual pool selection.
    /// </summary>
    [Serializable]
    public struct CategoryWeight
    {
        public RitualCategory category;

        [Tooltip("Relative selection weight. Higher = more likely to appear.")]
        [Range(0f, 10f)]
        public float weight;
    }

    /// <summary>
    /// ScriptableObject holding all configurable reward parameters.
    /// Designers can tweak currency amounts, ritual pool sizes, and category weights
    /// in the Inspector without code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Data/RewardConfig", fileName = "RewardConfig")]
    public class RewardConfig : ScriptableObject
    {
        // ── Ritual pool ──────────────────────────────────────────────────────

        [Header("Ritual Pool")]
        [Tooltip("Number of ritual options to present (default 2, Soul Tree can set 3).")]
        [Range(1, 5)]
        public int baseRitualChoices = 2;

        [Tooltip("Category weights for ritual selection. Higher weight = more likely.")]
        public CategoryWeight[] categoryWeights = new CategoryWeight[]
        {
            new CategoryWeight { category = RitualCategory.Core,        weight = 1.0f },
            new CategoryWeight { category = RitualCategory.General,     weight = 2.0f },
            new CategoryWeight { category = RitualCategory.Enhancement, weight = 1.5f },
            new CategoryWeight { category = RitualCategory.Twin,        weight = 0.5f }
        };

        // ── Currency rewards ─────────────────────────────────────────────────

        [Header("Currency — Crystals")]
        [Tooltip("Base crystal reward per area clear.")]
        public int baseCrystalReward = 25;

        [Tooltip("Additional crystals per area index (area 0 = base, area 1 = base + increment, etc.).")]
        public int crystalPerAreaIncrement = 5;

        [Header("Currency — Imbued Fruits")]
        [Tooltip("Base imbued fruit reward per area clear.")]
        public int baseImbuedFruitReward = 10;

        [Tooltip("Additional imbued fruits per area index.")]
        public int imbuedFruitPerAreaIncrement = 2;

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the crystal reward amount for the given area index.
        /// </summary>
        public int GetCrystalReward(int areaIndex)
        {
            return baseCrystalReward + crystalPerAreaIncrement * Mathf.Max(0, areaIndex);
        }

        /// <summary>
        /// Returns the imbued fruit reward amount for the given area index.
        /// </summary>
        public int GetImbuedFruitReward(int areaIndex)
        {
            return baseImbuedFruitReward + imbuedFruitPerAreaIncrement * Mathf.Max(0, areaIndex);
        }

        /// <summary>
        /// Returns the selection weight for the given category.
        /// Defaults to 1.0 if the category is not configured.
        /// </summary>
        public float GetCategoryWeight(RitualCategory category)
        {
            if (categoryWeights == null) return 1f;

            foreach (var cw in categoryWeights)
            {
                if (cw.category == category)
                    return cw.weight;
            }
            return 1f;
        }
    }
}
