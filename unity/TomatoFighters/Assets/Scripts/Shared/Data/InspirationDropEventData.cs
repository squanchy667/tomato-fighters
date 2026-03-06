using System;
using System.Collections.Generic;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Event payload fired when a mini-boss defeat triggers an inspiration drop.
    /// Contains the list of candidate inspirations for the player to choose from.
    /// </summary>
    [Serializable]
    public struct InspirationDropEventData
    {
        /// <summary>Candidate inspirations the player can pick from (typically 2-3).</summary>
        public List<InspirationData> candidates;
    }
}
