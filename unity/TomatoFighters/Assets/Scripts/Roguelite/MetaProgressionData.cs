using System;
using System.Collections.Generic;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Serializable snapshot of meta-progression state for save/load (T039).
    /// Contains only the list of unlocked node IDs — the full tree structure
    /// is defined by <see cref="TomatoFighters.Shared.Data.SoulTreeConfig"/>.
    /// </summary>
    [Serializable]
    public struct MetaProgressionData
    {
        /// <summary>IDs of all Soul Tree nodes the player has unlocked.</summary>
        public List<string> unlockedNodeIds;

        /// <summary>Creates an empty progression state with no unlocked nodes.</summary>
        public static MetaProgressionData Empty()
        {
            return new MetaProgressionData
            {
                unlockedNodeIds = new List<string>()
            };
        }
    }
}
