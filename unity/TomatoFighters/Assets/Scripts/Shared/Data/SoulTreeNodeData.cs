using System.Collections.Generic;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Defines a single node in the Soul Tree.
    /// Each node is either a stat bonus or a special unlock.
    /// </summary>
    [CreateAssetMenu(fileName = "SoulTreeNode", menuName = "TomatoFighters/SoulTree/Node")]
    public class SoulTreeNodeData : ScriptableObject
    {
        /// <summary>Unique identifier for this node (e.g., "hp_1", "self_revive").</summary>
        public string nodeId;

        /// <summary>Display name shown in the Soul Tree UI.</summary>
        public string displayName;

        /// <summary>Tooltip description shown when hovering over the node.</summary>
        [TextArea] public string description;

        /// <summary>Whether this node grants a stat bonus or a special unlock.</summary>
        public SoulTreeNodeType nodeType;

        [Header("Stat Bonus (if StatBonus type)")]
        /// <summary>Which stat this node boosts. Only used when nodeType is StatBonus.</summary>
        public StatType affectedStat;

        /// <summary>Additive bonus value (e.g., 0.05 for +5%). Only used when nodeType is StatBonus.</summary>
        public float bonusValue;

        [Header("Special Unlock (if SpecialUnlock type)")]
        /// <summary>Unique ID for the special unlock (e.g., "self_revive"). Only used when nodeType is SpecialUnlock.</summary>
        public string specialUnlockId;

        [Header("Cost")]
        /// <summary>Number of Crystals required to purchase this node.</summary>
        public int crystalCost;

        [Header("Prerequisites (optional)")]
        /// <summary>
        /// Node IDs that must be unlocked before this node can be purchased.
        /// Empty list means the node is available immediately.
        /// </summary>
        public List<string> prerequisiteNodeIds = new List<string>();
    }
}
