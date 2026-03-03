using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates initial RitualData ScriptableObject assets for the Fire and Lightning families.
    ///
    /// <para>Run once via <b>TomatoFighters → Create Ritual Assets</b> in the Unity menu bar.
    /// Assets are written to
    /// <c>Assets/ScriptableObjects/Rituals/{Family}/{Name}Ritual.asset</c>.</para>
    ///
    /// <para>Re-running overwrites existing assets — safe to re-run if values change.</para>
    /// </summary>
    public static class RitualDataCreator
    {
        private const string ROOT = "Assets/ScriptableObjects/Rituals";

        [MenuItem("TomatoFighters/Create Ritual Assets")]
        public static void CreateAllRitualAssets()
        {
            EnsureFolder(ROOT);

            CreateFireFamily();
            CreateLightningFamily();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RitualDataCreator] Created 8 ritual assets (Fire + Lightning families).");
        }

        // ── Fire Family ───────────────────────────────────────────────────────

        private static void CreateFireFamily()
        {
            string folder = $"{ROOT}/Fire";
            EnsureFolder(folder);

            // Core — Burn: DoT on every strike, stacks up to 3
            Save(folder, "BurnRitual", Make(
                ritualName:  "Burn",
                description: "Strikes ignite enemies, dealing fire damage over time.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Fire_Burn",
                l1: (baseValue: 5f,  maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 5f,  maxStacks: 4, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 5f,  maxStacks: 5, stackMult: 1.30f, power: 1.0f)
            ));

            // General — Blazing Dash: dash leaves a fire trail
            Save(folder, "BlazingDashRitual", Make(
                ritualName:  "Blazing Dash",
                description: "Dashing leaves a trail of flames that burns enemies who enter it.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Fire_BlazingDash",
                l1: (baseValue: 15f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 15f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 15f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Flame Strike: finishers deal bonus fire damage
            Save(folder, "FlameStrikeRitual", Make(
                ritualName:  "Flame Strike",
                description: "Finisher attacks erupt in flames, dealing amplified fire damage.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Fire_FlameStrike",
                l1: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Ember Shield: deflecting ignites the attacker
            Save(folder, "EmberShieldRitual", Make(
                ritualName:  "Ember Shield",
                description: "Deflecting an attack ignites the attacker with burning embers.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDeflect,
                effectId:    "Fire_EmberShield",
                l1: (baseValue: 10f, maxStacks: 2, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 10f, maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 10f, maxStacks: 4, stackMult: 1.25f, power: 1.0f)
            ));
        }

        // ── Lightning Family ──────────────────────────────────────────────────

        private static void CreateLightningFamily()
        {
            string folder = $"{ROOT}/Lightning";
            EnsureFolder(folder);

            // Core — Chain Lightning: strikes arc to nearby enemies
            Save(folder, "ChainLightningRitual", Make(
                ritualName:  "Chain Lightning",
                description: "Strikes arc lightning to nearby enemies, dealing bonus damage.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Lightning_Chain",
                l1: (baseValue: 8f,  maxStacks: 3, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 8f,  maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 8f,  maxStacks: 5, stackMult: 1.20f, power: 1.0f)
            ));

            // General — Lightning Strike: skills call down a lightning bolt
            Save(folder, "LightningStrikeRitual", Make(
                ritualName:  "Lightning Strike",
                description: "Using a skill calls down a lightning bolt on the target.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnSkill,
                effectId:    "Lightning_Strike",
                l1: (baseValue: 25f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 25f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 25f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Shock Wave: finishers release a lightning shock wave
            Save(folder, "ShockWaveRitual", Make(
                ritualName:  "Shock Wave",
                description: "Finisher attacks release a shock wave that stuns nearby enemies.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Lightning_ShockWave",
                l1: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Static Field: taking damage builds static charge
            Save(folder, "StaticFieldRitual", Make(
                ritualName:  "Static Field",
                description: "Taking damage builds static charge that discharges on the next strike.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnTakeDamage,
                effectId:    "Lightning_StaticField",
                l1: (baseValue: 5f,  maxStacks: 4, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 5f,  maxStacks: 5, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 5f,  maxStacks: 6, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static RitualData Make(
            string ritualName,
            string description,
            RitualFamily family,
            RitualCategory category,
            RitualTrigger trigger,
            string effectId,
            (float baseValue, int maxStacks, float stackMult, float power) l1,
            (float baseValue, int maxStacks, float stackMult, float power) l2,
            (float baseValue, int maxStacks, float stackMult, float power) l3)
        {
            var data             = ScriptableObject.CreateInstance<RitualData>();
            data.ritualName      = ritualName;
            data.description     = description;
            data.family          = family;
            data.category        = category;
            data.trigger         = trigger;
            data.effectId        = effectId;

            data.level1 = new RitualLevelData
                { baseValue = l1.baseValue, maxStacks = l1.maxStacks, stackingMultiplier = l1.stackMult, ritualPower = l1.power };
            data.level2 = new RitualLevelData
                { baseValue = l2.baseValue, maxStacks = l2.maxStacks, stackingMultiplier = l2.stackMult, ritualPower = l2.power };
            data.level3 = new RitualLevelData
                { baseValue = l3.baseValue, maxStacks = l3.maxStacks, stackingMultiplier = l3.stackMult, ritualPower = l3.power };

            return data;
        }

        private static void Save(string folder, string fileName, RitualData data)
        {
            string path = $"{folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RitualData>(path);
            if (existing != null)
            {
                // Overwrite — copy values onto existing asset to preserve references
                EditorUtility.CopySerialized(data, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(data);
            }
            else
            {
                AssetDatabase.CreateAsset(data, path);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int lastSlash = path.LastIndexOf('/');
                string parent = path[..lastSlash];
                string folder = path[(lastSlash + 1)..];
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
