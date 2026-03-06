using System.Collections.Generic;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Contains the full Soul Tree layout — all nodes available for purchase.
    /// One instance per game (not per character).
    /// </summary>
    [CreateAssetMenu(fileName = "SoulTreeConfig", menuName = "TomatoFighters/SoulTree/Config")]
    public class SoulTreeConfig : ScriptableObject
    {
        /// <summary>All nodes in the Soul Tree, in display order.</summary>
        public List<SoulTreeNodeData> nodes = new List<SoulTreeNodeData>();
    }
}
