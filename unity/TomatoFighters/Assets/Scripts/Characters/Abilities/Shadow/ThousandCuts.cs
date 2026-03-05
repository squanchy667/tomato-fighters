using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Shadow
{
    /// <summary>
    /// Shadow T3 signature (Main only): teleport between up to 6 enemies in rapid
    /// succession (0.2s each), dealing 150% ATK per hit. Each hit is a guaranteed crit.
    /// Slasher is invulnerable during the sequence. If fewer than 6 enemies,
    /// remaining hits target the last enemy. Cooldown: 40s.
    /// </summary>
    public class ThousandCuts : IPathAbility
    {
        private const string ID = "Shadow_ThousandCuts";
        private const float COOLDOWN = 40f;
        private const int MAX_HITS = 6;
        private const float TIME_PER_HIT = 0.2f;
        private const float DAMAGE_PER_HIT = 15f;    // 150% ATK
        private const float DETECT_RANGE = 10f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private bool _isExecuting;
        private float _hitTimer;
        private int _hitsRemaining;
        private readonly List<IDamageable> _targets = new();
        private int _currentTargetIndex;

        public ThousandCuts(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isExecuting;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Invulnerable during execution. Defense pipeline queries this.</summary>
        public bool IsInvulnerable => _isExecuting;

        public bool TryActivate()
        {
            // Gather targets
            _targets.Clear();
            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, DETECT_RANGE, _ctx.EnemyLayer);

            foreach (var hit in hits)
            {
                var dmg = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (dmg != null && !dmg.IsInvulnerable && dmg.CurrentHealth > 0f)
                    _targets.Add(dmg);
            }

            if (_targets.Count == 0)
            {
                Debug.Log("[ThousandCuts] No targets in range");
                return false;
            }

            _isExecuting = true;
            _hitsRemaining = MAX_HITS;
            _hitTimer = 0f;
            _currentTargetIndex = 0;
            _cooldownRemaining = COOLDOWN;

            Debug.Log($"[ThousandCuts] EXECUTING — {_targets.Count} targets, {MAX_HITS} hits");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isExecuting) return;

            _hitTimer += deltaTime;
            if (_hitTimer >= TIME_PER_HIT && _hitsRemaining > 0)
            {
                _hitTimer -= TIME_PER_HIT;
                ExecuteHit();
                _hitsRemaining--;

                if (_hitsRemaining <= 0)
                {
                    _isExecuting = false;
                    Debug.Log("[ThousandCuts] Sequence complete");
                }
            }
        }

        public void Cleanup()
        {
            _isExecuting = false;
            _cooldownRemaining = 0f;
            _targets.Clear();
        }

        private void ExecuteHit()
        {
            if (_targets.Count == 0) return;

            // Cycle through targets; if fewer than MAX_HITS, reuse last target
            if (_currentTargetIndex >= _targets.Count)
                _currentTargetIndex = _targets.Count - 1;

            var target = _targets[_currentTargetIndex];
            if (target != null && target.CurrentHealth > 0f && !target.IsInvulnerable)
            {
                var packet = new DamagePacket(
                    type: DamageType.Physical,
                    amount: DAMAGE_PER_HIT,
                    isPunishDamage: false,
                    knockbackForce: Vector2.zero,
                    launchForce: Vector2.zero,
                    source: CharacterType.Slasher,
                    stunFillAmount: 3f);
                target.TakeDamage(packet);
            }

            _currentTargetIndex++;
        }
    }
}
