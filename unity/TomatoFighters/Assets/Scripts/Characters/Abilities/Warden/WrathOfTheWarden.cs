using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Warden
{
    /// <summary>
    /// Warden T3 signature (Main only): when Brutor takes cumulative damage equal to
    /// 50% of max HP within 10 seconds, Wrath activates for 6 seconds.
    /// During Wrath: ATK doubled, attacks gain AoE shockwave, immune to stagger.
    /// Cooldown: 30s.
    /// </summary>
    public class WrathOfTheWarden : IPathAbility
    {
        private const string ID = "Warden_WrathOfTheWarden";
        private const float DAMAGE_THRESHOLD_RATIO = 0.5f;
        private const float TRACKING_WINDOW = 10f;
        private const float WRATH_DURATION = 6f;
        private const float COOLDOWN = 30f;
        private const float ATK_MULTIPLIER = 2f;
        private const float SHOCKWAVE_RANGE = 3f;
        private const float SHOCKWAVE_DAMAGE = 15f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _wrathTimeRemaining;
        private bool _isWrathActive;

        // Damage tracking for auto-trigger
        private float _cumulativeDamage;
        private float _trackingTimer;

        public WrathOfTheWarden(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isWrathActive;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>ATK multiplier while Wrath is active. Combat pipeline queries this.</summary>
        public float AttackMultiplier => _isWrathActive ? ATK_MULTIPLIER : 1f;

        /// <summary>Whether stagger is immune during Wrath.</summary>
        public bool StaggerImmune => _isWrathActive;

        /// <summary>
        /// Track incoming damage for auto-trigger. Called by PlayerDamageable pipeline.
        /// </summary>
        public void OnDamageTaken(float amount)
        {
            if (_isWrathActive || _cooldownRemaining > 0f) return;

            _cumulativeDamage += amount;
            _trackingTimer = TRACKING_WINDOW;

            float threshold = (_ctx.PlayerDamageable != null ? _ctx.PlayerDamageable.MaxHealth : 200f)
                * DAMAGE_THRESHOLD_RATIO;

            if (_cumulativeDamage >= threshold)
            {
                TryActivate();
            }
        }

        public bool TryActivate()
        {
            _isWrathActive = true;
            _wrathTimeRemaining = WRATH_DURATION;
            _cooldownRemaining = COOLDOWN;
            _cumulativeDamage = 0f;
            _trackingTimer = 0f;

            Debug.Log($"[WrathOfTheWarden] WRATH ACTIVATED — {ATK_MULTIPLIER}x ATK, AoE shockwave, stagger immune for {WRATH_DURATION}s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            // Decay damage tracking window
            if (_trackingTimer > 0f)
            {
                _trackingTimer -= deltaTime;
                if (_trackingTimer <= 0f)
                    _cumulativeDamage = 0f;
            }

            if (_isWrathActive)
            {
                _wrathTimeRemaining -= deltaTime;
                if (_wrathTimeRemaining <= 0f)
                {
                    _isWrathActive = false;
                    Debug.Log("[WrathOfTheWarden] Wrath expired");
                }
            }
        }

        /// <summary>
        /// Called on each attack hit during Wrath to spawn AoE shockwave.
        /// </summary>
        public void OnAttackHit(Vector2 hitPosition)
        {
            if (!_isWrathActive) return;

            var hits = Physics2D.OverlapCircleAll(hitPosition, SHOCKWAVE_RANGE, _ctx.EnemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsInvulnerable)
                {
                    var packet = new DamagePacket(
                        type: DamageType.Physical,
                        amount: SHOCKWAVE_DAMAGE,
                        isPunishDamage: false,
                        knockbackForce: Vector2.zero,
                        launchForce: Vector2.zero,
                        source: CharacterType.Brutor,
                        stunFillAmount: 2f);
                    damageable.TakeDamage(packet);
                }
            }
        }

        public void Cleanup()
        {
            _isWrathActive = false;
            _wrathTimeRemaining = 0f;
            _cooldownRemaining = 0f;
            _cumulativeDamage = 0f;
            _trackingTimer = 0f;
        }
    }
}
