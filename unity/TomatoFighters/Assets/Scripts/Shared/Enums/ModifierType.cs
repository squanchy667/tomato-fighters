namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// How a trinket modifier is applied to a stat.
    /// </summary>
    public enum ModifierType
    {
        /// <summary>Adds a flat value to the base stat before conversion to a multiplier.</summary>
        Flat,

        /// <summary>Applies a percentage multiplier directly (e.g. 0.1 = +10%).</summary>
        Percent
    }
}
