using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Brutor's defense bonus: prevents knockback/slideback on successful defense.
    /// On any successful defense action, the defender's velocity is zeroed out
    /// so they hold their ground.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Combat/DefenseBonus/Brutor")]
    public class BrutorDefenseBonus : DefenseBonus
    {
        /// <inheritdoc/>
        public override void Apply(DefenseContext context, DamageResponse responseType)
        {
            if (context.defender == null) return;

            var rb = context.defender.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                Debug.Log("[BrutorDefenseBonus] No-slideback applied — velocity zeroed.");
            }
        }
    }
}
