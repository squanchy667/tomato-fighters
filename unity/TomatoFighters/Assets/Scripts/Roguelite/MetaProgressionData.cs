using System;
using System.Collections.Generic;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Serializable snapshot of meta-progression state for save/load (T039).
    /// Contains unlocked Soul Tree node IDs and permanently unlocked inspiration IDs.
    /// </summary>
    [Serializable]
    public struct MetaProgressionData
    {
        /// <summary>IDs of all Soul Tree nodes the player has unlocked.</summary>
        public List<string> unlockedNodeIds;

        /// <summary>IDs of inspirations permanently unlocked via Primordial Seeds.</summary>
        public List<string> permanentInspirationIds;

        /// <summary>Creates an empty progression state with no unlocked nodes or inspirations.</summary>
        public static MetaProgressionData Empty()
        {
            return new MetaProgressionData
            {
                unlockedNodeIds = new List<string>(),
                permanentInspirationIds = new List<string>()
            };
        }
    }
}
