using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Roguelite pillar provides path state; Combat and World pillars query it.
    /// Exposes the player's chosen paths, tiers, and unlocked abilities.
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>Which character the player is using this run.</summary>
        CharacterType Character { get; }

        /// <summary>The selected main path. Null/default if not yet selected.</summary>
        object MainPath { get; } // TODO: Replace with PathData when T008 lands

        /// <summary>The selected secondary path. Null/default if not yet selected.</summary>
        object SecondaryPath { get; } // TODO: Replace with PathData when T008 lands

        /// <summary>Main path tier (1-3). 0 if no main path selected.</summary>
        int MainPathTier { get; }

        /// <summary>Secondary path tier (1-2). 0 if no secondary path selected.</summary>
        int SecondaryPathTier { get; }

        /// <summary>Check whether a specific path type is active (main or secondary).</summary>
        bool HasPath(PathType type);

        /// <summary>Get the total stat bonus from active paths for a given stat.</summary>
        float GetPathStatBonus(StatType stat);

        /// <summary>Check whether a specific path ability is unlocked at the current tier.</summary>
        bool IsPathAbilityUnlocked(string abilityId);
    }
}
