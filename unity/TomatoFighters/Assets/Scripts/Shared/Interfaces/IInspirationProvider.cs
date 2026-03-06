using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Cross-pillar interface for Combat to query active inspiration effects.
    /// Implemented by <c>InspirationSystem</c> in the Roguelite pillar.
    /// </summary>
    public interface IInspirationProvider
    {
        /// <summary>Returns additive stat bonus from all active stat-type inspirations for a given stat.</summary>
        float GetInspirationStatBonus(StatType statType);

        /// <summary>Returns multiplier from percent-type inspirations for a given stat (1.0 = no change).</summary>
        float GetInspirationStatMultiplier(StatType statType);

        /// <summary>Checks if an ability modifier inspiration is currently active.</summary>
        bool HasAbilityModifier(string abilityModifierId);
    }
}
