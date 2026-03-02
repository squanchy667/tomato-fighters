namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// All character stats used by the stat calculator and buff system.
    ///
    /// <para><b>RangedAttack</b> is Viper-only. Non-Viper characters store -1 in
    /// <c>CharacterBaseStats.rangedAttack</c> as a sentinel meaning "not a ranged specialist".
    /// The stat calculator skips this stat when the base value is negative.</para>
    ///
    /// <para><b>ThrowableAttack</b> applies to ALL characters and scales damage from
    /// ground-pickup throwable items (bottles, rocks, crates, etc.).</para>
    /// </summary>
    public enum StatType
    {
        Health,
        Defense,
        Attack,
        RangedAttack,     // Viper only (-1 on non-Viper characters)
        ThrowableAttack,  // All characters — scales throwable item damage
        Speed,
        Mana,
        ManaRegen,
        CritChance,
        StunRate,
        CancelWindow
    }
}
