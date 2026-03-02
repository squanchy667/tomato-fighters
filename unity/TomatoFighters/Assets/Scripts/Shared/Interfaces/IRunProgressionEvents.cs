using System;
using TomatoFighters.Shared.Data;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// World pillar fires these events; Roguelite subscribes for run state management.
    /// Tracks area progression, boss defeats, shop visits, and path selection.
    /// </summary>
    public interface IRunProgressionEvents
    {
        /// <summary>Fired when all waves in an area are cleared.</summary>
        event Action<AreaClearedData> OnAreaCleared;

        /// <summary>Fired when a boss is defeated.</summary>
        event Action<BossDefeatedData> OnBossDefeated;

        /// <summary>Fired when all areas and boss on an island are completed.</summary>
        event Action<IslandCompletedData> OnIslandCompleted;

        /// <summary>Fired when the player enters a shop between areas.</summary>
        event Action<ShopEnteredData> OnShopEntered;

        /// <summary>Fired when a new run begins.</summary>
        event Action OnRunStarted;

        /// <summary>Fired when a run ends (victory or defeat).</summary>
        event Action<RunEndData> OnRunEnded;

        /// <summary>Fired when the player selects their main path.</summary>
        event Action<PathSelectedData> OnMainPathSelected;

        /// <summary>Fired when the player selects their secondary path.</summary>
        event Action<PathSelectedData> OnSecondaryPathSelected;

        /// <summary>Fired when a path tier is upgraded.</summary>
        event Action<PathTierUpData> OnPathTierUp;
    }
}
