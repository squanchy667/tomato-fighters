using System;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Defines how many of a specific enemy to spawn, with what delay, and at which spawn point.
    /// Uses <see cref="GameObject"/> (not EnemyBase) because Shared cannot reference the World assembly.
    /// </summary>
    [Serializable]
    public struct EnemySpawnData
    {
        [Tooltip("Prefab with an EnemyBase-derived component. Null entries are skipped with a warning.")]
        public GameObject enemyPrefab;

        [Tooltip("Number of this enemy type to spawn in the group.")]
        [Range(1, 20)]
        public int spawnCount;

        [Tooltip("Delay in seconds between each individual spawn within this group.")]
        [Range(0f, 5f)]
        public float spawnDelay;

        [Tooltip("Index into WaveManager.spawnPoints. Out-of-range falls back to WaveManager position.")]
        [Range(0, 9)]
        public int spawnPointIndex;
    }
}
