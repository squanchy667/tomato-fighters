using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Event payload fired by <see cref="CurrencyManager"/> whenever a currency balance changes.
    /// Listeners (HUD, shop screens, MetaProgression) use this to reactively update their state.
    /// </summary>
    public struct CurrencyChangeEventData
    {
        /// <summary>Which currency changed.</summary>
        public CurrencyType currencyType;

        /// <summary>Balance before the change was applied.</summary>
        public int previousAmount;

        /// <summary>Balance after the change was applied.</summary>
        public int newAmount;

        /// <summary>
        /// The signed change amount. Positive for additions, negative for removals.
        /// On reset, this equals <c>-previousAmount</c>.
        /// </summary>
        public int delta;
    }
}
