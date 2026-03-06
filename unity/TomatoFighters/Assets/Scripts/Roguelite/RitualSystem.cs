using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Runtime backbone of the roguelite loop. Manages all active rituals for the current run,
    /// dispatches effects when combat events fire, and implements <see cref="IBuffProvider"/>
    /// so the Combat pillar can query damage/speed/defense multipliers each frame.
    ///
    /// <para><b>Integration:</b>
    /// <list type="bullet">
    ///   <item>Drag the combat MonoBehaviour that implements <see cref="ICombatEvents"/> into
    ///         <c>_combatEventsSource</c> in the Inspector.</item>
    ///   <item>Expose this component as <see cref="IBuffProvider"/> to Combat via
    ///         <c>[SerializeField]</c> injection on the damage calculator.</item>
    /// </list></para>
    ///
    /// <para><b>Phase 2 scope:</b> pipeline + VFX + instant bonus elemental damage.
    /// DoT ticks, stack decay, and full stacking formula are deferred to Phase 3 / T029.
    /// Each deferred slot is marked with a TODO comment.</para>
    /// </summary>
    public class RitualSystem : MonoBehaviour, IBuffProvider
    {
        // ── Injection ─────────────────────────────────────────────────────────

        /// <summary>
        /// MonoBehaviour that implements <see cref="ICombatEvents"/>.
        /// Drag the combat system root here in the Inspector.
        /// </summary>
        [SerializeField] private MonoBehaviour _combatEventsSource;

        // ── Runtime state ─────────────────────────────────────────────────────

        private readonly List<ActiveRitualEntry> _activeRituals = new List<ActiveRitualEntry>();
        private readonly Dictionary<RitualFamily, int> _familyCounts = new Dictionary<RitualFamily, int>();

        // Handler dispatch table: RitualData.effectId → handler delegate
        private Dictionary<string, Action<RitualContext>> _handlers;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            InitialiseFamilyCounts();
            RegisterHandlers();

            var src = _combatEventsSource as ICombatEvents;
            if (src == null) return;

            src.OnStrike          += HandleStrike;
            src.OnSkill           += HandleSkill;
            src.OnArcana          += HandleArcana;
            src.OnDash            += HandleDash;
            src.OnDeflect         += HandleDeflect;
            src.OnClash           += HandleClash;
            src.OnPunish          += HandlePunish;          // no RitualTrigger maps to Punish — no-op
            src.OnKill            += HandleKill;
            src.OnFinisher        += HandleFinisher;
            src.OnJump            += HandleJump;
            src.OnDodge           += HandleDodge;
            src.OnTakeDamage      += HandleTakeDamage;
            src.OnPathAbilityUsed += HandlePathAbilityUsed; // no RitualTrigger maps to PathAbility — no-op
        }

        private void OnDestroy()
        {
            var src = _combatEventsSource as ICombatEvents;
            if (src == null) return;

            src.OnStrike          -= HandleStrike;
            src.OnSkill           -= HandleSkill;
            src.OnArcana          -= HandleArcana;
            src.OnDash            -= HandleDash;
            src.OnDeflect         -= HandleDeflect;
            src.OnClash           -= HandleClash;
            src.OnPunish          -= HandlePunish;
            src.OnKill            -= HandleKill;
            src.OnFinisher        -= HandleFinisher;
            src.OnJump            -= HandleJump;
            src.OnDodge           -= HandleDodge;
            src.OnTakeDamage      -= HandleTakeDamage;
            src.OnPathAbilityUsed -= HandlePathAbilityUsed;
        }

        // ── Ritual management — public API ────────────────────────────────────

        /// <summary>
        /// Adds a ritual to the active list at level 1.
        /// If the same ritual is already active, levels it up instead (max level 3).
        /// </summary>
        /// <returns><c>true</c> if the ritual was added or levelled up; <c>false</c> if already at max level.</returns>
        public bool AddRitual(RitualData data)
        {
            if (data == null) return false;

            var existing = FindEntry(data);
            if (existing != null)
                return LevelUpRitual(data);

            _activeRituals.Add(new ActiveRitualEntry(data, level: 1));
            TrackFamilyCount(data, delta: +1);
            return true;
        }

        /// <summary>
        /// Increments the level of an already-active ritual (max level 3).
        /// </summary>
        /// <returns><c>true</c> on success; <c>false</c> if the ritual is not active or is already at level 3.</returns>
        public bool LevelUpRitual(RitualData data)
        {
            if (data == null) return false;

            var entry = FindEntry(data);
            if (entry == null || entry.level >= 3) return false;

            entry.level++;
            return true;
        }

        /// <summary>Clears all active rituals and resets family counts. Call at the start of each run.</summary>
        public void ResetForNewRun()
        {
            _activeRituals.Clear();
            InitialiseFamilyCounts();
        }

        /// <summary>
        /// Returns how many active rituals belong to <paramref name="family"/>.
        /// Twin rituals count toward both their families.
        /// Used by T047 (TwinRitualSystem) to check synergy prerequisites.
        /// </summary>
        public int GetFamilyCount(RitualFamily family)
        {
            return _familyCounts.TryGetValue(family, out var count) ? count : 0;
        }

        // ── IBuffProvider ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Iterates all active rituals and multiplies their per-type damage contributions.
        /// No cache — ritual list changes infrequently; query overhead is negligible (DD-4).
        /// </remarks>
        public float GetDamageMultiplier(DamageType type)
        {
            float mult = 1f;
            foreach (var entry in _activeRituals)
                mult *= entry.GetDamageContribution(type);
            return mult;
        }

        /// <inheritdoc/>
        /// <remarks>No Phase 2 rituals provide a passive speed multiplier. Returns 1.0.</remarks>
        public float GetSpeedMultiplier() => 1f;

        /// <inheritdoc/>
        /// <remarks>No Phase 2 rituals provide a passive defense multiplier. Returns 1.0.</remarks>
        public float GetDefenseMultiplier() => 1f;

        /// <inheritdoc/>
        /// <remarks>Returns <see cref="RitualEffect"/> for every OnStrike ritual currently active.</remarks>
        public List<RitualEffect> GetAdditionalOnHitEffects()
        {
            var results = new List<RitualEffect>();
            foreach (var entry in _activeRituals)
            {
                if (entry.data.trigger == RitualTrigger.OnStrike)
                    results.Add(entry.BuildEffect());
            }
            return results;
        }

        /// <inheritdoc/>
        /// <remarks>Returns <see cref="RitualEffect"/> for every ritual whose trigger matches.</remarks>
        public List<RitualEffect> GetTriggerEffects(RitualTrigger trigger)
        {
            var results = new List<RitualEffect>();
            foreach (var entry in _activeRituals)
            {
                if (entry.data.trigger == trigger)
                    results.Add(entry.BuildEffect());
            }
            return results;
        }

        /// <inheritdoc/>
        /// <remarks>No Phase 2 ritual overrides the repetitive penalty. Returns false.</remarks>
        public bool IsRepetitivePenaltyOverridden() => false;

        /// <inheritdoc/>
        /// <remarks>Path bonuses are PathSystem's responsibility — always returns 1.0.</remarks>
        public float GetPathDamageMultiplier()  => 1f;

        /// <inheritdoc/>
        /// <remarks>Path bonuses are PathSystem's responsibility — always returns 1.0.</remarks>
        public float GetPathDefenseMultiplier() => 1f;

        /// <inheritdoc/>
        /// <remarks>Path bonuses are PathSystem's responsibility — always returns 1.0.</remarks>
        public float GetPathSpeedMultiplier()   => 1f;

        /// <inheritdoc/>
        /// <remarks>Path abilities are PathSystem's responsibility — always returns empty list.</remarks>
        public List<PathAbility> GetActivePathAbilities() => new List<PathAbility>();

        /// <inheritdoc/>
        /// <remarks>No Phase 2 ritual modifies juggle gravity. Returns 1.0.</remarks>
        public float GetJuggleGravityMultiplier() => 1f;

        // ── ICombatEvents handlers ────────────────────────────────────────────

        private void HandleStrike(StrikeEventData e)         => DispatchTrigger(RitualTrigger.OnStrike,     e.hitPosition);
        private void HandleSkill(SkillEventData e)           => DispatchTrigger(RitualTrigger.OnSkill,      e.hitPosition);
        private void HandleArcana(ArcanaEventData e)         => DispatchTrigger(RitualTrigger.OnArcana,     Vector2.zero);
        private void HandleDash(DashEventData e)             => DispatchTrigger(RitualTrigger.OnDash,       Vector2.zero);
        private void HandleDeflect(DeflectEventData e)       => DispatchTrigger(RitualTrigger.OnDeflect,    Vector2.zero);
        private void HandleClash(ClashEventData e)           => DispatchTrigger(RitualTrigger.OnClash,      Vector2.zero);
        private void HandleKill(KillEventData e)             => DispatchTrigger(RitualTrigger.OnKill,       e.killPosition);
        private void HandleFinisher(FinisherEventData e)     => DispatchTrigger(RitualTrigger.OnFinisher,   e.hitPosition);
        private void HandleJump(JumpEventData e)             => DispatchTrigger(RitualTrigger.OnJump,       Vector2.zero);
        private void HandleDodge(DodgeEventData e)           => DispatchTrigger(RitualTrigger.OnDodge,      Vector2.zero);
        private void HandleTakeDamage(TakeDamageEventData e) => DispatchTrigger(RitualTrigger.OnTakeDamage, Vector2.zero);

        // OnPunish has no matching RitualTrigger — subscribed for completeness.
        private void HandlePunish(PunishEventData e) { }

        // OnPathAbilityUsed has no matching RitualTrigger — subscribed for completeness.
        private void HandlePathAbilityUsed(PathAbilityEventData e) { }

        // ── Dispatch ──────────────────────────────────────────────────────────

        /// <summary>
        /// Finds all active rituals matching <paramref name="trigger"/> and invokes their handler.
        /// Unknown effectIds produce a warning and are skipped — never throw.
        /// </summary>
        private void DispatchTrigger(RitualTrigger trigger, Vector2 hitPosition)
        {
            foreach (var entry in _activeRituals)
            {
                if (entry.data.trigger != trigger) continue;

                // Twin activation gate (T047): require at least one regular ritual from each family.
                // The Twin itself contributes +1 to both family counts, so >= 2 means
                // "the Twin plus at least one regular ritual from that family."
                if (entry.data.isTwin)
                {
                    if (GetFamilyCount(entry.data.family) < 2 || GetFamilyCount(entry.data.secondFamily) < 2)
                        continue;
                }

                if (_handlers.TryGetValue(entry.data.effectId, out var handler))
                    handler(new RitualContext(entry, hitPosition));
                else
                    Debug.LogWarning($"[RitualSystem] No handler for effectId '{entry.data.effectId}'. Skipping.");
            }
        }

        // ── Handler registration ──────────────────────────────────────────────

        private void RegisterHandlers()
        {
            _handlers = new Dictionary<string, Action<RitualContext>>();

            // Fire family (T020)
            _handlers["Fire_Burn"]        = FireBurnHandler;
            _handlers["Fire_BlazingDash"] = FireBlazingDashHandler;
            _handlers["Fire_FlameStrike"] = FireFlameStrikeHandler;
            _handlers["Fire_EmberShield"] = FireEmberShieldHandler;

            // Lightning family (T020)
            _handlers["Lightning_Chain"]       = LightningChainHandler;
            _handlers["Lightning_Strike"]      = LightningStrikeHandler;
            _handlers["Lightning_ShockWave"]   = LightningShockWaveHandler;
            _handlers["Lightning_StaticField"] = LightningStaticFieldHandler;

            // Water family (T048)
            _handlers["Water_TidalWave"] = WaterTidalWaveHandler;
            _handlers["Water_WaveDash"]  = WaterWaveDashHandler;
            _handlers["Water_Torrent"]   = WaterTorrentHandler;
            _handlers["Water_Riptide"]   = WaterRiptideHandler;

            // Thorn family (T048)
            _handlers["Thorn_BrambleKnives"] = ThornBrambleKnivesHandler;
            _handlers["Thorn_ThornGuard"]    = ThornThornGuardHandler;
            _handlers["Thorn_VineBurst"]     = ThornVineBurstHandler;
            _handlers["Thorn_BrambleDodge"]  = ThornBrambleDodgeHandler;

            // Gale family (T048)
            _handlers["Gale_Updraft"]  = GaleUpdraftHandler;
            _handlers["Gale_Tailwind"] = GaleTailwindHandler;
            _handlers["Gale_Cyclone"]  = GaleCycloneHandler;
            _handlers["Gale_GaleJump"] = GaleGaleJumpHandler;

            // Time family (T048)
            _handlers["Time_EchoStrike"]    = TimeEchoStrikeHandler;
            _handlers["Time_SlowField"]     = TimeSlowFieldHandler;
            _handlers["Time_TimeBurst"]     = TimeTimeBurstHandler;
            _handlers["Time_TemporalDodge"] = TimeTemporalDodgeHandler;

            // Cosmic family (T048)
            _handlers["Cosmic_StarBurst"]  = CosmicStarBurstHandler;
            _handlers["Cosmic_CosmicDash"] = CosmicCosmicDashHandler;
            _handlers["Cosmic_Supernova"]  = CosmicSupernovaHandler;
            _handlers["Cosmic_VoidShield"] = CosmicVoidShieldHandler;

            // Necro family (T048)
            _handlers["Necro_LifeDrain"]  = NecroLifeDrainHandler;
            _handlers["Necro_SoulHarvest"] = NecroSoulHarvestHandler;
            _handlers["Necro_DeathBlow"]  = NecroDeathBlowHandler;
            _handlers["Necro_BoneShield"] = NecroBoneShieldHandler;

            // Twin rituals (T047)
            _handlers["Fire_Lightning_ThunderBurn"]    = TwinThunderBurnHandler;
            _handlers["Fire_Thorn_BurningBrambles"]    = TwinBurningBramblesHandler;
            _handlers["Water_Gale_Monsoon"]            = TwinMonsoonHandler;
            _handlers["Lightning_Time_TemporalShock"]  = TwinTemporalShockHandler;
            _handlers["Necro_Cosmic_VoidHarvest"]      = TwinVoidHarvestHandler;
            _handlers["Thorn_Water_CoralBarrier"]      = TwinCoralBarrierHandler;
        }

        // ── Fire handlers ─────────────────────────────────────────────────────

        private void FireBurnHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Replace with DoT coroutine when IDamageable target lifetime is stable.
            // Add to RitualEffect: float dotDuration, float dotTickInterval.
            // Handler: StartCoroutine(ElementalTick(ctx.target, effect)) instead of instant hit.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void FireBlazingDashHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Replace with zone/trail spawning when World supports it.
            SpawnVfx(ctx);
        }

        private void FireFlameStrikeHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Replace with DoT coroutine when IDamageable target lifetime is stable.
            SpawnVfx(ctx);
        }

        private void FireEmberShieldHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Replace with ignite-attacker DoT when IDamageable target lifetime is stable.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Lightning handlers ────────────────────────────────────────────────

        private void LightningChainHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Replace with arc-to-secondary-target logic when World supports target queries.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void LightningStrikeHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Replace with bolt-to-target instantiation when World supports it.
            SpawnVfx(ctx);
        }

        private void LightningShockWaveHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Replace with AoE damage application when World supports nearby-enemy queries.
            SpawnVfx(ctx);
        }

        private void LightningStaticFieldHandler(RitualContext ctx)
        {
            // TODO [Phase 3 / T029+]: Discharge built charge as a damage spike on next strike.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Water handlers (T048) ────────────────────────────────────────────

        private void WaterTidalWaveHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Spawn tidal wave projectile that grows with stacks.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void WaterWaveDashHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Spawn water zone along dash path when World supports zones.
            SpawnVfx(ctx);
        }

        private void WaterTorrentHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Burst water damage on finisher when World supports AoE queries.
            SpawnVfx(ctx);
        }

        private void WaterRiptideHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Pull nearby enemies toward dodge position.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Thorn handlers (T048) ────────────────────────────────────────────

        private void ThornBrambleKnivesHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Spawn thorn projectiles on strike, count scales with stacks.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void ThornThornGuardHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Reflect thorn damage to attacker on deflect.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void ThornVineBurstHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: AoE vine eruption on finisher when World supports nearby-enemy queries.
            SpawnVfx(ctx);
        }

        private void ThornBrambleDodgeHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Spawn thorn patch at dodge origin.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Gale handlers (T048) ─────────────────────────────────────────────

        private void GaleUpdraftHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Lift enemies on strike, height scales with stacks.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void GaleTailwindHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Grant temporary speed buff on dash.
            SpawnVfx(ctx);
        }

        private void GaleCycloneHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: AoE cyclone on finisher when World supports AoE queries.
            SpawnVfx(ctx);
        }

        private void GaleGaleJumpHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Ground slam on jump landing, AoE damage.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Time handlers (T048) ─────────────────────────────────────────────

        private void TimeEchoStrikeHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Repeat last strike as a ghost echo, copies scale with stacks.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void TimeSlowFieldHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Spawn slow zone at skill target when World supports zones.
            SpawnVfx(ctx);
        }

        private void TimeTimeBurstHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: AoE time burst on finisher, freezes enemies briefly.
            SpawnVfx(ctx);
        }

        private void TimeTemporalDodgeHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Rewind player to pre-dodge position after a delay.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Cosmic handlers (T048) ───────────────────────────────────────────

        private void CosmicStarBurstHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: AoE star burst on strike, radius scales with stacks.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void CosmicCosmicDashHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Leave cosmic trail on dash that damages enemies.
            SpawnVfx(ctx);
        }

        private void CosmicSupernovaHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Massive burst on finisher when World supports AoE queries.
            SpawnVfx(ctx);
        }

        private void CosmicVoidShieldHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Counter-damage attacker on taking damage.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Necro handlers (T048) ────────────────────────────────────────────

        private void NecroLifeDrainHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Heal player for a fraction of strike damage, scales with stacks.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void NecroSoulHarvestHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Temporary damage buff on kill.
            SpawnVfx(ctx);
        }

        private void NecroDeathBlowHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Massive necro burst on finisher.
            SpawnVfx(ctx);
        }

        private void NecroBoneShieldHandler(RitualContext ctx)
        {
            // TODO [Phase 4+]: Spawn bone minion on deflect that absorbs damage.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Twin handlers (T047) ───────────────────────────────────────────────

        private void TwinThunderBurnHandler(RitualContext ctx)
        {
            // TODO [Phase 5+]: Chain lightning with burn DoT — combines Fire burn + Lightning chain.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void TwinBurningBramblesHandler(RitualContext ctx)
        {
            // TODO [Phase 5+]: Thorns reflect fire damage back to attacker on deflect.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void TwinMonsoonHandler(RitualContext ctx)
        {
            // TODO [Phase 5+]: Leave a slowing rain zone along dash path.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void TwinTemporalShockHandler(RitualContext ctx)
        {
            // TODO [Phase 5+]: Echo the stun effect after a delay on finisher.
            SpawnVfx(ctx);
        }

        private void TwinVoidHarvestHandler(RitualContext ctx)
        {
            // TODO [Phase 5+]: Kill explosions that heal the player.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        private void TwinCoralBarrierHandler(RitualContext ctx)
        {
            // TODO [Phase 5+]: Reactive thorns + damage reduction on taking damage.
            SpawnVfx(ctx);
            ctx.entry.AddStack();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void SpawnVfx(RitualContext ctx)
        {
            if (ctx.entry.data.effectPrefab != null)
                Instantiate(ctx.entry.data.effectPrefab, ctx.hitPosition, Quaternion.identity);
        }

        private ActiveRitualEntry FindEntry(RitualData data)
        {
            foreach (var entry in _activeRituals)
                if (entry.data == data) return entry;
            return null;
        }

        private void InitialiseFamilyCounts()
        {
            _familyCounts.Clear();
            foreach (RitualFamily family in Enum.GetValues(typeof(RitualFamily)))
                _familyCounts[family] = 0;
        }

        private void TrackFamilyCount(RitualData data, int delta)
        {
            _familyCounts[data.family] += delta;
            if (data.isTwin)
                _familyCounts[data.secondFamily] += delta;
        }

        // ── Nested types ──────────────────────────────────────────────────────

        /// <summary>
        /// Runtime record for a single active ritual — wraps the <see cref="RitualData"/> SO
        /// with mutable run state (level, stacks, decay timer).
        /// Only <see cref="RitualSystem"/> creates or reads this type directly.
        /// </summary>
        private class ActiveRitualEntry
        {
            public readonly RitualData data;
            public int level;           // 1–3
            public int currentStacks;   // active stacks of this ritual's effect
            public float lastStackTime; // timestamp of last stack addition — for decay (Phase 3, T029)

            public ActiveRitualEntry(RitualData data, int level)
            {
                this.data  = data;
                this.level = level;
            }

            /// <summary>
            /// Increments the stack count up to the per-level maximum.
            /// Timestamp is stored for future decay logic (T029).
            /// </summary>
            public void AddStack()
            {
                var ld = data.GetLevelData(level);
                if (currentStacks < ld.maxStacks)
                    currentStacks++;
                lastStackTime = Time.time;
            }

            /// <summary>
            /// Damage multiplier contribution of this ritual for <paramref name="type"/>.
            /// Returns 1.0 if this ritual does not produce damage of <paramref name="type"/>.
            /// Uses <see cref="RitualStackCalculator.Compute"/> for the full
            /// level × stacking × ritualPower formula.
            /// </summary>
            public float GetDamageContribution(DamageType type)
            {
                if (FamilyToDamageType(data.family) != type) return 1f;

                var ld = data.GetLevelData(level);
                float effect = RitualStackCalculator.Compute(
                    ld.baseValue, level, currentStacks,
                    ld.stackingMultiplier, ld.ritualPower);
                return 1f + (effect / 100f);
            }

            /// <summary>
            /// Builds the <see cref="RitualEffect"/> Combat applies when this ritual fires.
            /// </summary>
            public RitualEffect BuildEffect()
            {
                var ld = data.GetLevelData(level);
                return new RitualEffect
                {
                    damageType      = FamilyToDamageType(data.family),
                    damageMultiplier = ld.baseValue / 100f,
                    vfxPrefab       = data.effectPrefab,
                    speedMultiplier = 1f
                };
            }

            private static DamageType FamilyToDamageType(RitualFamily family)
            {
                return family switch
                {
                    RitualFamily.Fire      => DamageType.Fire,
                    RitualFamily.Lightning => DamageType.Lightning,
                    RitualFamily.Water     => DamageType.Water,
                    RitualFamily.Thorn     => DamageType.Thorn,
                    RitualFamily.Gale      => DamageType.Gale,
                    RitualFamily.Time      => DamageType.Time,
                    RitualFamily.Cosmic    => DamageType.Cosmic,
                    RitualFamily.Necro     => DamageType.Necro,
                    _                      => DamageType.Physical
                };
            }
        }

        /// <summary>Context passed to each ritual handler at dispatch time.</summary>
        private readonly struct RitualContext
        {
            public readonly ActiveRitualEntry entry;
            public readonly Vector2 hitPosition;

            public RitualContext(ActiveRitualEntry entry, Vector2 hitPosition)
            {
                this.entry       = entry;
                this.hitPosition = hitPosition;
            }
        }
    }
}
