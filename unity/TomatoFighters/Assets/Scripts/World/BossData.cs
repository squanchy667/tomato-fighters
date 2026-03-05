using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// ScriptableObject holding all phase data for a boss.
    /// Sits alongside <see cref="EnemyData"/> on the same prefab —
    /// EnemyData handles base stats, BossData handles phase transitions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBossData", menuName = "TomatoFighters/BossData", order = 11)]
    public class BossData : ScriptableObject
    {
        [Header("Phases")]
        [Tooltip("Ordered from first phase (highest HP%) to last phase (lowest HP%). " +
                 "Phase 0 is the starting phase.")]
        public BossPhaseData[] phases;

        [Header("Phase Transition")]
        [Tooltip("Duration of the invulnerable phase transition cinematic (seconds).")]
        [Range(0.5f, 3f)]
        public float phaseTransitionDuration = 1.5f;

        [Tooltip("Number of sprite blinks during phase transition.")]
        [Range(1, 10)]
        public int transitionBlinkCount = 5;

        [Header("Enrage Visual")]
        [Tooltip("Tint color applied when a phase has enableEnrage = true.")]
        public Color enrageColor = new Color(1f, 0.15f, 0.15f);
    }
}
