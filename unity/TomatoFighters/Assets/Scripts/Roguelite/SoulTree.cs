using System.Collections.Generic;
using System.Linq;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Pure C# class containing all Soul Tree logic: unlock tracking,
    /// prerequisite validation, and bonus calculation.
    /// Stateless relative to Unity — fully unit-testable.
    /// </summary>
    public class SoulTree
    {
        private readonly HashSet<string> _unlockedNodeIds = new HashSet<string>();

        /// <summary>
        /// Attempts to unlock a node. Succeeds only if all prerequisites are met
        /// and the node is not already unlocked.
        /// Does NOT handle currency — that's <see cref="MetaProgression"/>'s job.
        /// </summary>
        /// <returns><c>true</c> if the node was successfully unlocked.</returns>
        public bool TryUnlock(SoulTreeNodeData node)
        {
            if (node == null) return false;
            if (_unlockedNodeIds.Contains(node.nodeId)) return false;

            // Check prerequisites
            if (node.prerequisiteNodeIds != null)
            {
                foreach (var prereq in node.prerequisiteNodeIds)
                {
                    if (!_unlockedNodeIds.Contains(prereq))
                        return false;
                }
            }

            _unlockedNodeIds.Add(node.nodeId);
            return true;
        }

        /// <summary>Returns whether a node with the given ID has been unlocked.</summary>
        public bool IsUnlocked(string nodeId)
        {
            return _unlockedNodeIds.Contains(nodeId);
        }

        /// <summary>
        /// Calculates the soul tree multiplier for a given stat.
        /// Additive stacking within the tree: 1.0 + sum of all unlocked bonus values for this stat.
        /// </summary>
        /// <param name="stat">The stat type to calculate the bonus for.</param>
        /// <param name="allNodes">All nodes in the soul tree config.</param>
        /// <returns>Multiplier >= 1.0. Returns 1.0 if no bonuses apply.</returns>
        public float GetStatBonus(StatType stat, List<SoulTreeNodeData> allNodes)
        {
            float sum = 0f;

            foreach (var node in allNodes)
            {
                if (node.nodeType == SoulTreeNodeType.StatBonus
                    && node.affectedStat == stat
                    && _unlockedNodeIds.Contains(node.nodeId))
                {
                    sum += node.bonusValue;
                }
            }

            return 1.0f + sum;
        }

        /// <summary>
        /// Checks if any unlocked special-unlock node has the given unlock ID.
        /// </summary>
        public bool HasSpecialUnlock(string unlockId, List<SoulTreeNodeData> allNodes)
        {
            foreach (var node in allNodes)
            {
                if (node.nodeType == SoulTreeNodeType.SpecialUnlock
                    && node.specialUnlockId == unlockId
                    && _unlockedNodeIds.Contains(node.nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns a copy of all unlocked node IDs for serialization.</summary>
        public List<string> GetUnlockedNodeIds()
        {
            return _unlockedNodeIds.ToList();
        }

        /// <summary>Restores state from saved data. Clears current state first.</summary>
        public void LoadFromData(MetaProgressionData data)
        {
            _unlockedNodeIds.Clear();

            if (data.unlockedNodeIds == null) return;

            foreach (var id in data.unlockedNodeIds)
            {
                _unlockedNodeIds.Add(id);
            }
        }

        /// <summary>
        /// Checks whether a node's prerequisites are all met and the node is not yet unlocked.
        /// </summary>
        public bool CanUnlock(SoulTreeNodeData node)
        {
            if (node == null) return false;
            if (_unlockedNodeIds.Contains(node.nodeId)) return false;

            if (node.prerequisiteNodeIds != null)
            {
                foreach (var prereq in node.prerequisiteNodeIds)
                {
                    if (!_unlockedNodeIds.Contains(prereq))
                        return false;
                }
            }

            return true;
        }
    }
}
