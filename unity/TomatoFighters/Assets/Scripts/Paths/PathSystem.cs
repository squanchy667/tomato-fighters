using System;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Paths
{
    /// <summary>
    /// Runtime authority for a player's path state during a run.
    /// Implements <see cref="IPathProvider"/> so all three pillars can query path state.
    ///
    /// <para>Enforces Main + Secondary selection rules, tracks tier progression triggered
    /// by boss/island defeats, and fires events when selection or tier-up occurs.</para>
    ///
    /// <para>Wired up in the Inspector — inject via <c>[SerializeField]</c> into
    /// PathSelectionUI (T019) and PathAbilityExecutor (T028).</para>
    /// </summary>
    public class PathSystem : MonoBehaviour, IPathProvider
    {
        // ── Configuration ──────────────────────────────────────────────────
        [SerializeField] private CharacterType character;

        /// <summary>
        /// Drag the RunManager (or any MonoBehaviour implementing IRunProgressionEvents)
        /// here once Dev 3 ships it. Leave empty until then — null-safe.
        /// </summary>
        [SerializeField] private MonoBehaviour _runProgressionSource;

        // ── Runtime state ──────────────────────────────────────────────────
        private PathData _mainPath;
        private PathData _secondaryPath;
        private int _mainTier;
        private int _secondaryTier;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired when the main path is successfully selected.</summary>
        public event Action<PathSelectedData> OnMainPathSelected;

        /// <summary>Fired when the secondary path is successfully selected.</summary>
        public event Action<PathSelectedData> OnSecondaryPathSelected;

        /// <summary>Fired when either active path advances to a new tier.</summary>
        public event Action<PathTierUpData> OnPathTierUp;

        // ── IPathProvider — properties ──────────────────────────────────────
        /// <inheritdoc/>
        public CharacterType Character => character;

        /// <inheritdoc/>
        public PathData MainPath => _mainPath;

        /// <inheritdoc/>
        public PathData SecondaryPath => _secondaryPath;

        /// <inheritdoc/>
        public int MainPathTier => _mainTier;

        /// <inheritdoc/>
        public int SecondaryPathTier => _secondaryTier;

        // ── Unity lifecycle ────────────────────────────────────────────────
        private void Awake()
        {
            var src = _runProgressionSource as IRunProgressionEvents;
            if (src != null)
            {
                src.OnBossDefeated    += HandleBossDefeated;
                src.OnIslandCompleted += HandleIslandCompleted;
            }
        }

        private void OnDestroy()
        {
            var src = _runProgressionSource as IRunProgressionEvents;
            if (src != null)
            {
                src.OnBossDefeated    -= HandleBossDefeated;
                src.OnIslandCompleted -= HandleIslandCompleted;
            }
        }

        // ── Selection ──────────────────────────────────────────────────────
        /// <summary>
        /// Select the Main path. Grants tier 1 immediately and fires <see cref="OnMainPathSelected"/>.
        /// </summary>
        /// <returns><c>true</c> if accepted; <c>false</c> if the selection was invalid.</returns>
        public bool SelectMainPath(PathData data)
        {
            if (data == null)               return false;
            if (data.character != character) return false;
            if (_mainPath != null)          return false; // already selected

            _mainPath  = data;
            _mainTier  = 1;

            OnMainPathSelected?.Invoke(new PathSelectedData(character, data.pathType, isMainPath: true));
            return true;
        }

        /// <summary>
        /// Select the Secondary path. Grants tier 1 immediately and fires <see cref="OnSecondaryPathSelected"/>.
        /// Requires a main path to already be selected.
        /// </summary>
        /// <returns><c>true</c> if accepted; <c>false</c> if the selection was invalid.</returns>
        public bool SelectSecondaryPath(PathData data)
        {
            if (data == null)               return false;
            if (data.character != character) return false;
            if (_mainPath == null)          return false; // must choose main first
            if (_secondaryPath != null)     return false; // already selected
            if (data == _mainPath)          return false; // same path as main

            _secondaryPath  = data;
            _secondaryTier  = 1;

            OnSecondaryPathSelected?.Invoke(new PathSelectedData(character, data.pathType, isMainPath: false));
            return true;
        }

        // ── Tier Progression ──────────────────────────────────────────────
        /// <summary>
        /// Advances both active paths from T1 → T2. Call when a boss is defeated.
        /// Safe to call when no paths are selected (no-op).
        /// </summary>
        public void HandleBossDefeated(BossDefeatedData data)
        {
            TryAdvanceTier(ref _mainTier,      maxTier: 2, _mainPath);
            TryAdvanceTier(ref _secondaryTier, maxTier: 2, _secondaryPath);
        }

        /// <summary>
        /// Advances the Main path from T2 → T3. Secondary path is unaffected.
        /// Call when an island is fully completed.
        /// </summary>
        public void HandleIslandCompleted(IslandCompletedData data)
        {
            TryAdvanceTier(ref _mainTier, maxTier: 3, _mainPath);
        }

        // ── Run Lifecycle ──────────────────────────────────────────────────
        /// <summary>Clears all path state. Call at the start of each new run.</summary>
        public void ResetForNewRun()
        {
            _mainPath      = null;
            _secondaryPath = null;
            _mainTier      = 0;
            _secondaryTier = 0;
        }

        // ── IPathProvider — methods ────────────────────────────────────────
        /// <inheritdoc/>
        public bool HasPath(PathType type)
        {
            return (_mainPath      != null && _mainPath.pathType      == type)
                || (_secondaryPath != null && _secondaryPath.pathType == type);
        }

        /// <inheritdoc/>
        public float GetPathStatBonus(StatType stat)
        {
            float total = 0f;
            if (_mainPath != null && _mainTier > 0)
                total += _mainPath.GetStatBonusArray(_mainTier)[(int)stat];
            if (_secondaryPath != null && _secondaryTier > 0)
                total += _secondaryPath.GetStatBonusArray(_secondaryTier)[(int)stat];
            return total;
        }

        /// <inheritdoc/>
        public bool IsPathAbilityUnlocked(string abilityId)
        {
            if (_mainPath != null)
                for (int t = 1; t <= _mainTier; t++)
                    if (_mainPath.GetAbilityIdForTier(t) == abilityId) return true;

            if (_secondaryPath != null)
                for (int t = 1; t <= _secondaryTier; t++)
                    if (_secondaryPath.GetAbilityIdForTier(t) == abilityId) return true;

            return false;
        }

        // ── Private helpers ────────────────────────────────────────────────
        /// <summary>
        /// Advances <paramref name="tier"/> by one if it equals <c>maxTier - 1</c> and
        /// <paramref name="path"/> is active. Fires <see cref="OnPathTierUp"/> on change.
        /// Idempotent — calling twice at the same tier does nothing.
        /// </summary>
        private void TryAdvanceTier(ref int tier, int maxTier, PathData path)
        {
            if (path == null)          return;
            if (tier != maxTier - 1)   return; // only advance from the tier just below max

            tier++;
            OnPathTierUp?.Invoke(new PathTierUpData(character, path.pathType, newTier: tier));
        }
    }
}
