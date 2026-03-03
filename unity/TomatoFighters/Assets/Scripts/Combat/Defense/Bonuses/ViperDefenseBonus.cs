using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Viper's defense bonus: reflects projectiles on successful defense.
    /// When a projectile is deflected/clashed, it reverses direction toward the attacker.
    /// Projectile system will check this flag for reflection logic.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Combat/DefenseBonus/Viper")]
    public class ViperDefenseBonus : DefenseBonus
    {
        /// <summary>
        /// Whether a projectile reflect is pending. Consumed by the projectile system.
        /// </summary>
        [System.NonSerialized]
        public bool reflectPending;

        /// <summary>Direction to reflect toward (attacker position).</summary>
        [System.NonSerialized]
        public Vector2 reflectDirection;

        /// <inheritdoc/>
        public override void Apply(DefenseContext context, DamageResponse responseType)
        {
            if (context.attacker == null) return;

            reflectPending = true;
            reflectDirection = ((Vector2)context.attacker.transform.position -
                                (Vector2)context.defender.transform.position).normalized;

            Debug.Log($"[ViperDefenseBonus] Projectile reflect pending toward {reflectDirection}.");
        }
    }
}
