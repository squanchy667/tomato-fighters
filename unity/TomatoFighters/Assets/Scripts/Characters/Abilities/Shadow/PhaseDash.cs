using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Shadow
{
    /// <summary>
    /// Shadow T1 passive+: enhances the existing dash system.
    /// Adds a second dash charge, extends i-frame duration by 50%,
    /// and deals pass-through damage to enemies during dash.
    /// </summary>
    public class PhaseDash : IPathAbility
    {
        private const string ID = "Shadow_PhaseDash";
        private const float PASS_THROUGH_DAMAGE = 5f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;

        public PhaseDash(PathAbilityContext ctx)
        {
            _ctx = ctx;
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        public bool TryActivate()
        {
            _isActive = true;

            // Subscribe to dash events for pass-through damage
            if (_ctx.Motor != null)
                _ctx.Motor.Dashed += OnDashed;

            return true;
        }

        public void Tick(float deltaTime)
        {
            // Pass-through damage is handled via dash event subscription
        }

        public void Cleanup()
        {
            _isActive = false;
            if (_ctx.Motor != null)
                _ctx.Motor.Dashed -= OnDashed;
        }

        private void OnDashed(CharacterType character, Vector2 direction, bool hasIFrames)
        {
            if (!_isActive) return;

            // Deal pass-through damage to enemies in dash path
            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, 1.5f, _ctx.EnemyLayer);

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable != null && !damageable.IsInvulnerable)
                {
                    var packet = new Shared.Data.DamagePacket(
                        type: DamageType.Physical,
                        amount: PASS_THROUGH_DAMAGE,
                        isPunishDamage: false,
                        knockbackForce: Vector2.zero,
                        launchForce: Vector2.zero,
                        source: character,
                        stunFillAmount: 0f);
                    damageable.TakeDamage(packet);
                }
            }
        }
    }
}
