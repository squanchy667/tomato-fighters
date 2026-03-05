using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Tracks active status effects on an entity. Ticks durations each frame,
    /// removes expired effects, and provides queries for combat systems.
    /// Add to enemies via Creator Scripts.
    /// </summary>
    public class StatusEffectTracker : MonoBehaviour, IStatusEffectable
    {
        private readonly Dictionary<StatusEffectType, StatusEffect> _effects = new();
        private readonly List<StatusEffectType> _expiredKeys = new();

        private void Update()
        {
            if (_effects.Count == 0) return;

            float dt = Time.deltaTime;
            _expiredKeys.Clear();

            // Tick durations — collect expired keys to remove after iteration
            foreach (var kvp in _effects)
            {
                var effect = kvp.Value;
                effect.duration -= dt;

                if (effect.duration <= 0f)
                {
                    _expiredKeys.Add(kvp.Key);
                }
                else
                {
                    _effects[kvp.Key] = effect;
                }
            }

            foreach (var key in _expiredKeys)
            {
                _effects.Remove(key);
            }
        }

        /// <inheritdoc/>
        public void AddEffect(StatusEffect effect)
        {
            _effects[effect.type] = effect;

            // Taunt: force AI targeting within World pillar (keeps Combat decoupled)
            if (effect.type == StatusEffectType.Taunt && effect.source != null)
            {
                var ai = GetComponent<EnemyAI>();
                ai?.ForceTarget(effect.source, effect.duration);
            }
        }

        /// <inheritdoc/>
        public bool HasEffect(StatusEffectType type)
        {
            return _effects.ContainsKey(type);
        }

        /// <inheritdoc/>
        public StatusEffect? GetEffect(StatusEffectType type)
        {
            return _effects.TryGetValue(type, out var effect) ? effect : null;
        }

        /// <inheritdoc/>
        public float GetSlowMultiplier()
        {
            if (_effects.TryGetValue(StatusEffectType.Slow, out var slow))
                return 1f - slow.magnitude;
            return 1f;
        }

        /// <inheritdoc/>
        public bool IsImmobilized()
        {
            return _effects.ContainsKey(StatusEffectType.Immobilize);
        }

        /// <summary>Remove all active effects. Called on death or phase transition.</summary>
        public void ClearAll()
        {
            _effects.Clear();
        }
    }
}
