namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// Distinguishes between stat-boosting and ability-enhancing inspirations.
    /// Mirrors the SoulTreeNodeType pattern of StatBonus vs SpecialUnlock.
    /// </summary>
    public enum InspirationEffectType
    {
        /// <summary>Grants a stat bonus (uses statType + modifierType + value).</summary>
        StatModifier,

        /// <summary>Enhances a specific move (uses abilityModifierId string).</summary>
        AbilityModifier
    }
}
