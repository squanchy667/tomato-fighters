using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Core runtime system for character-specific path-locked inspirations.
    /// Manages collection, drop candidate generation, permanent unlocks, and
    /// provides stat/ability queries via <see cref="IInspirationProvider"/>.
    ///
    /// <para>24 total inspirations: 4 characters x 3 paths x 2 per path.
    /// Dropped as a choice of 2-3 options from mini-boss kills.
    /// Some permanently unlockable via Primordial Seeds.</para>
    /// </summary>
    public class InspirationSystem : MonoBehaviour, IInspirationProvider
    {
        // ── Dependencies (SerializeField) ────────────────────────────────────

        [SerializeField] private MonoBehaviour _pathProviderSource;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private List<InspirationData> _allInspirations;

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Fired when a mini-boss defeat presents inspiration choices to the player.</summary>
        public event Action<InspirationDropEventData> OnInspirationDropReady;

        /// <summary>Fired after the player picks an inspiration from the drop choices.</summary>
        public event Action<InspirationData> OnInspirationCollected;

        // ── Runtime state ────────────────────────────────────────────────────

        private readonly List<InspirationData> _collectedInspirations = new List<InspirationData>();
        private readonly HashSet<string> _permanentlyUnlockedIds = new HashSet<string>();

        private IPathProvider _pathProvider;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_pathProviderSource != null)
                _pathProvider = _pathProviderSource as IPathProvider;
        }

        // ── IInspirationProvider ─────────────────────────────────────────────

        /// <summary>Returns additive stat bonus from all active flat-type inspirations for a given stat.</summary>
        public float GetInspirationStatBonus(StatType statType)
        {
            float total = 0f;
            for (int i = 0; i < _collectedInspirations.Count; i++)
            {
                var insp = _collectedInspirations[i];
                if (insp.effectType == InspirationEffectType.StatModifier
                    && insp.statType == statType
                    && insp.modifierType == ModifierType.Flat)
                {
                    total += insp.value;
                }
            }
            return total;
        }

        /// <summary>Returns multiplier from percent-type inspirations for a given stat (1.0 = no change).</summary>
        public float GetInspirationStatMultiplier(StatType statType)
        {
            float multiplier = 1.0f;
            for (int i = 0; i < _collectedInspirations.Count; i++)
            {
                var insp = _collectedInspirations[i];
                if (insp.effectType == InspirationEffectType.StatModifier
                    && insp.statType == statType
                    && insp.modifierType == ModifierType.Percent)
                {
                    multiplier *= (1f + insp.value);
                }
            }
            return multiplier;
        }

        /// <summary>Checks if an ability modifier inspiration is currently active.</summary>
        public bool HasAbilityModifier(string abilityModifierId)
        {
            if (string.IsNullOrEmpty(abilityModifierId))
                return false;

            for (int i = 0; i < _collectedInspirations.Count; i++)
            {
                var insp = _collectedInspirations[i];
                if (insp.effectType == InspirationEffectType.AbilityModifier
                    && insp.abilityModifierId == abilityModifierId)
                {
                    return true;
                }
            }
            return false;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Adds an inspiration to the collected list. Rejects duplicates and
        /// inspirations that don't match the current character.
        /// </summary>
        /// <returns><c>true</c> if successfully collected.</returns>
        public bool CollectInspiration(InspirationData inspiration)
        {
            if (inspiration == null)
                return false;

            // Reject duplicates
            for (int i = 0; i < _collectedInspirations.Count; i++)
            {
                if (_collectedInspirations[i].inspirationId == inspiration.inspirationId)
                {
                    Debug.LogWarning($"[InspirationSystem] Already collected: {inspiration.inspirationId}");
                    return false;
                }
            }

            // Reject wrong character
            if (_pathProvider != null && inspiration.character != _pathProvider.Character)
            {
                Debug.LogWarning($"[InspirationSystem] Wrong character: {inspiration.character} != {_pathProvider.Character}");
                return false;
            }

            _collectedInspirations.Add(inspiration);
            OnInspirationCollected?.Invoke(inspiration);
            return true;
        }

        /// <summary>
        /// Attempts to permanently unlock an inspiration by spending Primordial Seeds.
        /// </summary>
        /// <returns><c>true</c> if the unlock succeeded.</returns>
        public bool TryPermanentUnlock(string inspirationId)
        {
            if (string.IsNullOrEmpty(inspirationId))
                return false;

            if (_permanentlyUnlockedIds.Contains(inspirationId))
                return false;

            var data = FindInspirationById(inspirationId);
            if (data == null || data.permanentUnlockCost <= 0)
                return false;

            if (_currencyManager == null || !_currencyManager.TryRemove(CurrencyType.PrimordialSeeds, data.permanentUnlockCost))
                return false;

            _permanentlyUnlockedIds.Add(inspirationId);
            return true;
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> random inspirations from the player's
        /// active paths, excluding already collected ones. Used by the drop/UI system.
        /// </summary>
        public List<InspirationData> GetDropCandidates(int count)
        {
            var candidates = new List<InspirationData>();

            for (int i = 0; i < _allInspirations.Count; i++)
            {
                var insp = _allInspirations[i];

                // Must match current character
                if (_pathProvider != null && insp.character != _pathProvider.Character)
                    continue;

                // Must match active main or secondary path
                if (_pathProvider != null && !_pathProvider.HasPath(insp.path))
                    continue;

                // Exclude already collected
                if (IsCollected(insp.inspirationId))
                    continue;

                candidates.Add(insp);
            }

            // Shuffle and take up to count
            Shuffle(candidates);
            if (candidates.Count > count)
                candidates.RemoveRange(count, candidates.Count - count);

            return candidates;
        }

        /// <summary>
        /// Fires the drop-ready event with candidate inspirations.
        /// Called when a mini-boss is defeated (or by external trigger).
        /// </summary>
        public void TriggerInspirationDrop(int candidateCount = 3)
        {
            var candidates = GetDropCandidates(candidateCount);
            if (candidates.Count == 0)
                return;

            OnInspirationDropReady?.Invoke(new InspirationDropEventData
            {
                candidates = candidates
            });
        }

        /// <summary>
        /// Resets run-specific state. Clears collected inspirations and
        /// re-grants permanently unlocked ones.
        /// </summary>
        public void ResetForNewRun()
        {
            _collectedInspirations.Clear();

            // Re-grant permanently unlocked inspirations
            foreach (var id in _permanentlyUnlockedIds)
            {
                var data = FindInspirationById(id);
                if (data != null)
                    _collectedInspirations.Add(data);
            }
        }

        /// <summary>Creates serializable save data for permanent unlocks.</summary>
        public List<string> CreateSaveData()
        {
            return new List<string>(_permanentlyUnlockedIds);
        }

        /// <summary>Loads permanent unlock IDs from save data.</summary>
        public void LoadSaveData(List<string> permanentIds)
        {
            _permanentlyUnlockedIds.Clear();
            if (permanentIds != null)
            {
                for (int i = 0; i < permanentIds.Count; i++)
                    _permanentlyUnlockedIds.Add(permanentIds[i]);
            }
        }

        /// <summary>Returns the list of currently collected inspirations (read-only).</summary>
        public IReadOnlyList<InspirationData> CollectedInspirations => _collectedInspirations;

        // ── Internal helpers ─────────────────────────────────────────────────

        private bool IsCollected(string inspirationId)
        {
            for (int i = 0; i < _collectedInspirations.Count; i++)
            {
                if (_collectedInspirations[i].inspirationId == inspirationId)
                    return true;
            }
            return false;
        }

        private InspirationData FindInspirationById(string inspirationId)
        {
            if (_allInspirations == null)
                return null;

            for (int i = 0; i < _allInspirations.Count; i++)
            {
                if (_allInspirations[i].inspirationId == inspirationId)
                    return _allInspirations[i];
            }
            return null;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        // ── Test support ─────────────────────────────────────────────────────

        /// <summary>
        /// Initializes the system with injected dependencies (for unit testing without MonoBehaviour).
        /// </summary>
        internal void InitializeForTest(
            IPathProvider pathProvider,
            CurrencyManager currencyManager,
            List<InspirationData> allInspirations)
        {
            _pathProvider = pathProvider;
            _currencyManager = currencyManager;
            _allInspirations = allInspirations;
        }
    }
}
