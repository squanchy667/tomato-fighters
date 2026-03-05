using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Reaper
{
    /// <summary>
    /// Reaper T3 signature (Main only): 4-second spin attack hitting every 0.3s.
    /// Deals 60% ATK per hit to ALL enemies in melee radius.
    /// Immune to stagger, can move at 50% speed. Final hit launches all nearby enemies.
    /// Cooldown: 35s.
    /// </summary>
    public class Whirlwind : IPathAbility
    {
        private const string ID = "Reaper_Whirlwind";
        private const float DURATION = 4f;
        private const float COOLDOWN = 35f;
        private const float HIT_INTERVAL = 0.3f;
        private const float DAMAGE_PER_HIT = 6f;     // 60% ATK
        private const float SPIN_RANGE = 2.5f;
        private const float MOVE_SPEED_MULT = 0.5f;
        private const float LAUNCH_FORCE = 10f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _timeRemaining;
        private float _hitTimer;
        private bool _isSpinning;

        public Whirlwind(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isSpinning;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Whether Slasher is stagger-immune during Whirlwind.</summary>
        public bool StaggerImmune => _isSpinning;

        /// <summary>Speed multiplier during spin. Motor queries this.</summary>
        public float SpeedMultiplier => _isSpinning ? MOVE_SPEED_MULT : 1f;

        public bool TryActivate()
        {
            _isSpinning = true;
            _timeRemaining = DURATION;
            _hitTimer = 0f;
            _cooldownRemaining = COOLDOWN;

            Debug.Log($"[Whirlwind] SPIN! {DURATION}s of AoE destruction");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isSpinning) return;

            _timeRemaining -= deltaTime;
            _hitTimer += deltaTime;

            // Hit tick
            if (_hitTimer >= HIT_INTERVAL)
            {
                _hitTimer -= HIT_INTERVAL;
                bool isFinalHit = _timeRemaining <= 0f;
                SpinHit(isFinalHit);
            }

            if (_timeRemaining <= 0f)
            {
                _isSpinning = false;
                Debug.Log("[Whirlwind] Spin ended");
            }
        }

        public void Cleanup()
        {
            _isSpinning = false;
            _timeRemaining = 0f;
            _cooldownRemaining = 0f;
        }

        private void SpinHit(bool isFinalHit)
        {
            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, SPIN_RANGE, _ctx.EnemyLayer);

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsInvulnerable) continue;

                Vector2 launchForce = Vector2.zero;
                if (isFinalHit)
                    launchForce = Vector2.up * LAUNCH_FORCE;

                var packet = new DamagePacket(
                    type: DamageType.Physical,
                    amount: DAMAGE_PER_HIT,
                    isPunishDamage: false,
                    knockbackForce: Vector2.zero,
                    launchForce: launchForce,
                    source: CharacterType.Slasher,
                    stunFillAmount: isFinalHit ? 5f : 1f);
                damageable.TakeDamage(packet);
            }
        }
    }
}
