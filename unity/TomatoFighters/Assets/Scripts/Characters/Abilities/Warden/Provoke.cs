using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Warden
{
    /// <summary>
    /// Warden T1 active: AoE taunt that forces nearby enemies to target the player.
    /// Applies Taunt status via <see cref="IStatusEffectable"/>. The World pillar's
    /// StatusEffectTracker handles AI targeting override when it receives a Taunt effect.
    /// Duration: 4s, Cooldown: 8s.
    /// </summary>
    public class Provoke : IPathAbility
    {
        private const string ID = "Warden_Provoke";
        private const float TAUNT_DURATION = 4f;
        private const float TAUNT_RANGE = 5f;
        private const float COOLDOWN = 8f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private bool _isActive;

        public Provoke(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isActive;
        public float CooldownRemaining => _cooldownRemaining;

        public bool TryActivate()
        {
            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, TAUNT_RANGE, _ctx.EnemyLayer);

            int tauntedCount = 0;
            foreach (var hit in hits)
            {
                // Apply taunt status — StatusEffectTracker handles AI targeting internally
                var statusEffectable = hit.GetComponent<IStatusEffectable>();
                if (statusEffectable == null)
                    statusEffectable = hit.GetComponentInParent<IStatusEffectable>();

                if (statusEffectable != null)
                {
                    statusEffectable.AddEffect(new StatusEffect(
                        StatusEffectType.Taunt, TAUNT_DURATION, 0f, _ctx.PlayerTransform));
                    tauntedCount++;
                }
            }

            _cooldownRemaining = COOLDOWN;
            _isActive = true;
            Debug.Log($"[Provoke] Taunted {tauntedCount} enemies for {TAUNT_DURATION}s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            // Active state is instant — no sustained effect to tick
            _isActive = false;
        }

        public void Cleanup()
        {
            _isActive = false;
            _cooldownRemaining = 0f;
        }
    }
}
