using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Describes an active status effect on an entity.
    /// Abilities create these; <see cref="TomatoFighters.Shared.Interfaces.IStatusEffectable"/>
    /// implementations track duration and queries.
    /// </summary>
    public struct StatusEffect
    {
        /// <summary>Which effect this is (Mark, Immobilize, Slow, Taunt).</summary>
        public StatusEffectType type;

        /// <summary>Remaining duration in seconds.</summary>
        public float duration;

        /// <summary>Effect strength. Slow: 0.3 = 30% slow. Mark: 0.25 = 25% bonus damage.</summary>
        public float magnitude;

        /// <summary>Who applied this effect. Null if source was destroyed.</summary>
        public Transform source;

        public StatusEffect(StatusEffectType type, float duration, float magnitude, Transform source)
        {
            this.type = type;
            this.duration = duration;
            this.magnitude = magnitude;
            this.source = source;
        }
    }
}
