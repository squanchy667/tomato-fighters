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
            CreateWaterFamily();
            CreateThornFamily();
            CreateGaleFamily();
            CreateTimeFamily();
            CreateCosmicFamily();
            CreateNecroFamily();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RitualDataCreator] Created 32 ritual assets (all 8 families).");
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

        // ── Water Family (T048) ─────────────────────────────────────────────

        private static void CreateWaterFamily()
        {
            string folder = $"{ROOT}/Water";
            EnsureFolder(folder);

            // Core — Tidal Wave: stacking water damage on strike
            Save(folder, "TidalWaveRitual", Make(
                ritualName:  "Tidal Wave",
                description: "Strikes build tidal energy, dealing increasing water damage.",
                family:      RitualFamily.Water,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Water_TidalWave",
                l1: (baseValue: 6f,  maxStacks: 3, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 6f,  maxStacks: 4, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 6f,  maxStacks: 5, stackMult: 1.25f, power: 1.0f)
            ));

            // General — Wave Dash: dash creates a water zone
            Save(folder, "WaveDashRitual", Make(
                ritualName:  "Wave Dash",
                description: "Dashing creates a wave zone that slows and damages enemies.",
                family:      RitualFamily.Water,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Water_WaveDash",
                l1: (baseValue: 18f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 18f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 18f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Torrent: finisher burst
            Save(folder, "TorrentRitual", Make(
                ritualName:  "Torrent",
                description: "Finisher attacks unleash a torrent of water damage.",
                family:      RitualFamily.Water,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Water_Torrent",
                l1: (baseValue: 25f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 25f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 25f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Riptide: dodge pull (stacking)
            Save(folder, "RiptideRitual", Make(
                ritualName:  "Riptide",
                description: "Dodging creates a riptide that pulls nearby enemies closer.",
                family:      RitualFamily.Water,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDodge,
                effectId:    "Water_Riptide",
                l1: (baseValue: 8f,  maxStacks: 3, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 8f,  maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 8f,  maxStacks: 5, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Thorn Family (T048) ─────────────────────────────────────────────

        private static void CreateThornFamily()
        {
            string folder = $"{ROOT}/Thorn";
            EnsureFolder(folder);

            // Core — Bramble Knives: stacking thorn projectiles on strike
            Save(folder, "BrambleKnivesRitual", Make(
                ritualName:  "Bramble Knives",
                description: "Strikes launch thorn projectiles that stack for more damage.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Thorn_BrambleKnives",
                l1: (baseValue: 7f,  maxStacks: 3, stackMult: 1.12f, power: 1.0f),
                l2: (baseValue: 7f,  maxStacks: 4, stackMult: 1.17f, power: 1.0f),
                l3: (baseValue: 7f,  maxStacks: 5, stackMult: 1.22f, power: 1.0f)
            ));

            // General — Thorn Guard: deflect reflects thorns (stacking)
            Save(folder, "ThornGuardRitual", Make(
                ritualName:  "Thorn Guard",
                description: "Deflecting an attack sends thorns back at the attacker.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDeflect,
                effectId:    "Thorn_ThornGuard",
                l1: (baseValue: 7f,  maxStacks: 3, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 7f,  maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 7f,  maxStacks: 5, stackMult: 1.20f, power: 1.0f)
            ));

            // Enhancement — Vine Burst: finisher AoE burst
            Save(folder, "VineBurstRitual", Make(
                ritualName:  "Vine Burst",
                description: "Finisher attacks cause vines to erupt from the ground in an AoE.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Thorn_VineBurst",
                l1: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Bramble Dodge: dodge spawns thorn patch (stacking)
            Save(folder, "BrambleDodgeRitual", Make(
                ritualName:  "Bramble Dodge",
                description: "Dodging leaves behind a patch of brambles that damages enemies.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDodge,
                effectId:    "Thorn_BrambleDodge",
                l1: (baseValue: 6f,  maxStacks: 2, stackMult: 1.12f, power: 1.0f),
                l2: (baseValue: 6f,  maxStacks: 3, stackMult: 1.17f, power: 1.0f),
                l3: (baseValue: 6f,  maxStacks: 4, stackMult: 1.22f, power: 1.0f)
            ));
        }

        // ── Gale Family (T048) ──────────────────────────────────────────────

        private static void CreateGaleFamily()
        {
            string folder = $"{ROOT}/Gale";
            EnsureFolder(folder);

            // Core — Updraft: stacking lift on strike
            Save(folder, "UpdraftRitual", Make(
                ritualName:  "Updraft",
                description: "Strikes create updrafts that lift enemies, stacking for more lift.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Gale_Updraft",
                l1: (baseValue: 6f,  maxStacks: 3, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 6f,  maxStacks: 4, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 6f,  maxStacks: 5, stackMult: 1.25f, power: 1.0f)
            ));

            // General — Tailwind: dash grants speed buff
            Save(folder, "TailwindRitual", Make(
                ritualName:  "Tailwind",
                description: "Dashing grants a temporary speed boost.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Gale_Tailwind",
                l1: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Cyclone: finisher AoE
            Save(folder, "CycloneRitual", Make(
                ritualName:  "Cyclone",
                description: "Finisher attacks spawn a cyclone that pulls and damages nearby enemies.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Gale_Cyclone",
                l1: (baseValue: 24f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 24f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 24f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Gale Jump: jump slam (stacking)
            Save(folder, "GaleJumpRitual", Make(
                ritualName:  "Gale Jump",
                description: "Jumping creates a wind slam on landing that damages enemies below.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnJump,
                effectId:    "Gale_GaleJump",
                l1: (baseValue: 8f,  maxStacks: 3, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 8f,  maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 8f,  maxStacks: 5, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Time Family (T048) ──────────────────────────────────────────────

        private static void CreateTimeFamily()
        {
            string folder = $"{ROOT}/Time";
            EnsureFolder(folder);

            // Core — Echo Strike: stacking repeat on strike
            Save(folder, "EchoStrikeRitual", Make(
                ritualName:  "Echo Strike",
                description: "Strikes echo as ghost copies, repeating damage. Stacks increase copies.",
                family:      RitualFamily.Time,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Time_EchoStrike",
                l1: (baseValue: 5f,  maxStacks: 3, stackMult: 1.18f, power: 1.0f),
                l2: (baseValue: 5f,  maxStacks: 4, stackMult: 1.23f, power: 1.0f),
                l3: (baseValue: 5f,  maxStacks: 5, stackMult: 1.28f, power: 1.0f)
            ));

            // General — Slow Field: skill spawns slow zone
            Save(folder, "SlowFieldRitual", Make(
                ritualName:  "Slow Field",
                description: "Using a skill creates a temporal zone that slows enemies.",
                family:      RitualFamily.Time,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnSkill,
                effectId:    "Time_SlowField",
                l1: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Time Burst: finisher AoE freeze
            Save(folder, "TimeBurstRitual", Make(
                ritualName:  "Time Burst",
                description: "Finisher attacks release a time burst that briefly freezes nearby enemies.",
                family:      RitualFamily.Time,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Time_TimeBurst",
                l1: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Temporal Dodge: rewind on dodge (stacking)
            Save(folder, "TemporalDodgeRitual", Make(
                ritualName:  "Temporal Dodge",
                description: "Dodging stores a temporal anchor; rewind to it after a delay.",
                family:      RitualFamily.Time,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDodge,
                effectId:    "Time_TemporalDodge",
                l1: (baseValue: 5f,  maxStacks: 2, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 5f,  maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 5f,  maxStacks: 4, stackMult: 1.25f, power: 1.0f)
            ));
        }

        // ── Cosmic Family (T048) ────────────────────────────────────────────

        private static void CreateCosmicFamily()
        {
            string folder = $"{ROOT}/Cosmic";
            EnsureFolder(folder);

            // Core — Star Burst: stacking AoE on strike
            Save(folder, "StarBurstRitual", Make(
                ritualName:  "Star Burst",
                description: "Strikes release star fragments in an AoE. Stacks increase radius.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Cosmic_StarBurst",
                l1: (baseValue: 7f,  maxStacks: 3, stackMult: 1.12f, power: 1.0f),
                l2: (baseValue: 7f,  maxStacks: 4, stackMult: 1.17f, power: 1.0f),
                l3: (baseValue: 7f,  maxStacks: 5, stackMult: 1.22f, power: 1.0f)
            ));

            // General — Cosmic Dash: dash leaves cosmic trail
            Save(folder, "CosmicDashRitual", Make(
                ritualName:  "Cosmic Dash",
                description: "Dashing leaves a trail of cosmic energy that damages enemies.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Cosmic_CosmicDash",
                l1: (baseValue: 16f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 16f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 16f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Supernova: finisher massive burst
            Save(folder, "SupernovaRitual", Make(
                ritualName:  "Supernova",
                description: "Finisher attacks trigger a supernova dealing massive cosmic damage.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Cosmic_Supernova",
                l1: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Void Shield: counter on take damage (stacking)
            Save(folder, "VoidShieldRitual", Make(
                ritualName:  "Void Shield",
                description: "Taking damage builds void energy that counter-damages the attacker.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnTakeDamage,
                effectId:    "Cosmic_VoidShield",
                l1: (baseValue: 6f,  maxStacks: 4, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 6f,  maxStacks: 5, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 6f,  maxStacks: 6, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Necro Family (T048) ─────────────────────────────────────────────

        private static void CreateNecroFamily()
        {
            string folder = $"{ROOT}/Necro";
            EnsureFolder(folder);

            // Core — Life Drain: stacking heal on strike
            Save(folder, "LifeDrainRitual", Make(
                ritualName:  "Life Drain",
                description: "Strikes drain life from enemies, healing the player. Stacks increase healing.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Necro_LifeDrain",
                l1: (baseValue: 5f,  maxStacks: 3, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 5f,  maxStacks: 4, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 5f,  maxStacks: 5, stackMult: 1.25f, power: 1.0f)
            ));

            // General — Soul Harvest: buff on kill
            Save(folder, "SoulHarvestRitual", Make(
                ritualName:  "Soul Harvest",
                description: "Killing an enemy harvests their soul, granting a temporary damage buff.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnKill,
                effectId:    "Necro_SoulHarvest",
                l1: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 22f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Death Blow: finisher burst
            Save(folder, "DeathBlowRitual", Make(
                ritualName:  "Death Blow",
                description: "Finisher attacks deal a massive burst of necro damage.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Necro_DeathBlow",
                l1: (baseValue: 26f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 26f, maxStacks: 1, stackMult: 1.0f, power: 1.5f),
                l3: (baseValue: 26f, maxStacks: 1, stackMult: 1.0f, power: 2.0f)
            ));

            // Enhancement — Bone Shield: minion on deflect (stacking)
            Save(folder, "BoneShieldRitual", Make(
                ritualName:  "Bone Shield",
                description: "Deflecting an attack summons a bone minion that absorbs damage.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDeflect,
                effectId:    "Necro_BoneShield",
                l1: (baseValue: 8f,  maxStacks: 2, stackMult: 1.12f, power: 1.0f),
                l2: (baseValue: 8f,  maxStacks: 3, stackMult: 1.17f, power: 1.0f),
                l3: (baseValue: 8f,  maxStacks: 4, stackMult: 1.22f, power: 1.0f)
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
