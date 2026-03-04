using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Describes a single combat wave: its enemy groups, display name, and whether it can be skipped.
    /// Multiple <see cref="WaveData"/> entries form an area's full encounter sequence.
    /// </summary>
    [Serializable]
    public class WaveData
    {
        [Tooltip("Display name for UI and debug ('Ambush Wave', 'Mini-Boss').")]
        public string waveName;

        [Tooltip("Enemy groups to spawn in this wave. All groups begin spawning simultaneously.")]
        public List<EnemySpawnData> enemyGroups;

        [Tooltip("Optional waves can be skipped (e.g. bonus challenge rooms). Auto-skipped in Phase 1.")]
        public bool isOptional;
    }
}
