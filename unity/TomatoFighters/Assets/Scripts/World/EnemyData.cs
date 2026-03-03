using TomatoFighters.Shared.Data;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// ScriptableObject defining all base stats for an enemy type.
    /// Referenced by <see cref="EnemyBase"/> at runtime — no hardcoded values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "TomatoFighters/EnemyData", order = 10)]
    public class EnemyData : ScriptableObject
    {
        [Header("Vitals")]
        [Tooltip("Maximum health points.")]
        public float maxHealth = 100f;

        [Header("Pressure / Stun")]
        [Tooltip("Pressure meter threshold — enemy is stunned when this is reached.")]
        public float pressureThreshold = 50f;

        [Tooltip("Duration in seconds the enemy stays stunned.")]
        public float stunDuration = 2f;

        [Tooltip("Duration in seconds of invulnerability after stun recovery (blink).")]
        public float invulnerabilityDuration = 1f;

        [Header("Physics")]
        [Tooltip("Multiplier applied to incoming knockback (0 = immovable, 1 = full force).")]
        [Range(0f, 1f)]
        public float knockbackResistance;

        [Tooltip("Base movement speed.")]
        public float movementSpeed = 3f;

        [Header("Attacks")]
        [Tooltip("List of attacks this enemy can perform. Referenced by AI or timer logic.")]
        public AttackData[] attacks;
    }
}
