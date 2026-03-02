namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// Identifies the type of attack being executed. Used to query the correct
    /// damage multiplier from <see cref="TomatoFighters.Paths.FinalStats.GetAttackForMode"/>.
    ///
    /// <para>Combat pillar passes this when requesting a damage value so it never
    /// needs to know about the Viper-only rangedAttack sentinel (-1f).</para>
    /// </summary>
    public enum AttackMode
    {
        /// <summary>Close-range physical strike. Uses <c>FinalStats.attack</c>.</summary>
        Melee,

        /// <summary>
        /// Projectile attack. Uses <c>FinalStats.rangedAttack</c> for Viper;
        /// falls back to <c>FinalStats.attack</c> for all other characters.
        /// </summary>
        Ranged,

        /// <summary>
        /// Ground-pickup throwable item (bottle, rock, crate, etc.).
        /// Uses <c>FinalStats.throwableAttack</c> for all characters.
        /// </summary>
        Throwable,
    }
}
