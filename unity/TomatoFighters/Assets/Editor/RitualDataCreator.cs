using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates all RitualData ScriptableObject assets for all 8 elemental families + 6 Twin rituals.
    ///
    /// <para>Run via <b>TomatoFighters → Create Ritual Assets</b> in the Unity menu bar.
    /// Assets are written to
    /// <c>Assets/ScriptableObjects/Rituals/{Family}/{Name}Ritual.asset</c>.</para>
    ///
    /// <para>Re-running overwrites existing assets — safe to re-run if values change.</para>
    ///
    /// <para><b>T049 Balance Notes:</b>
    /// <list type="bullet">
    ///   <item>Level scaling is now EXPLICIT in data: L2 = base × 1.5, L3 = base × 2.0.
    ///         All <c>power</c> fields are 1.0 — scaling is visible in the asset.</item>
    ///   <item>High-impact single-hit (finisher/kill bursts): base 28–32 — same threat floor
    ///         across families so no Enhancement ritual dominates at L1.</item>
    ///   <item>DoT/persistent stacking (Core on-strike): base 4–6 per tick — balanced so
    ///         no family's stacking ritual snowballs faster than others at L1.</item>
    ///   <item>Mobility (General on-dash/skill): base 15–20 — situational, not dominant.</item>
    ///   <item>Shield/Reactive (on-deflect, on-dodge, on-takeDamage): base 6–10 —
    ///         rewards defensive play without making defence mandatory.</item>
    ///   <item>Power budget per family is roughly equal at L1; differentiation comes from
    ///         trigger type and stacking behaviour, not raw number inflation.</item>
    /// </list></para>
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
            CreateTwinRituals();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RitualDataCreator] Created 38 ritual assets (8 families + 6 twins).");
        }

        [MenuItem("TomatoFighters/Create Twin Ritual Assets")]
        public static void CreateTwinRitualAssets()
        {
            EnsureFolder(ROOT);
            CreateTwinRituals();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RitualDataCreator] Created 6 Twin ritual assets.");
        }

        // ── Fire Family ───────────────────────────────────────────────────────
        // Power budget: balanced DoT + reactive shield + one strong finisher + one mobility.
        // Burn is intentionally moderate (DoT 5/tick) — it's sticky, not a burst.

        private static void CreateFireFamily()
        {
            string folder = $"{ROOT}/Fire";
            EnsureFolder(folder);

            // Core — Burn: DoT on every strike, stacks up to 3.
            // Target: DoT range 4–6/tick at L1. Power = 1.0 at all levels; explicit level values.
            Save(folder, "BurnRitual", Make(
                ritualName:  "Burn",
                description: "Strikes ignite enemies, dealing fire damage over time.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Fire_Burn",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.30f, power: 1.0f)
            ));

            // General — Blazing Dash: dash leaves a fire trail.
            // Target: mobility range 15–20 at L1. Explicit scaling, no power multiplier.
            Save(folder, "BlazingDashRitual", Make(
                ritualName:  "Blazing Dash",
                description: "Dashing leaves a trail of flames that burns enemies who enter it.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Fire_BlazingDash",
                l1: (baseValue: 18f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 27f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 36f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Flame Strike: finishers deal bonus fire damage.
            // Target: high-impact 28–32 at L1.
            Save(folder, "FlameStrikeRitual", Make(
                ritualName:  "Flame Strike",
                description: "Finisher attacks erupt in flames, dealing amplified fire damage.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Fire_FlameStrike",
                l1: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 45f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 60f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Ember Shield: deflecting ignites the attacker.
            // Target: shield/reactive range 6–10 at L1.
            Save(folder, "EmberShieldRitual", Make(
                ritualName:  "Ember Shield",
                description: "Deflecting an attack ignites the attacker with burning embers.",
                family:      RitualFamily.Fire,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDeflect,
                effectId:    "Fire_EmberShield",
                l1: (baseValue: 8f,  maxStacks: 2, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 12f, maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 16f, maxStacks: 4, stackMult: 1.25f, power: 1.0f)
            ));
        }

        // ── Lightning Family ──────────────────────────────────────────────────
        // Power budget: chain (persistent) + bolt (skill-triggered) + shock (finisher) + charge (reactive).
        // Lightning Strike normalised up to 28 (was 25) — in line with high-impact target.

        private static void CreateLightningFamily()
        {
            string folder = $"{ROOT}/Lightning";
            EnsureFolder(folder);

            // Core — Chain Lightning: strikes arc to nearby enemies.
            // Target: DoT/persistent range 4–6 at L1 (chain counts as burst-per-hit, not DoT).
            // Kept at 5 for parity with Burn.
            Save(folder, "ChainLightningRitual", Make(
                ritualName:  "Chain Lightning",
                description: "Strikes arc lightning to nearby enemies, dealing bonus damage.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Lightning_Chain",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.30f, power: 1.0f)
            ));

            // General — Lightning Strike: skills call down a lightning bolt.
            // Normalised from 25 → 28 to hit high-impact floor. Explicit level scaling.
            Save(folder, "LightningStrikeRitual", Make(
                ritualName:  "Lightning Strike",
                description: "Using a skill calls down a lightning bolt on the target.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnSkill,
                effectId:    "Lightning_Strike",
                l1: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 42f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 56f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Shock Wave: finishers release a lightning shock wave.
            // Normalised from 20 → 28 to hit high-impact floor.
            Save(folder, "ShockWaveRitual", Make(
                ritualName:  "Shock Wave",
                description: "Finisher attacks release a shock wave that stuns nearby enemies.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Lightning_ShockWave",
                l1: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 42f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 56f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Static Field: taking damage builds static charge.
            // Target: shield/reactive 6–10. Normalised from 5 → 6.
            Save(folder, "StaticFieldRitual", Make(
                ritualName:  "Static Field",
                description: "Taking damage builds static charge that discharges on the next strike.",
                family:      RitualFamily.Lightning,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnTakeDamage,
                effectId:    "Lightning_StaticField",
                l1: (baseValue: 6f,  maxStacks: 4, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 9f,  maxStacks: 5, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 12f, maxStacks: 6, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Water Family (T048) ─────────────────────────────────────────────
        // Power budget: tidal (stacking) + wave (mobility) + torrent (finisher) + riptide (dodge).
        // Torrent normalised from 25 → 30 to hit high-impact floor.

        private static void CreateWaterFamily()
        {
            string folder = $"{ROOT}/Water";
            EnsureFolder(folder);

            // Core — Tidal Wave: stacking water damage on strike.
            // Target: DoT range 4–6 at L1. Matched to Fire/Lightning/Time cores at 5.
            Save(folder, "TidalWaveRitual", Make(
                ritualName:  "Tidal Wave",
                description: "Strikes build tidal energy, dealing increasing water damage.",
                family:      RitualFamily.Water,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Water_TidalWave",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.30f, power: 1.0f)
            ));

            // General — Wave Dash: dash creates a water zone.
            // Target: mobility range 15–20 at L1.
            Save(folder, "WaveDashRitual", Make(
                ritualName:  "Wave Dash",
                description: "Dashing creates a wave zone that slows and damages enemies.",
                family:      RitualFamily.Water,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Water_WaveDash",
                l1: (baseValue: 18f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 27f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 36f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Torrent: finisher burst.
            // Normalised from 25 → 30 to match high-impact floor.
            Save(folder, "TorrentRitual", Make(
                ritualName:  "Torrent",
                description: "Finisher attacks unleash a torrent of water damage.",
                family:      RitualFamily.Water,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Water_Torrent",
                l1: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 45f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 60f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Riptide: dodge pull (stacking).
            // Target: shield/reactive 6–10 at L1.
            Save(folder, "RiptideRitual", Make(
                ritualName:  "Riptide",
                description: "Dodging creates a riptide that pulls nearby enemies closer.",
                family:      RitualFamily.Water,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDodge,
                effectId:    "Water_Riptide",
                l1: (baseValue: 8f,  maxStacks: 3, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 12f, maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 16f, maxStacks: 5, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Thorn Family (T048) ─────────────────────────────────────────────
        // Power budget: knives (stacking) + guard (reactive deflect) + vine (finisher) + bramble (dodge).
        // Vine Burst normalised from 22 → 30 to match high-impact floor.

        private static void CreateThornFamily()
        {
            string folder = $"{ROOT}/Thorn";
            EnsureFolder(folder);

            // Core — Bramble Knives: stacking thorn projectiles on strike.
            // Target: DoT range 4–6 at L1. Normalised from 7 → 5 for parity.
            Save(folder, "BrambleKnivesRitual", Make(
                ritualName:  "Bramble Knives",
                description: "Strikes launch thorn projectiles that stack for more damage.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Thorn_BrambleKnives",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.30f, power: 1.0f)
            ));

            // Enhancement — Thorn Guard: deflect reflects thorns (stacking).
            // Target: shield/reactive 6–10 at L1.
            Save(folder, "ThornGuardRitual", Make(
                ritualName:  "Thorn Guard",
                description: "Deflecting an attack sends thorns back at the attacker.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDeflect,
                effectId:    "Thorn_ThornGuard",
                l1: (baseValue: 8f,  maxStacks: 3, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 12f, maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 16f, maxStacks: 5, stackMult: 1.20f, power: 1.0f)
            ));

            // Enhancement — Vine Burst: finisher AoE burst.
            // Normalised from 22 → 30 to hit high-impact floor.
            Save(folder, "VineBurstRitual", Make(
                ritualName:  "Vine Burst",
                description: "Finisher attacks cause vines to erupt from the ground in an AoE.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Thorn_VineBurst",
                l1: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 45f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 60f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Bramble Dodge: dodge spawns thorn patch (stacking).
            // Target: shield/reactive 6–10. Kept at 6, dodge has inherent timing reward.
            Save(folder, "BrambleDodgeRitual", Make(
                ritualName:  "Bramble Dodge",
                description: "Dodging leaves behind a patch of brambles that damages enemies.",
                family:      RitualFamily.Thorn,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDodge,
                effectId:    "Thorn_BrambleDodge",
                l1: (baseValue: 6f,  maxStacks: 2, stackMult: 1.12f, power: 1.0f),
                l2: (baseValue: 9f,  maxStacks: 3, stackMult: 1.17f, power: 1.0f),
                l3: (baseValue: 12f, maxStacks: 4, stackMult: 1.22f, power: 1.0f)
            ));
        }

        // ── Gale Family (T048) ──────────────────────────────────────────────
        // Power budget: updraft (stacking) + tailwind (mobility) + cyclone (finisher) + gale jump.
        // Cyclone normalised from 24 → 28, tailwind kept at 20 (top of mobility range).

        private static void CreateGaleFamily()
        {
            string folder = $"{ROOT}/Gale";
            EnsureFolder(folder);

            // Core — Updraft: stacking lift on strike.
            // Target: DoT range 4–6. Normalised from 6 → 5 for parity.
            Save(folder, "UpdraftRitual", Make(
                ritualName:  "Updraft",
                description: "Strikes create updrafts that lift enemies, stacking for more lift.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Gale_Updraft",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.30f, power: 1.0f)
            ));

            // General — Tailwind: dash grants speed buff.
            // Target: mobility 15–20. Kept at 20 (speed buff, not damage).
            Save(folder, "TailwindRitual", Make(
                ritualName:  "Tailwind",
                description: "Dashing grants a temporary speed boost.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Gale_Tailwind",
                l1: (baseValue: 20f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 40f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Cyclone: finisher AoE.
            // Normalised from 24 → 28 to hit high-impact floor.
            Save(folder, "CycloneRitual", Make(
                ritualName:  "Cyclone",
                description: "Finisher attacks spawn a cyclone that pulls and damages nearby enemies.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Gale_Cyclone",
                l1: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 42f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 56f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Gale Jump: jump slam (stacking).
            // Target: shield/reactive 6–10. Kept at 8 — jump timing adds skill expression.
            Save(folder, "GaleJumpRitual", Make(
                ritualName:  "Gale Jump",
                description: "Jumping creates a wind slam on landing that damages enemies below.",
                family:      RitualFamily.Gale,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnJump,
                effectId:    "Gale_GaleJump",
                l1: (baseValue: 8f,  maxStacks: 3, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 12f, maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 16f, maxStacks: 5, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Time Family (T048) ──────────────────────────────────────────────
        // Power budget: echo (stacking) + slow field (zone/skill) + time burst (finisher) + temporal dodge.
        // Slow Field and Time Burst both normalised upward to hit their respective floors.

        private static void CreateTimeFamily()
        {
            string folder = $"{ROOT}/Time";
            EnsureFolder(folder);

            // Core — Echo Strike: stacking repeat on strike.
            // Target: DoT range 4–6. Kept at 5 — echoes are additive, not true DoT.
            Save(folder, "EchoStrikeRitual", Make(
                ritualName:  "Echo Strike",
                description: "Strikes echo as ghost copies, repeating damage. Stacks increase copies.",
                family:      RitualFamily.Time,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Time_EchoStrike",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.18f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.23f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.28f, power: 1.0f)
            ));

            // General — Slow Field: skill spawns slow zone.
            // Target: mobility 15–20. Normalised from 20 → 18 (control effect, not pure damage).
            Save(folder, "SlowFieldRitual", Make(
                ritualName:  "Slow Field",
                description: "Using a skill creates a temporal zone that slows enemies.",
                family:      RitualFamily.Time,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnSkill,
                effectId:    "Time_SlowField",
                l1: (baseValue: 18f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 27f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 36f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Time Burst: finisher AoE freeze.
            // Normalised from 22 → 28 to hit high-impact floor.
            Save(folder, "TimeBurstRitual", Make(
                ritualName:  "Time Burst",
                description: "Finisher attacks release a time burst that briefly freezes nearby enemies.",
                family:      RitualFamily.Time,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Time_TimeBurst",
                l1: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 42f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 56f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Temporal Dodge: rewind on dodge (stacking).
            // Target: shield/reactive 6–10. Kept at 5 — utility ritual, not a damage source.
            Save(folder, "TemporalDodgeRitual", Make(
                ritualName:  "Temporal Dodge",
                description: "Dodging stores a temporal anchor; rewind to it after a delay.",
                family:      RitualFamily.Time,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDodge,
                effectId:    "Time_TemporalDodge",
                l1: (baseValue: 5f,   maxStacks: 2, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 4, stackMult: 1.25f, power: 1.0f)
            ));
        }

        // ── Cosmic Family (T048) ────────────────────────────────────────────
        // Power budget: star burst (stacking) + cosmic dash (mobility) + supernova (finisher) + void shield.
        // Cosmic Dash normalised from 16 → 18 (same mobility floor as others).

        private static void CreateCosmicFamily()
        {
            string folder = $"{ROOT}/Cosmic";
            EnsureFolder(folder);

            // Core — Star Burst: stacking AoE on strike.
            // Target: DoT range 4–6. Normalised from 7 → 5 for parity.
            Save(folder, "StarBurstRitual", Make(
                ritualName:  "Star Burst",
                description: "Strikes release star fragments in an AoE. Stacks increase radius.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Cosmic_StarBurst",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.30f, power: 1.0f)
            ));

            // General — Cosmic Dash: dash leaves cosmic trail.
            // Target: mobility 15–20. Normalised from 16 → 18.
            Save(folder, "CosmicDashRitual", Make(
                ritualName:  "Cosmic Dash",
                description: "Dashing leaves a trail of cosmic energy that damages enemies.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnDash,
                effectId:    "Cosmic_CosmicDash",
                l1: (baseValue: 18f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 27f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 36f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Supernova: finisher massive burst.
            // Already at 28 (high-impact floor). Explicit scaling added.
            Save(folder, "SupernovaRitual", Make(
                ritualName:  "Supernova",
                description: "Finisher attacks trigger a supernova dealing massive cosmic damage.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Cosmic_Supernova",
                l1: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 45f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 60f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Void Shield: counter on take damage (stacking).
            // Target: shield/reactive 6–10. Kept at 6.
            Save(folder, "VoidShieldRitual", Make(
                ritualName:  "Void Shield",
                description: "Taking damage builds void energy that counter-damages the attacker.",
                family:      RitualFamily.Cosmic,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnTakeDamage,
                effectId:    "Cosmic_VoidShield",
                l1: (baseValue: 6f,  maxStacks: 4, stackMult: 1.10f, power: 1.0f),
                l2: (baseValue: 9f,  maxStacks: 5, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 12f, maxStacks: 6, stackMult: 1.20f, power: 1.0f)
            ));
        }

        // ── Necro Family (T048) ─────────────────────────────────────────────
        // Power budget: life drain (DoT-style) + soul harvest (kill buff) + death blow (finisher) + bone shield.
        // Soul Harvest normalised from 22 → 28, Death Blow from 26 → 30.

        private static void CreateNecroFamily()
        {
            string folder = $"{ROOT}/Necro";
            EnsureFolder(folder);

            // Core — Life Drain: stacking heal on strike.
            // Target: DoT range 4–6 at L1.
            Save(folder, "LifeDrainRitual", Make(
                ritualName:  "Life Drain",
                description: "Strikes drain life from enemies, healing the player. Stacks increase healing.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.Core,
                trigger:     RitualTrigger.OnStrike,
                effectId:    "Necro_LifeDrain",
                l1: (baseValue: 5f,   maxStacks: 3, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 7.5f, maxStacks: 4, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 10f,  maxStacks: 5, stackMult: 1.25f, power: 1.0f)
            ));

            // General — Soul Harvest: buff on kill.
            // Normalised from 22 → 28 (kill-triggered, single-hit equivalent, high-impact floor).
            Save(folder, "SoulHarvestRitual", Make(
                ritualName:  "Soul Harvest",
                description: "Killing an enemy harvests their soul, granting a temporary damage buff.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.General,
                trigger:     RitualTrigger.OnKill,
                effectId:    "Necro_SoulHarvest",
                l1: (baseValue: 28f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 42f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 56f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Death Blow: finisher burst.
            // Normalised from 26 → 30 (high-impact floor).
            Save(folder, "DeathBlowRitual", Make(
                ritualName:  "Death Blow",
                description: "Finisher attacks deal a massive burst of necro damage.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnFinisher,
                effectId:    "Necro_DeathBlow",
                l1: (baseValue: 30f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l2: (baseValue: 45f, maxStacks: 1, stackMult: 1.0f, power: 1.0f),
                l3: (baseValue: 60f, maxStacks: 1, stackMult: 1.0f, power: 1.0f)
            ));

            // Enhancement — Bone Shield: minion on deflect (stacking).
            // Target: shield/reactive 6–10 at L1.
            Save(folder, "BoneShieldRitual", Make(
                ritualName:  "Bone Shield",
                description: "Deflecting an attack summons a bone minion that absorbs damage.",
                family:      RitualFamily.Necro,
                category:    RitualCategory.Enhancement,
                trigger:     RitualTrigger.OnDeflect,
                effectId:    "Necro_BoneShield",
                l1: (baseValue: 8f,  maxStacks: 2, stackMult: 1.12f, power: 1.0f),
                l2: (baseValue: 12f, maxStacks: 3, stackMult: 1.17f, power: 1.0f),
                l3: (baseValue: 16f, maxStacks: 4, stackMult: 1.22f, power: 1.0f)
            ));
        }

        // ── Twin Rituals (T047) ───────────────────────────────────────────────

        private static void CreateTwinRituals()
        {
            string folder = $"{ROOT}/Twins";
            EnsureFolder(folder);

            // Thunder Burn — Fire + Lightning, OnStrike
            Save(folder, "ThunderBurnRitual", MakeTwin(
                ritualName:   "Thunder Burn",
                description:  "Chain lightning that also applies burn DoT. Requires Fire and Lightning rituals.",
                family:       RitualFamily.Fire,
                secondFamily: RitualFamily.Lightning,
                trigger:      RitualTrigger.OnStrike,
                effectId:     "Fire_Lightning_ThunderBurn",
                l1: (baseValue: 12f, maxStacks: 3, stackMult: 1.25f, power: 1.0f),
                l2: (baseValue: 18f, maxStacks: 3, stackMult: 1.25f, power: 1.0f),
                l3: (baseValue: 24f, maxStacks: 3, stackMult: 1.25f, power: 1.0f)
            ));

            // Burning Brambles — Fire + Thorn, OnDeflect
            Save(folder, "BurningBramblesRitual", MakeTwin(
                ritualName:   "Burning Brambles",
                description:  "Thorns reflect fire damage back to attackers. Requires Fire and Thorn rituals.",
                family:       RitualFamily.Fire,
                secondFamily: RitualFamily.Thorn,
                trigger:      RitualTrigger.OnDeflect,
                effectId:     "Fire_Thorn_BurningBrambles",
                l1: (baseValue: 18f, maxStacks: 2, stackMult: 1.30f, power: 1.0f),
                l2: (baseValue: 27f, maxStacks: 2, stackMult: 1.30f, power: 1.0f),
                l3: (baseValue: 36f, maxStacks: 2, stackMult: 1.30f, power: 1.0f)
            ));

            // Monsoon — Water + Gale, OnDash
            Save(folder, "MonsoonRitual", MakeTwin(
                ritualName:   "Monsoon",
                description:  "Dashing leaves a slowing rain zone behind. Requires Water and Gale rituals.",
                family:       RitualFamily.Water,
                secondFamily: RitualFamily.Gale,
                trigger:      RitualTrigger.OnDash,
                effectId:     "Water_Gale_Monsoon",
                l1: (baseValue: 15f, maxStacks: 2, stackMult: 1.20f, power: 1.0f),
                l2: (baseValue: 22.5f, maxStacks: 2, stackMult: 1.20f, power: 1.0f),
                l3: (baseValue: 30f, maxStacks: 2, stackMult: 1.20f, power: 1.0f)
            ));

            // Temporal Shock — Lightning + Time, OnFinisher
            Save(folder, "TemporalShockRitual", MakeTwin(
                ritualName:   "Temporal Shock",
                description:  "Echo the stun effect after a delay. Requires Lightning and Time rituals.",
                family:       RitualFamily.Lightning,
                secondFamily: RitualFamily.Time,
                trigger:      RitualTrigger.OnFinisher,
                effectId:     "Lightning_Time_TemporalShock",
                l1: (baseValue: 25f, maxStacks: 1, stackMult: 1.35f, power: 1.0f),
                l2: (baseValue: 37.5f, maxStacks: 1, stackMult: 1.35f, power: 1.0f),
                l3: (baseValue: 50f, maxStacks: 1, stackMult: 1.35f, power: 1.0f)
            ));

            // Void Harvest — Necro + Cosmic, OnKill
            Save(folder, "VoidHarvestRitual", MakeTwin(
                ritualName:   "Void Harvest",
                description:  "Kill explosions that heal the player. Requires Necro and Cosmic rituals.",
                family:       RitualFamily.Necro,
                secondFamily: RitualFamily.Cosmic,
                trigger:      RitualTrigger.OnKill,
                effectId:     "Necro_Cosmic_VoidHarvest",
                l1: (baseValue: 20f, maxStacks: 2, stackMult: 1.30f, power: 1.0f),
                l2: (baseValue: 30f, maxStacks: 2, stackMult: 1.30f, power: 1.0f),
                l3: (baseValue: 40f, maxStacks: 2, stackMult: 1.30f, power: 1.0f)
            ));

            // Coral Barrier — Thorn + Water, OnTakeDamage
            Save(folder, "CoralBarrierRitual", MakeTwin(
                ritualName:   "Coral Barrier",
                description:  "Reactive thorns and damage reduction on taking damage. Requires Thorn and Water rituals.",
                family:       RitualFamily.Thorn,
                secondFamily: RitualFamily.Water,
                trigger:      RitualTrigger.OnTakeDamage,
                effectId:     "Thorn_Water_CoralBarrier",
                l1: (baseValue: 10f, maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l2: (baseValue: 15f, maxStacks: 4, stackMult: 1.15f, power: 1.0f),
                l3: (baseValue: 20f, maxStacks: 4, stackMult: 1.15f, power: 1.0f)
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

        private static RitualData MakeTwin(
            string ritualName,
            string description,
            RitualFamily family,
            RitualFamily secondFamily,
            RitualTrigger trigger,
            string effectId,
            (float baseValue, int maxStacks, float stackMult, float power) l1,
            (float baseValue, int maxStacks, float stackMult, float power) l2,
            (float baseValue, int maxStacks, float stackMult, float power) l3)
        {
            var data = Make(ritualName, description, family, RitualCategory.Twin,
                           trigger, effectId, l1, l2, l3);
            data.isTwin       = true;
            data.secondFamily = secondFamily;
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
