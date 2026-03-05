using System;
using TomatoFighters.Shared.Data;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Data for a single boss phase. Authored inline on <see cref="BossData"/>
    /// as an array element — one per phase, ordered by descending HP% threshold.
    /// </summary>
    [Serializable]
    public class BossPhaseData
    {
        [Tooltip("Display name for debug/UI (e.g. 'Phase 2 — Enraged').")]
        public string phaseName;

        [Tooltip("HP% at which this phase activates (1.0 = 100%, 0.6 = 60%). " +
                 "Phases are evaluated from last to first — the lowest threshold that " +
                 "the current HP% is at or below wins.")]
        [Range(0f, 1f)]
        public float hpThreshold;

        [Tooltip("Attack pool for this phase. BossAI swaps the EnemyAI attack pool on transition.")]
        public AttackData[] attacks;

        [Tooltip("Multiplier on attack cooldown speed. Higher = faster attacks.")]
        [Range(0.5f, 3f)]
        public float tempoMultiplier = 1f;

        [Tooltip("Override for EnemyData.attackCooldown. -1 = use base value.")]
        public float attackCooldownOverride = -1f;

        [Tooltip("If true, sprite tints red to signal enrage.")]
        public bool enableEnrage;
    }
}
