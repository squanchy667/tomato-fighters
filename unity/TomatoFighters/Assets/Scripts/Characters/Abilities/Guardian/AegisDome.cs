using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Guardian
{
    /// <summary>
    /// Guardian T3 signature (Main only): deploy a protective dome for 5 seconds.
    /// Allies inside take 60% less damage. Enemies inside are slowed 30%.
    /// On expire: pulse heal 20% max HP to all allies inside. Cooldown: 60s.
    /// </summary>
    public class AegisDome : IPathAbility
    {
        private const string ID = "Guardian_AegisDome";
        private const float DURATION = 5f;
        private const float COOLDOWN = 60f;
        private const float DOME_RADIUS = 5f;
        private const float DAMAGE_REDUCTION = 0.6f;
        private const float ENEMY_SLOW = 0.3f;
        private const float HEAL_PULSE_RATIO = 0.2f; // 20% max HP
        private const float SLOW_TICK_INTERVAL = 1f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _timeRemaining;
        private bool _isDomeActive;
        private float _slowTickTimer;
        private GameObject _domeVisual;

        public AegisDome(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isDomeActive;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>DR for allies inside the dome. Defense pipeline queries this.</summary>
        public float DamageReduction => _isDomeActive ? DAMAGE_REDUCTION : 0f;

        /// <summary>World position of the dome center (placed at activation position).</summary>
        public Vector2 DomeCenter { get; private set; }

        public bool TryActivate()
        {
            DomeCenter = _ctx.PlayerTransform.position;
            _isDomeActive = true;
            _timeRemaining = DURATION;
            _slowTickTimer = 0f;
            _cooldownRemaining = COOLDOWN;

            // Visual placeholder
            _domeVisual = new GameObject("AegisDome");
            _domeVisual.transform.position = (Vector3)DomeCenter;
            var sr = _domeVisual.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0.6f, 1f, 0.2f);
            sr.sortingOrder = -2;

            Debug.Log($"[AegisDome] DEPLOYED at {DomeCenter} — {DURATION}s, {DOME_RADIUS} radius");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isDomeActive) return;

            _timeRemaining -= deltaTime;

            // Periodically slow enemies inside dome
            _slowTickTimer += deltaTime;
            if (_slowTickTimer >= SLOW_TICK_INTERVAL)
            {
                _slowTickTimer -= SLOW_TICK_INTERVAL;
                ApplySlowToEnemies();
            }

            if (_timeRemaining <= 0f)
            {
                PulseHeal();
                DestroyDome();
            }
        }

        public void Cleanup()
        {
            DestroyDome();
            _cooldownRemaining = 0f;
        }

        private void ApplySlowToEnemies()
        {
            var hits = Physics2D.OverlapCircleAll(DomeCenter, DOME_RADIUS, _ctx.EnemyLayer);
            foreach (var hit in hits)
            {
                var statusEffectable = hit.GetComponent<IStatusEffectable>()
                    ?? hit.GetComponentInParent<IStatusEffectable>();
                if (statusEffectable != null)
                {
                    statusEffectable.AddEffect(new StatusEffect(
                        StatusEffectType.Slow, SLOW_TICK_INTERVAL + 0.1f, ENEMY_SLOW, _ctx.PlayerTransform));
                }
            }
        }

        private void PulseHeal()
        {
            // Solo: heal self
            if (_ctx.PlayerDamageable != null)
            {
                float dist = Vector2.Distance(_ctx.PlayerTransform.position, DomeCenter);
                if (dist <= DOME_RADIUS)
                {
                    float healAmount = _ctx.PlayerDamageable.MaxHealth * HEAL_PULSE_RATIO;
                    _ctx.PlayerDamageable.Heal(healAmount);
                    Debug.Log($"[AegisDome] Heal pulse: {healAmount:F0} HP");
                }
            }
        }

        private void DestroyDome()
        {
            _isDomeActive = false;
            _timeRemaining = 0f;
            if (_domeVisual != null)
            {
                Object.Destroy(_domeVisual);
                _domeVisual = null;
            }
            Debug.Log("[AegisDome] Dome expired");
        }
    }
}
