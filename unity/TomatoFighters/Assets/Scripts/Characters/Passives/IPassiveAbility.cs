using TomatoFighters.Shared.Data;

namespace TomatoFighters.Characters.Passives
{
    /// <summary>
    /// Contract for individual passive ability implementations.
    /// Each passive is a plain C# class receiving config at construction time.
    /// </summary>
    public interface IPassiveAbility
    {
        /// <summary>Outgoing damage multiplier for this hit.</summary>
        float GetDamageMultiplier(HitContext context);

        /// <summary>Incoming damage defense multiplier (1.0 = no reduction).</summary>
        float GetDefenseMultiplier();

        /// <summary>Incoming knockback force multiplier (1.0 = full knockback).</summary>
        float GetKnockbackMultiplier();

        /// <summary>Movement speed multiplier (1.0 = no change).</summary>
        float GetSpeedMultiplier();

        /// <summary>Called each frame to advance timers and decay logic.</summary>
        void Tick(float deltaTime);

        /// <summary>Called when the owner lands a hit on a target.</summary>
        void OnHitLanded();

        /// <summary>Called when the owner performs an attack (cast/swing).</summary>
        void OnAttackPerformed();
    }
}
