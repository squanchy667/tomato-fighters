using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat.Projectiles
{
    /// <summary>
    /// Trapper's harpoon projectile. Extends <see cref="ProjectileBase"/>.
    /// On hit, applies Immobilize status for 2 seconds and deals minor damage.
    /// Destroyed on first hit (does not pierce).
    /// </summary>
    public class HarpoonProjectile : ProjectileBase
    {
        private const float IMMOBILIZE_DURATION = 2f;
        private const float DAMAGE = 8f;

        private CharacterType _sourceCharacter;

        /// <summary>Set the source character for damage attribution.</summary>
        public void SetSource(CharacterType source)
        {
            _sourceCharacter = source;
        }

        protected override void OnTargetHit(IDamageable target, Collider2D collider)
        {
            // Apply damage
            if (!target.IsInvulnerable)
            {
                var packet = new DamagePacket(
                    type: DamageType.Physical,
                    amount: DAMAGE,
                    isPunishDamage: false,
                    knockbackForce: Vector2.zero,
                    launchForce: Vector2.zero,
                    source: _sourceCharacter,
                    stunFillAmount: 2f);
                target.TakeDamage(packet);
            }

            // Apply immobilize via IStatusEffectable
            var statusEffectable = collider.GetComponent<IStatusEffectable>();
            if (statusEffectable == null)
                statusEffectable = collider.GetComponentInParent<IStatusEffectable>();

            statusEffectable?.AddEffect(new StatusEffect(
                StatusEffectType.Immobilize, IMMOBILIZE_DURATION, 1f, transform));

            Debug.Log($"[HarpoonProjectile] Hit {collider.name} — immobilized for {IMMOBILIZE_DURATION}s");
            Destroy(gameObject);
        }
    }
}
