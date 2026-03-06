using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Roguelite pillar provides persistent meta-progression data;
    /// Combat and Paths pillars query it for soul tree bonuses.
    /// Default returns when no progression: 1.0f for multipliers, false for unlocks.
    /// </summary>
    public interface IMetaProvider
    {
        /// <summary>
        /// Returns the soul tree multiplier for a given stat type.
        /// Formula: 1.0 + sum of all unlocked stat bonus values for this stat.
        /// Returns 1.0f if no bonuses are unlocked.
        /// </summary>
        float GetSoulTreeBonus(StatType statType);

        /// <summary>
        /// Checks if a special unlock (e.g., "self_revive", "third_ritual_choice") is active.
        /// </summary>
        bool HasSpecialUnlock(string unlockId);
    }
}
