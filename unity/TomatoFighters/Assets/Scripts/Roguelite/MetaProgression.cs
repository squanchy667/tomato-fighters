using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// MonoBehaviour manager that owns the Soul Tree and CurrencyManager references.
    /// Implements <see cref="IMetaProvider"/> so other pillars can query meta-progression
    /// bonuses without importing Roguelite code.
    /// </summary>
    public class MetaProgression : MonoBehaviour, IMetaProvider
    {
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private SoulTreeConfig _soulTreeConfig;

        private SoulTree _soulTree;

        /// <summary>Fired when a node is successfully purchased and unlocked.</summary>
        public event Action<string> OnNodeUnlocked;

        private void Awake()
        {
            _soulTree = new SoulTree();
        }

        // ── IMetaProvider ────────────────────────────────────────────────────

        /// <inheritdoc />
        public float GetSoulTreeBonus(StatType statType)
        {
            return _soulTree.GetStatBonus(statType, _soulTreeConfig.nodes);
        }

        /// <inheritdoc />
        public bool HasSpecialUnlock(string unlockId)
        {
            return _soulTree.HasSpecialUnlock(unlockId, _soulTreeConfig.nodes);
        }

        // ── Purchase API ─────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to purchase and unlock a Soul Tree node by ID.
        /// Deducts crystals via CurrencyManager on success.
        /// </summary>
        /// <returns><c>true</c> if the node was purchased; <c>false</c> if prerequisites not met,
        /// already unlocked, node not found, or insufficient crystals.</returns>
        public bool TryPurchaseNode(string nodeId)
        {
            var node = FindNode(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[MetaProgression] Node '{nodeId}' not found in SoulTreeConfig.");
                return false;
            }

            if (!_soulTree.CanUnlock(node))
                return false;

            if (!_currencyManager.TryRemove(CurrencyType.Crystals, node.crystalCost))
                return false;

            // Currency deducted — unlock the node
            bool unlocked = _soulTree.TryUnlock(node);
            if (unlocked)
            {
                OnNodeUnlocked?.Invoke(nodeId);
            }

            return unlocked;
        }

        /// <summary>Returns whether a node is currently unlocked.</summary>
        public bool IsNodeUnlocked(string nodeId)
        {
            return _soulTree.IsUnlocked(nodeId);
        }

        /// <summary>Returns whether a node can be purchased (prerequisites met, not yet unlocked).</summary>
        public bool CanPurchaseNode(string nodeId)
        {
            var node = FindNode(nodeId);
            if (node == null) return false;

            return _soulTree.CanUnlock(node)
                   && _currencyManager.CanAfford(CurrencyType.Crystals, node.crystalCost);
        }

        // ── Save/Load (for T039) ────────────────────────────────────────────

        /// <summary>Creates a serializable snapshot of current meta-progression state.</summary>
        public MetaProgressionData CreateSaveData()
        {
            return new MetaProgressionData
            {
                unlockedNodeIds = _soulTree.GetUnlockedNodeIds()
            };
        }

        /// <summary>Restores meta-progression state from saved data.</summary>
        public void LoadSaveData(MetaProgressionData data)
        {
            _soulTree.LoadFromData(data);
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private SoulTreeNodeData FindNode(string nodeId)
        {
            foreach (var node in _soulTreeConfig.nodes)
            {
                if (node.nodeId == nodeId)
                    return node;
            }
            return null;
        }
    }
}
