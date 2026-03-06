using System.Collections.Generic;
using NUnit.Framework;
using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Roguelite
{
    [TestFixture]
    public class SoulTreeTests
    {
        private SoulTree _tree;
        private List<SoulTreeNodeData> _allNodes;

        [SetUp]
        public void SetUp()
        {
            _tree = new SoulTree();
            _allNodes = new List<SoulTreeNodeData>();
        }

        // ── Unlock Flow ──────────────────────────────────────────────────────

        [Test]
        public void TryUnlock_NoPrerequisites_Succeeds()
        {
            var node = CreateStatNode("hp_1", StatType.Health, 0.05f);
            _allNodes.Add(node);

            Assert.IsTrue(_tree.TryUnlock(node));
            Assert.IsTrue(_tree.IsUnlocked("hp_1"));
        }

        [Test]
        public void TryUnlock_AlreadyUnlocked_ReturnsFalse()
        {
            var node = CreateStatNode("hp_1", StatType.Health, 0.05f);
            _allNodes.Add(node);

            _tree.TryUnlock(node);
            Assert.IsFalse(_tree.TryUnlock(node));
        }

        [Test]
        public void TryUnlock_NullNode_ReturnsFalse()
        {
            Assert.IsFalse(_tree.TryUnlock(null));
        }

        // ── Prerequisites ────────────────────────────────────────────────────

        [Test]
        public void TryUnlock_PrerequisiteNotMet_ReturnsFalse()
        {
            var node1 = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var node2 = CreateStatNode("hp_2", StatType.Health, 0.05f, "hp_1");
            _allNodes.Add(node1);
            _allNodes.Add(node2);

            Assert.IsFalse(_tree.TryUnlock(node2));
            Assert.IsFalse(_tree.IsUnlocked("hp_2"));
        }

        [Test]
        public void TryUnlock_PrerequisiteMet_Succeeds()
        {
            var node1 = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var node2 = CreateStatNode("hp_2", StatType.Health, 0.05f, "hp_1");
            _allNodes.Add(node1);
            _allNodes.Add(node2);

            _tree.TryUnlock(node1);
            Assert.IsTrue(_tree.TryUnlock(node2));
        }

        [Test]
        public void TryUnlock_MultiplePrerequisites_AllRequired()
        {
            var node1 = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var node2 = CreateStatNode("atk_1", StatType.Attack, 0.03f);
            var node3 = CreateStatNode("combo_1", StatType.Health, 0.10f, "hp_1", "atk_1");
            _allNodes.Add(node1);
            _allNodes.Add(node2);
            _allNodes.Add(node3);

            // Only one prereq met
            _tree.TryUnlock(node1);
            Assert.IsFalse(_tree.TryUnlock(node3));

            // Both met
            _tree.TryUnlock(node2);
            Assert.IsTrue(_tree.TryUnlock(node3));
        }

        // ── Additive Stacking ────────────────────────────────────────────────

        [Test]
        public void GetStatBonus_NoUnlocks_ReturnsOne()
        {
            var node = CreateStatNode("hp_1", StatType.Health, 0.05f);
            _allNodes.Add(node);

            Assert.AreEqual(1.0f, _tree.GetStatBonus(StatType.Health, _allNodes));
        }

        [Test]
        public void GetStatBonus_SingleUnlock_AddsBonus()
        {
            var node = CreateStatNode("hp_1", StatType.Health, 0.05f);
            _allNodes.Add(node);
            _tree.TryUnlock(node);

            Assert.AreEqual(1.05f, _tree.GetStatBonus(StatType.Health, _allNodes), 0.0001f);
        }

        [Test]
        public void GetStatBonus_MultipleUnlocks_StacksAdditively()
        {
            // DD-2: Two +5% HP nodes → 1.0 + 0.05 + 0.05 = 1.10
            var node1 = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var node2 = CreateStatNode("hp_2", StatType.Health, 0.05f);
            _allNodes.Add(node1);
            _allNodes.Add(node2);

            _tree.TryUnlock(node1);
            _tree.TryUnlock(node2);

            Assert.AreEqual(1.10f, _tree.GetStatBonus(StatType.Health, _allNodes), 0.0001f);
        }

        [Test]
        public void GetStatBonus_DifferentStats_Independent()
        {
            var hpNode = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var atkNode = CreateStatNode("atk_1", StatType.Attack, 0.03f);
            _allNodes.Add(hpNode);
            _allNodes.Add(atkNode);

            _tree.TryUnlock(hpNode);
            _tree.TryUnlock(atkNode);

            Assert.AreEqual(1.05f, _tree.GetStatBonus(StatType.Health, _allNodes), 0.0001f);
            Assert.AreEqual(1.03f, _tree.GetStatBonus(StatType.Attack, _allNodes), 0.0001f);
            // Unaffected stat stays at 1.0
            Assert.AreEqual(1.0f, _tree.GetStatBonus(StatType.Speed, _allNodes));
        }

        [Test]
        public void GetStatBonus_IgnoresSpecialUnlockNodes()
        {
            var statNode = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var specialNode = CreateSpecialNode("self_revive", "self_revive");
            _allNodes.Add(statNode);
            _allNodes.Add(specialNode);

            _tree.TryUnlock(statNode);
            _tree.TryUnlock(specialNode);

            // Special unlock should not affect health bonus
            Assert.AreEqual(1.05f, _tree.GetStatBonus(StatType.Health, _allNodes), 0.0001f);
        }

        // ── Special Unlocks ──────────────────────────────────────────────────

        [Test]
        public void HasSpecialUnlock_NotUnlocked_ReturnsFalse()
        {
            var node = CreateSpecialNode("self_revive", "self_revive");
            _allNodes.Add(node);

            Assert.IsFalse(_tree.HasSpecialUnlock("self_revive", _allNodes));
        }

        [Test]
        public void HasSpecialUnlock_Unlocked_ReturnsTrue()
        {
            var node = CreateSpecialNode("self_revive", "self_revive");
            _allNodes.Add(node);
            _tree.TryUnlock(node);

            Assert.IsTrue(_tree.HasSpecialUnlock("self_revive", _allNodes));
        }

        [Test]
        public void HasSpecialUnlock_DifferentId_ReturnsFalse()
        {
            var node = CreateSpecialNode("self_revive", "self_revive");
            _allNodes.Add(node);
            _tree.TryUnlock(node);

            Assert.IsFalse(_tree.HasSpecialUnlock("third_ritual_choice", _allNodes));
        }

        // ── Save/Load ────────────────────────────────────────────────────────

        [Test]
        public void LoadFromData_RestoresUnlockedNodes()
        {
            var node1 = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var node2 = CreateStatNode("atk_1", StatType.Attack, 0.03f);
            _allNodes.Add(node1);
            _allNodes.Add(node2);

            var data = new MetaProgressionData
            {
                unlockedNodeIds = new List<string> { "hp_1", "atk_1" }
            };

            _tree.LoadFromData(data);

            Assert.IsTrue(_tree.IsUnlocked("hp_1"));
            Assert.IsTrue(_tree.IsUnlocked("atk_1"));
            Assert.AreEqual(1.05f, _tree.GetStatBonus(StatType.Health, _allNodes), 0.0001f);
        }

        [Test]
        public void LoadFromData_ClearsPreviousState()
        {
            var node = CreateStatNode("hp_1", StatType.Health, 0.05f);
            _allNodes.Add(node);
            _tree.TryUnlock(node);

            // Load empty data — should clear hp_1
            _tree.LoadFromData(MetaProgressionData.Empty());

            Assert.IsFalse(_tree.IsUnlocked("hp_1"));
            Assert.AreEqual(1.0f, _tree.GetStatBonus(StatType.Health, _allNodes));
        }

        [Test]
        public void GetUnlockedNodeIds_ReturnsAllUnlocked()
        {
            var node1 = CreateStatNode("hp_1", StatType.Health, 0.05f);
            var node2 = CreateStatNode("atk_1", StatType.Attack, 0.03f);
            _allNodes.Add(node1);
            _allNodes.Add(node2);

            _tree.TryUnlock(node1);
            _tree.TryUnlock(node2);

            var ids = _tree.GetUnlockedNodeIds();
            Assert.AreEqual(2, ids.Count);
            Assert.Contains("hp_1", ids);
            Assert.Contains("atk_1", ids);
        }

        // ── CanUnlock ────────────────────────────────────────────────────────

        [Test]
        public void CanUnlock_AvailableNode_ReturnsTrue()
        {
            var node = CreateStatNode("hp_1", StatType.Health, 0.05f);
            Assert.IsTrue(_tree.CanUnlock(node));
        }

        [Test]
        public void CanUnlock_AlreadyUnlocked_ReturnsFalse()
        {
            var node = CreateStatNode("hp_1", StatType.Health, 0.05f);
            _tree.TryUnlock(node);
            Assert.IsFalse(_tree.CanUnlock(node));
        }

        [Test]
        public void CanUnlock_PrerequisiteNotMet_ReturnsFalse()
        {
            var node = CreateStatNode("hp_2", StatType.Health, 0.05f, "hp_1");
            Assert.IsFalse(_tree.CanUnlock(node));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static SoulTreeNodeData CreateStatNode(string id, StatType stat, float bonus, params string[] prereqs)
        {
            var node = ScriptableObject.CreateInstance<SoulTreeNodeData>();
            node.nodeId = id;
            node.displayName = id;
            node.nodeType = SoulTreeNodeType.StatBonus;
            node.affectedStat = stat;
            node.bonusValue = bonus;
            node.crystalCost = 50;
            node.prerequisiteNodeIds = new List<string>(prereqs);
            return node;
        }

        private static SoulTreeNodeData CreateSpecialNode(string id, string unlockId, params string[] prereqs)
        {
            var node = ScriptableObject.CreateInstance<SoulTreeNodeData>();
            node.nodeId = id;
            node.displayName = id;
            node.nodeType = SoulTreeNodeType.SpecialUnlock;
            node.specialUnlockId = unlockId;
            node.crystalCost = 100;
            node.prerequisiteNodeIds = new List<string>(prereqs);
            return node;
        }
    }
}
