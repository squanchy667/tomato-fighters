using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates sample TrinketData ScriptableObjects for testing and design iteration.
    /// Safe to re-run — overwrites existing assets in place (preserves GUIDs).
    /// Run via menu: <b>Tools > TomatoFighters > Create Trinket Data</b>.
    /// </summary>
    public static class CreateTrinketData
    {
        private const string FOLDER = "Assets/ScriptableObjects/Trinkets";

        [MenuItem("Tools/TomatoFighters/Create Trinket Data")]
        public static void Execute()
        {
            EnsureFolderExists(FOLDER);

            // Always-active trinkets
            CreateTrinket("IronBand", "Iron Band",
                "A sturdy ring that bolsters health.",
                StatType.Health, 10f, ModifierType.Flat,
                TrinketTriggerType.Always, 0f);

            CreateTrinket("SharpFang", "Sharp Fang",
                "A razor-edged tooth that enhances attack power.",
                StatType.Attack, 0.1f, ModifierType.Percent,
                TrinketTriggerType.Always, 0f);

            CreateTrinket("SwiftFeather", "Swift Feather",
                "A light feather that increases movement speed.",
                StatType.Speed, 0.08f, ModifierType.Percent,
                TrinketTriggerType.Always, 0f);

            // Conditional trinkets
            CreateTrinket("KillerInstinct", "Killer Instinct",
                "Attack surges after slaying an enemy.",
                StatType.Attack, 0.2f, ModifierType.Percent,
                TrinketTriggerType.OnKill, 5f);

            CreateTrinket("DeflectorCharm", "Deflector Charm",
                "Defense spikes after a successful deflect.",
                StatType.Defense, 3f, ModifierType.Flat,
                TrinketTriggerType.OnDeflect, 4f);

            CreateTrinket("DodgersLuck", "Dodger's Luck",
                "Critical chance rises after dodging.",
                StatType.CritChance, 0.1f, ModifierType.Percent,
                TrinketTriggerType.OnDodge, 6f);

            CreateTrinket("FinisherRush", "Finisher Rush",
                "Speed burst after landing a finisher.",
                StatType.Speed, 0.15f, ModifierType.Percent,
                TrinketTriggerType.OnFinisher, 3f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CreateTrinketData] Done. Sample trinkets created at " + FOLDER);
        }

        private static void CreateTrinket(string fileName, string displayName, string desc,
            StatType stat, float value, ModifierType modType,
            TrinketTriggerType trigger, float duration)
        {
            string path = $"{FOLDER}/{fileName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<TrinketData>(path);
            TrinketData data;

            if (existing != null)
            {
                data = existing;
            }
            else
            {
                data = ScriptableObject.CreateInstance<TrinketData>();
            }

            data.displayName = displayName;
            data.description = desc;
            data.affectedStat = stat;
            data.modifierValue = value;
            data.modifierType = modType;
            data.triggerType = trigger;
            data.buffDuration = duration;

            if (!AssetDatabase.Contains(data))
                AssetDatabase.CreateAsset(data, path);
            else
                EditorUtility.SetDirty(data);
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
