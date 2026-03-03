using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Payload passed from <see cref="HitboxDamage"/> through <see cref="HitboxManager"/>
    /// when a hitbox trigger detects a valid target. Contains everything downstream
    /// systems need to process the hit.
    /// </summary>
    public readonly struct HitDetectionData
    {
        /// <summary>The damageable target that was hit.</summary>
        public readonly IDamageable target;

        /// <summary>The attack data for the current combo step.</summary>
        public readonly AttackData attackData;

        /// <summary>World-space point where the collision occurred.</summary>
        public readonly Vector2 hitPoint;

        /// <summary>Which character archetype delivered the hit.</summary>
        public readonly CharacterType attacker;

        public HitDetectionData(
            IDamageable target,
            AttackData attackData,
            Vector2 hitPoint,
            CharacterType attacker)
        {
            this.target = target;
            this.attackData = attackData;
            this.hitPoint = hitPoint;
            this.attacker = attacker;
        }
    }
}
