namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Query interface for passive abilities that modify combat hit resolution.
    /// HitboxManager queries this during hit processing to apply CleavingStrikes/PiercingShots.
    /// </summary>
    public interface IPathAbilityModifier
    {
        /// <summary>
        /// Extra targets hit beyond the primary. CleavingStrikes: 2.
        /// </summary>
        int GetAdditionalTargetCount();

        /// <summary>
        /// Damage scale for additional targets. CleavingStrikes: 0.6.
        /// </summary>
        float GetAdditionalTargetDamageScale();

        /// <summary>
        /// Whether projectiles pass through targets. PiercingShots: true.
        /// </summary>
        bool DoProjectilesPierce();

        /// <summary>
        /// Damage falloff per pierced target. PiercingShots: 0.8 (20% reduction per target).
        /// </summary>
        float GetPierceDamageFalloff();
    }
}
