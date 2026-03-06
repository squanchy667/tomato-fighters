using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Conjurer
{
    /// <summary>
    /// Conjurer T2 passive (Totem Pulse): Sproutlings pulse AoE damage (50% ATK)
    /// and 20% slow in a 2-unit radius every 2 seconds.
    /// Enhances existing SummonSproutling (T1) companions.
    /// </summary>
    public class DeployTotem : IPathAbility
    {
        private const string ID = "Conjurer_TotemPulse";
        private const float PULSE_INTERVAL = 2f;
        private const float PULSE_RANGE = 2f;
        private const float PULSE_DAMAGE_MULT = 0.5f;
        private const float PULSE_DAMAGE_BASE = 5f;
        private const float SLOW_MAGNITUDE = 0.2f;
        private const float SLOW_DURATION = 2f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;
        private float _pulseTimer;

        public DeployTotem(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>Whether Sproutlings should pulse AoE. Sproutling AI queries this.</summary>
        public bool TotemPulseEnabled => _isActive;

        /// <summary>
        /// Called by Sproutling AI to execute the pulse effect at the sproutling's position.
        /// </summary>
        public void ExecutePulse(Vector2 pulseOrigin)
        {
            if (!_isActive) return;

            var hits = Physics2D.OverlapCircleAll(pulseOrigin, PULSE_RANGE, _ctx.EnemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsInvulnerable)
                {
                    float damage = PULSE_DAMAGE_BASE * PULSE_DAMAGE_MULT;
                    var packet = new DamagePacket(
                        type: DamageType.Physical,
                        amount: damage,
                        isPunishDamage: false,
                        knockbackForce: Vector2.zero,
                        launchForce: Vector2.zero,
                        source: CharacterType.Mystica,
                        stunFillAmount: 0f);
                    damageable.TakeDamage(packet);
                }

                var statusEffectable = hit.GetComponent<IStatusEffectable>()
                    ?? hit.GetComponentInParent<IStatusEffectable>();
                if (statusEffectable != null)
                {
                    statusEffectable.AddEffect(new StatusEffect(
                        StatusEffectType.Slow, SLOW_DURATION, SLOW_MAGNITUDE, null));
                }
            }
        }

        public bool TryActivate()
        {
            _isActive = true;
            Debug.Log("[DeployTotem] Totem Pulse active — Sproutlings pulse AoE damage + slow");
            return true;
        }

        public void Tick(float deltaTime) { }

        public void Cleanup()
        {
            _isActive = false;
            _pulseTimer = 0f;
        }
    }
}
