using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Apply and query status effects on an entity. Combat abilities call this interface;
    /// World pillar's StatusEffectTracker implements it on enemies.
    /// </summary>
    public interface IStatusEffectable
    {
        /// <summary>Apply a status effect. Replaces existing effect of the same type.</summary>
        void AddEffect(StatusEffect effect);

        /// <summary>Check if an effect of the given type is currently active.</summary>
        bool HasEffect(StatusEffectType type);

        /// <summary>Get the active effect of the given type, or null if none.</summary>
        StatusEffect? GetEffect(StatusEffectType type);

        /// <summary>Combined slow multiplier. 1.0 = no slow.</summary>
        float GetSlowMultiplier();

        /// <summary>Whether the entity is currently immobilized (cannot move).</summary>
        bool IsImmobilized();
    }
}
