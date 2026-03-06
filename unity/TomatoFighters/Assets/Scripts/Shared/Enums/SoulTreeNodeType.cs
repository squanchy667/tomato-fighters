namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// Distinguishes between Soul Tree nodes that grant stat bonuses
    /// and those that unlock special abilities.
    /// </summary>
    public enum SoulTreeNodeType
    {
        /// <summary>Grants an additive multiplier bonus to a specific stat.</summary>
        StatBonus,

        /// <summary>Unlocks a special ability (e.g., self-revive, 3rd ritual choice).</summary>
        SpecialUnlock
    }
}
