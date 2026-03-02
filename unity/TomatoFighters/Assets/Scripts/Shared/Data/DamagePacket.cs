using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Immutable payload passed to <see cref="Interfaces.IDamageable.TakeDamage"/>.
    /// Contains everything the target needs to process one hit.
    /// </summary>
    public readonly struct DamagePacket
    {
        /// <summary>Element type of the damage (Physical, Fire, etc.).</summary>
        public readonly DamageType type;

        /// <summary>Final calculated damage amount after all multipliers.</summary>
        public readonly float amount;

        /// <summary>Whether this hit came from a punish window (2x stun fill).</summary>
        public readonly bool isPunishDamage;

        /// <summary>Horizontal knockback force applied to Rigidbody2D.</summary>
        public readonly Vector2 knockbackForce;

        /// <summary>Vertical launch force applied to Rigidbody2D.</summary>
        public readonly Vector2 launchForce;

        /// <summary>Which character dealt this damage (for passive tracking).</summary>
        public readonly CharacterType source;

        public DamagePacket(
            DamageType type,
            float amount,
            bool isPunishDamage,
            Vector2 knockbackForce,
            Vector2 launchForce,
            CharacterType source)
        {
            this.type = type;
            this.amount = amount;
            this.isPunishDamage = isPunishDamage;
            this.knockbackForce = knockbackForce;
            this.launchForce = launchForce;
            this.source = source;
        }
    }
}
