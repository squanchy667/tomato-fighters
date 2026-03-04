using TomatoFighters.Shared.Data;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Provides passive ability multipliers to the damage pipeline.
    /// Combat-internal concern — separate from <see cref="IBuffProvider"/> which
    /// is Roguelite's channel to Combat.
    /// </summary>
    public interface IPassiveProvider
    {
        /// <summary>
        /// Damage multiplier applied to outgoing attacks.
        /// Context-dependent: Viper uses distance, Slasher uses stack count.
        /// </summary>
        float GetDamageMultiplier(HitContext context);

        /// <summary>
        /// Defense multiplier applied to incoming damage.
        /// Brutor's Thick Skin returns 0.85 (15% DR).
        /// </summary>
        float GetDefenseMultiplier();

        /// <summary>
        /// Knockback multiplier applied to incoming knockback force.
        /// Brutor's Thick Skin returns 0.6 (40% reduction).
        /// </summary>
        float GetKnockbackMultiplier();

        /// <summary>
        /// Speed multiplier applied to movement speed. Reserved for future passives.
        /// </summary>
        float GetSpeedMultiplier();

        /// <summary>
        /// Advance time-based passive logic (decay timers, stack expiry).
        /// Called each frame with delta time.
        /// </summary>
        void Tick(float deltaTime);
    }
}
