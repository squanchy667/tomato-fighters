namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// The 3 currency types in Tomato Fighters, each with different persistence
    /// and rarity behaviors.
    ///
    /// <list type="bullet">
    ///   <item><description><b>Crystals</b> — common, persist between runs. Spent on Soul Tree upgrades and hub shop.</description></item>
    ///   <item><description><b>ImbuedFruits</b> — rare, reset on run end. Spent on in-run power-ups and ritual upgrades.</description></item>
    ///   <item><description><b>PrimordialSeeds</b> — rare, reset on run end. Spent to permanently unlock Arcanas and Inspirations.</description></item>
    /// </list>
    /// </summary>
    public enum CurrencyType
    {
        Crystals,
        ImbuedFruits,
        PrimordialSeeds
    }
}
