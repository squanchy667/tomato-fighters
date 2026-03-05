using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Reward type chosen by the player after clearing a combat area.
    /// </summary>
    public enum RewardType
    {
        Ritual,
        Currency
    }

    /// <summary>
    /// Immutable payload carrying the player's reward selection result.
    /// Fired via <see cref="TomatoFighters.Shared.Events.RewardSelectedEventChannel"/>
    /// and consumed by RitualSystem (to add the ritual) or CurrencyManager (to grant currency).
    /// </summary>
    public struct RewardSelectedData
    {
        /// <summary>Whether the player chose a ritual or a currency reward.</summary>
        public RewardType rewardType;

        /// <summary>
        /// The selected ritual SO. Only valid when <see cref="rewardType"/> is <see cref="RewardType.Ritual"/>.
        /// Cast to <c>RitualData</c> in the Roguelite pillar.
        /// Typed as <see cref="ScriptableObject"/> to avoid cross-pillar import.
        /// </summary>
        public ScriptableObject selectedRitual;

        /// <summary>Currency type granted. Only valid when <see cref="rewardType"/> is <see cref="RewardType.Currency"/>.</summary>
        public CurrencyType currencyType;

        /// <summary>Amount of currency granted. Only valid when <see cref="rewardType"/> is <see cref="RewardType.Currency"/>.</summary>
        public int currencyAmount;
    }
}
