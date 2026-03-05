using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Manages equipped trinkets during a run. Tracks timed buff activation/expiry
    /// for conditional trinkets and exposes multipliers for <see cref="CharacterBaseStats"/>
    /// integration via <c>StatModifierInput.trinketMultipliers</c>.
    ///
    /// <para>Subscribes to <see cref="ICombatEvents"/> for dodge/kill/deflect/finisher triggers.</para>
    /// </summary>
    public class TrinketSystem : MonoBehaviour
    {
        private const int MAX_TRINKET_SLOTS = 5;

        // ── Injection ───────────────────────────────────────────────────────

        /// <summary>
        /// MonoBehaviour that implements <see cref="ICombatEvents"/>.
        /// Drag the combat system root here in the Inspector.
        /// </summary>
        [SerializeField] private MonoBehaviour _combatEventsSource;

        /// <summary>Character base stats for flat-to-multiplier conversion.</summary>
        [SerializeField] private CharacterBaseStats _baseStats;

        // ── Runtime state ───────────────────────────────────────────────────

        private readonly List<ActiveTrinketEntry> _activeTrinkets = new List<ActiveTrinketEntry>();

        // ── Unity lifecycle ─────────────────────────────────────────────────

        private void Awake()
        {
            var src = _combatEventsSource as ICombatEvents;
            if (src == null) return;

            src.OnDodge    += HandleDodge;
            src.OnKill     += HandleKill;
            src.OnDeflect  += HandleDeflect;
            src.OnFinisher += HandleFinisher;
        }

        private void OnDestroy()
        {
            var src = _combatEventsSource as ICombatEvents;
            if (src == null) return;

            src.OnDodge    -= HandleDodge;
            src.OnKill     -= HandleKill;
            src.OnDeflect  -= HandleDeflect;
            src.OnFinisher -= HandleFinisher;
        }

        private void Update()
        {
            TickBuffTimers(Time.deltaTime);
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to equip a trinket. Returns false if all slots are full.
        /// Caller should prompt swap UI when this returns false.
        /// </summary>
        public bool AddTrinket(TrinketData data)
        {
            if (data == null) return false;
            if (_activeTrinkets.Count >= MAX_TRINKET_SLOTS) return false;

            _activeTrinkets.Add(new ActiveTrinketEntry(data));
            return true;
        }

        /// <summary>
        /// Removes a trinket by index. Returns false if index is out of range.
        /// </summary>
        public bool RemoveTrinket(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _activeTrinkets.Count) return false;

            _activeTrinkets.RemoveAt(slotIndex);
            return true;
        }

        /// <summary>
        /// Swaps an existing trinket at <paramref name="slotIndex"/> with <paramref name="newData"/>.
        /// Returns false if the slot index is invalid.
        /// </summary>
        public bool SwapTrinket(int slotIndex, TrinketData newData)
        {
            if (newData == null) return false;
            if (slotIndex < 0 || slotIndex >= _activeTrinkets.Count) return false;

            _activeTrinkets[slotIndex] = new ActiveTrinketEntry(newData);
            return true;
        }

        /// <summary>Number of currently equipped trinkets.</summary>
        public int EquippedCount => _activeTrinkets.Count;

        /// <summary>Maximum number of trinket slots available.</summary>
        public int MaxSlots => MAX_TRINKET_SLOTS;

        /// <summary>
        /// Returns the current trinket multiplier array, ready to assign
        /// to <c>StatModifierInput.trinketMultipliers</c>.
        /// </summary>
        public float[] GetMultipliers()
        {
            return TrinketStackCalculator.CalculateMultipliers(_activeTrinkets, _baseStats);
        }

        /// <summary>Clears all equipped trinkets. Call at the start of each run.</summary>
        public void ResetForNewRun()
        {
            _activeTrinkets.Clear();
        }

        /// <summary>Read-only access to active trinkets for UI display.</summary>
        public IReadOnlyList<ActiveTrinketEntry> ActiveTrinkets => _activeTrinkets;

        // ── ICombatEvents handlers ──────────────────────────────────────────

        private void HandleDodge(DodgeEventData e) => ActivateConditional(TrinketTriggerType.OnDodge);
        private void HandleKill(KillEventData e) => ActivateConditional(TrinketTriggerType.OnKill);
        private void HandleDeflect(DeflectEventData e) => ActivateConditional(TrinketTriggerType.OnDeflect);
        private void HandleFinisher(FinisherEventData e) => ActivateConditional(TrinketTriggerType.OnFinisher);

        // ── Private helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Activates all equipped trinkets matching the given trigger type
        /// and resets their buff timer.
        /// </summary>
        private void ActivateConditional(TrinketTriggerType trigger)
        {
            foreach (var entry in _activeTrinkets)
            {
                if (entry.Data.triggerType == trigger)
                {
                    entry.IsActive = true;
                    entry.RemainingTime = entry.Data.buffDuration;
                }
            }
        }

        /// <summary>
        /// Ticks down buff timers for conditional trinkets and deactivates expired ones.
        /// </summary>
        private void TickBuffTimers(float deltaTime)
        {
            foreach (var entry in _activeTrinkets)
            {
                if (entry.Data.triggerType == TrinketTriggerType.Always) continue;
                if (!entry.IsActive) continue;

                entry.RemainingTime -= deltaTime;
                if (entry.RemainingTime <= 0f)
                {
                    entry.IsActive = false;
                    entry.RemainingTime = 0f;
                }
            }
        }
    }
}
