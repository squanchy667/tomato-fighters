using System;
using System.Collections;
using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Events;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Core World pillar orchestrator that drives combat encounter pacing.
    /// Spawns enemies in configurable waves, tracks alive counts via <see cref="EnemyBase.OnDied"/>,
    /// and fires SO event channels that drive the run loop (camera lock, reward selection, area transitions).
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        // ── Wave Configuration ──────────────────────────────────────────

        [Header("Wave Configuration")]
        [Tooltip("Ordered list of waves for this area. Executed sequentially.")]
        [SerializeField] private List<WaveData> waves;

        [Tooltip("World-space positions where enemies can spawn. Referenced by EnemySpawnData.spawnPointIndex.")]
        [SerializeField] private Transform[] spawnPoints;

        // ── Timing ──────────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("Delay before first enemy spawns after wave start event fires.")]
        [Range(0f, 5f)]
        [SerializeField] private float waveStartDelay = 1f;

        [Tooltip("Delay after all enemies die before advancing to next wave or completing area.")]
        [Range(0f, 3f)]
        [SerializeField] private float waveClearDelay = 0.5f;

        // ── SO Event Channels ───────────────────────────────────────────

        [Header("Event Channels — Outgoing")]
        [Tooltip("Raised when a wave begins. Payload: wave index (0-based).")]
        [SerializeField] private IntEventChannel onWaveStart;

        [Tooltip("Raised when all enemies in the current wave are defeated.")]
        [SerializeField] private VoidEventChannel onWaveCleared;

        [Tooltip("Raised when all waves in this area are complete.")]
        [SerializeField] private VoidEventChannel onAreaComplete;

        [Tooltip("Raised to lock the camera at the current position (combat boundary).")]
        [SerializeField] private VoidEventChannel onCameraLock;

        [Tooltip("Raised to unlock the camera when the area is cleared.")]
        [SerializeField] private VoidEventChannel onCameraUnlock;

        [Header("Event Channels — Incoming")]
        [Tooltip("Subscribes to LevelBound trigger. Starts the wave sequence on arrival.")]
        [SerializeField] private VoidEventChannel onBoundReached;

        // ── Runtime State ───────────────────────────────────────────────

        private int _currentWaveIndex;
        private int _aliveEnemyCount;
        private bool _isWaveActive;
        private int _totalEnemiesDefeatedInArea;
        private Coroutine _waveCoroutine;

        // ── Public Properties ───────────────────────────────────────────

        /// <summary>Zero-based index of the wave currently in progress.</summary>
        public int CurrentWaveIndex => _currentWaveIndex;

        /// <summary>Number of enemies still alive in the current wave.</summary>
        public int AliveEnemyCount => _aliveEnemyCount;

        /// <summary>True while a wave is spawning or enemies are still alive.</summary>
        public bool IsWaveActive => _isWaveActive;

        /// <summary>Total number of waves configured for this area.</summary>
        public int TotalWaveCount => waves != null ? waves.Count : 0;

        // ── Unity Lifecycle ─────────────────────────────────────────────

        private void OnEnable()
        {
            if (onBoundReached != null)
                onBoundReached.Register(HandleBoundReached);
        }

        private void OnDisable()
        {
            if (onBoundReached != null)
                onBoundReached.Unregister(HandleBoundReached);
        }

        // ── Bound Handler ───────────────────────────────────────────────

        private void HandleBoundReached()
        {
            if (_isWaveActive) return;

            // Lock camera at combat boundary
            if (onCameraLock != null)
                onCameraLock.Raise();

            _waveCoroutine = StartCoroutine(RunWaveSequence());
        }

        // ── Wave Sequence ───────────────────────────────────────────────

        private IEnumerator RunWaveSequence()
        {
            for (_currentWaveIndex = 0; _currentWaveIndex < waves.Count; _currentWaveIndex++)
            {
                var wave = waves[_currentWaveIndex];

                // Skip optional waves in Phase 1 (UI for choosing deferred)
                if (wave.isOptional)
                {
                    Debug.Log($"[WaveManager] Skipping optional wave '{wave.waveName}' (index {_currentWaveIndex}).");
                    continue;
                }

                yield return RunWave(wave, _currentWaveIndex);
            }

            CompleteArea();
        }

        private IEnumerator RunWave(WaveData wave, int index)
        {
            _isWaveActive = true;
            _aliveEnemyCount = 0;

            Debug.Log($"[WaveManager] Wave {index}: '{wave.waveName}' starting.");

            // Notify listeners (UI, music, etc.)
            if (onWaveStart != null)
                onWaveStart.Raise(index);

            yield return new WaitForSeconds(waveStartDelay);

            // Spawn all enemy groups concurrently
            if (wave.enemyGroups != null)
            {
                foreach (var group in wave.enemyGroups)
                {
                    StartCoroutine(SpawnEnemyGroup(group));
                }
            }

            // Wait until all enemies from this wave are dead
            yield return new WaitUntil(() => _aliveEnemyCount <= 0 && !IsSpawning());

            Debug.Log($"[WaveManager] Wave {index}: '{wave.waveName}' cleared.");

            if (onWaveCleared != null)
                onWaveCleared.Raise();

            yield return new WaitForSeconds(waveClearDelay);

            _isWaveActive = false;
        }

        // ── Spawning ────────────────────────────────────────────────────

        private int _activeSpawnCoroutines;

        private bool IsSpawning() => _activeSpawnCoroutines > 0;

        private IEnumerator SpawnEnemyGroup(EnemySpawnData group)
        {
            _activeSpawnCoroutines++;

            if (group.enemyPrefab == null)
            {
                Debug.LogWarning("[WaveManager] EnemySpawnData has null prefab — skipping group.", this);
                _activeSpawnCoroutines--;
                yield break;
            }

            Vector3 spawnPos = GetSpawnPosition(group.spawnPointIndex);

            for (int i = 0; i < group.spawnCount; i++)
            {
                // Small random offset so enemies don't stack exactly
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    0f,
                    0f
                );

                GameObject spawned = Instantiate(group.enemyPrefab, spawnPos + offset, Quaternion.identity);

                var enemy = spawned.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    _aliveEnemyCount++;
                    enemy.OnDied += HandleEnemyDied;
                }
                else
                {
                    Debug.LogWarning($"[WaveManager] Spawned '{group.enemyPrefab.name}' has no EnemyBase component.", spawned);
                }

                if (i < group.spawnCount - 1 && group.spawnDelay > 0f)
                {
                    yield return new WaitForSeconds(group.spawnDelay);
                }
            }

            _activeSpawnCoroutines--;
        }

        private Vector3 GetSpawnPosition(int spawnPointIndex)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return transform.position;

            if (spawnPointIndex < 0 || spawnPointIndex >= spawnPoints.Length)
            {
                Debug.LogWarning($"[WaveManager] Spawn point index {spawnPointIndex} out of range (0–{spawnPoints.Length - 1}). " +
                                 "Falling back to WaveManager position.", this);
                return transform.position;
            }

            return spawnPoints[spawnPointIndex].position;
        }

        // ── Death Tracking ──────────────────────────────────────────────

        private void HandleEnemyDied()
        {
            _aliveEnemyCount--;
            _totalEnemiesDefeatedInArea++;

            if (_aliveEnemyCount < 0)
            {
                Debug.LogError("[WaveManager] Alive enemy count went negative — double-death detected.", this);
                _aliveEnemyCount = 0;
            }
        }

        // ── Area Completion ─────────────────────────────────────────────

        private void CompleteArea()
        {
            _isWaveActive = false;

            Debug.Log($"[WaveManager] Area complete. {_totalEnemiesDefeatedInArea} enemies defeated.");

            if (onAreaComplete != null)
                onAreaComplete.Raise();

            if (onCameraUnlock != null)
                onCameraUnlock.Raise();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Resets all state for a new area. Call after transitioning to a new combat zone.
        /// Assign new <see cref="waves"/> and <see cref="spawnPoints"/> before calling this
        /// if the area layout changes.
        /// </summary>
        public void AdvanceToNextArea()
        {
            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
                _waveCoroutine = null;
            }

            _currentWaveIndex = 0;
            _aliveEnemyCount = 0;
            _isWaveActive = false;
            _totalEnemiesDefeatedInArea = 0;
            _activeSpawnCoroutines = 0;
        }

        /// <summary>
        /// Attempts to skip the current wave if it is marked as optional.
        /// Returns true if the wave was skipped, false if it is mandatory or no wave is active.
        /// </summary>
        public bool TrySkipCurrentWave()
        {
            if (!_isWaveActive) return false;
            if (_currentWaveIndex < 0 || _currentWaveIndex >= waves.Count) return false;
            if (!waves[_currentWaveIndex].isOptional) return false;

            Debug.Log($"[WaveManager] Skipping optional wave '{waves[_currentWaveIndex].waveName}'.");
            return true;
        }

        // ── Gizmos ──────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (spawnPoints == null) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null) continue;

                Vector3 pos = spawnPoints[i].position;
                Gizmos.DrawWireSphere(pos, 0.3f);
                Gizmos.DrawLine(pos + Vector3.left * 0.3f, pos + Vector3.right * 0.3f);
                Gizmos.DrawLine(pos + Vector3.up * 0.3f, pos + Vector3.down * 0.3f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"Spawn [{i}]");
#endif
            }
        }
    }
}
