using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Uniform display model wrapping either a <see cref="RitualData"/> or a currency reward.
    /// Used by <see cref="RewardSelectorUI"/> to render reward cards without caring about
    /// the underlying reward type.
    /// </summary>
    public class RewardOption
    {
        /// <summary>Whether this is a ritual or currency reward.</summary>
        public RewardType type;

        /// <summary>Name shown on the reward card.</summary>
        public string displayName;

        /// <summary>Description text shown on the card.</summary>
        public string description;

        /// <summary>Color used for the card border, derived from family or currency type.</summary>
        public Color borderColor;

        /// <summary>Backing ritual SO. Null for currency rewards.</summary>
        public RitualData ritualData;

        /// <summary>Currency type. Only meaningful when <see cref="type"/> is <see cref="RewardType.Currency"/>.</summary>
        public CurrencyType currencyType;

        /// <summary>Currency amount. Only meaningful when <see cref="type"/> is <see cref="RewardType.Currency"/>.</summary>
        public int currencyAmount;

        /// <summary>
        /// Creates a <see cref="RewardOption"/> from a <see cref="RitualData"/> SO.
        /// Border color is derived from the ritual's family.
        /// </summary>
        public static RewardOption FromRitual(RitualData data)
        {
            return new RewardOption
            {
                type = RewardType.Ritual,
                displayName = data.ritualName,
                description = data.description,
                borderColor = GetFamilyColor(data.family),
                ritualData = data,
                currencyType = default,
                currencyAmount = 0
            };
        }

        /// <summary>
        /// Creates a <see cref="RewardOption"/> for a currency reward.
        /// </summary>
        public static RewardOption FromCurrency(CurrencyType currencyType, int amount)
        {
            return new RewardOption
            {
                type = RewardType.Currency,
                displayName = FormatCurrencyName(currencyType),
                description = $"+{amount} {FormatCurrencyName(currencyType)}",
                borderColor = GetCurrencyColor(currencyType),
                ritualData = null,
                currencyType = currencyType,
                currencyAmount = amount
            };
        }

        /// <summary>
        /// Builds the <see cref="RewardSelectedData"/> payload for this option,
        /// ready to fire through the event channel.
        /// </summary>
        public RewardSelectedData ToEventData()
        {
            return new RewardSelectedData
            {
                rewardType = type,
                selectedRitual = ritualData,
                currencyType = currencyType,
                currencyAmount = currencyAmount
            };
        }

        private static Color GetFamilyColor(RitualFamily family) => family switch
        {
            RitualFamily.Fire      => new Color(1.0f, 0.35f, 0.15f),
            RitualFamily.Lightning => new Color(0.95f, 0.85f, 0.2f),
            RitualFamily.Water     => new Color(0.2f, 0.6f, 1.0f),
            RitualFamily.Thorn     => new Color(0.3f, 0.8f, 0.25f),
            RitualFamily.Gale      => new Color(0.6f, 0.9f, 0.95f),
            RitualFamily.Time      => new Color(0.7f, 0.5f, 0.9f),
            RitualFamily.Cosmic    => new Color(0.9f, 0.3f, 0.8f),
            RitualFamily.Necro     => new Color(0.4f, 0.9f, 0.5f),
            _                      => Color.white
        };

        private static Color GetCurrencyColor(CurrencyType type) => type switch
        {
            CurrencyType.Crystals        => new Color(0.5f, 0.8f, 1.0f),
            CurrencyType.ImbuedFruits    => new Color(1.0f, 0.6f, 0.2f),
            CurrencyType.PrimordialSeeds => new Color(0.4f, 0.9f, 0.4f),
            _                            => Color.grey
        };

        private static string FormatCurrencyName(CurrencyType type) => type switch
        {
            CurrencyType.Crystals        => "Crystals",
            CurrencyType.ImbuedFruits    => "Imbued Fruits",
            CurrencyType.PrimordialSeeds => "Primordial Seeds",
            _                            => type.ToString()
        };
    }
}
