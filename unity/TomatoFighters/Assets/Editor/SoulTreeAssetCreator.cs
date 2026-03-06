using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Editor menu items to generate default Soul Tree ScriptableObject assets.
    /// Creates SoulTreeNodeData instances and a SoulTreeConfig that references them.
    /// </summary>
    public static class SoulTreeAssetCreator
    {
        private const string BasePath = "Assets/ScriptableObjects/SoulTree";
        private const string NodesPath = BasePath + "/Nodes";

        [MenuItem("TomatoFighters/Create Soul Tree Assets")]
        public static void CreateAllSoulTreeAssets()
        {
            EnsureDirectories();

            var nodes = new List<SoulTreeNodeData>();

            // ── Stat Bonus Nodes ─────────────────────────────────────────
            nodes.Add(CreateStatNode("hp_1",  "Vitality I",    "+5% Health",   StatType.Health,  0.05f, 50));
            nodes.Add(CreateStatNode("hp_2",  "Vitality II",   "+5% Health",   StatType.Health,  0.05f, 75,  "hp_1"));
            nodes.Add(CreateStatNode("hp_3",  "Vitality III",  "+10% Health",  StatType.Health,  0.10f, 120, "hp_2"));
            nodes.Add(CreateStatNode("atk_1", "Ferocity I",    "+3% Attack",   StatType.Attack,  0.03f, 50));
            nodes.Add(CreateStatNode("atk_2", "Ferocity II",   "+5% Attack",   StatType.Attack,  0.05f, 80,  "atk_1"));
            nodes.Add(CreateStatNode("def_1", "Fortitude I",   "+3% Defense",  StatType.Defense, 0.03f, 50));
            nodes.Add(CreateStatNode("def_2", "Fortitude II",  "+5% Defense",  StatType.Defense, 0.05f, 80,  "def_1"));
            nodes.Add(CreateStatNode("spd_1", "Agility I",     "+3% Speed",    StatType.Speed,   0.03f, 60));
            nodes.Add(CreateStatNode("spd_2", "Agility II",    "+4% Speed",    StatType.Speed,   0.04f, 90,  "spd_1"));

            // ── Special Unlock Nodes ─────────────────────────────────────
            nodes.Add(CreateSpecialNode("third_ritual", "Third Ritual Choice",
                "Unlocks a third option when selecting rituals.", "third_ritual_choice", 150));
            nodes.Add(CreateSpecialNode("self_revive", "Second Wind",
                "Revive once per run with 25% HP.", "self_revive", 200));
            nodes.Add(CreateSpecialNode("rare_chance", "Fortune's Favor",
                "Increased chance of rare drops.", "rare_chance_boost", 100));

            // ── Soul Tree Config ─────────────────────────────────────────
            var config = ScriptableObject.CreateInstance<SoulTreeConfig>();
            config.nodes = nodes;
            AssetDatabase.CreateAsset(config, $"{BasePath}/SoulTreeConfig.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SoulTreeAssetCreator] Created {nodes.Count} nodes + 1 config at {BasePath}");
        }

        private static SoulTreeNodeData CreateStatNode(
            string id, string displayName, string description,
            StatType stat, float bonus, int cost, string prereq = null)
        {
            var node = ScriptableObject.CreateInstance<SoulTreeNodeData>();
            node.nodeId = id;
            node.displayName = displayName;
            node.description = description;
            node.nodeType = SoulTreeNodeType.StatBonus;
            node.affectedStat = stat;
            node.bonusValue = bonus;
            node.crystalCost = cost;
            node.prerequisiteNodeIds = prereq != null
                ? new List<string> { prereq }
                : new List<string>();

            AssetDatabase.CreateAsset(node, $"{NodesPath}/{id}.asset");
            return node;
        }

        private static SoulTreeNodeData CreateSpecialNode(
            string id, string displayName, string description,
            string unlockId, int cost, string prereq = null)
        {
            var node = ScriptableObject.CreateInstance<SoulTreeNodeData>();
            node.nodeId = id;
            node.displayName = displayName;
            node.description = description;
            node.nodeType = SoulTreeNodeType.SpecialUnlock;
            node.specialUnlockId = unlockId;
            node.crystalCost = cost;
            node.prerequisiteNodeIds = prereq != null
                ? new List<string> { prereq }
                : new List<string>();

            AssetDatabase.CreateAsset(node, $"{NodesPath}/{id}.asset");
            return node;
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(BasePath))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "SoulTree");
            if (!AssetDatabase.IsValidFolder(NodesPath))
                AssetDatabase.CreateFolder(BasePath, "Nodes");
        }
    }
}
