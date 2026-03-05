using System.Collections;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Concrete boss enemy. Extends <see cref="EnemyBase"/> with boss-specific
    /// IAttacker overrides and bridges virtual hooks to both
    /// <see cref="EnemyAI"/> and <see cref="BossAI"/>.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    [RequireComponent(typeof(BossAI))]
    public class BossEnemy : EnemyBase
    {
        private EnemyAI _ai;
        private BossAI _bossAI;

        private bool _isInPunishableState;
        private Coroutine _damageFlash;

        // ── IAttacker Overrides ─────────────────────────────────────────

        /// <inheritdoc/>
        public override AttackData CurrentAttack => _ai != null ? _ai.ActiveAttack : null;

        /// <inheritdoc/>
        public override bool IsCurrentAttackUnstoppable => _ai != null && _ai.IsPerformingUnstoppable;

        /// <inheritdoc/>
        public override TelegraphType CurrentTelegraphType
        {
            get
            {
                var attack = CurrentAttack;
                return attack != null ? attack.telegraphType : TelegraphType.Normal;
            }
        }

        /// <inheritdoc/>
        public override float PunishWindowDuration
        {
            get
            {
                var attack = CurrentAttack;
                return attack != null && attack.hasPunishWindow ? attack.punishWindowDuration : 0f;
            }
        }

        /// <inheritdoc/>
        public override bool IsInPunishableState => _isInPunishableState;

        // ── Punish State Control ────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="States.BossPunishState"/> to toggle punishable flag.
        /// </summary>
        public void SetPunishable(bool punishable)
        {
            _isInPunishableState = punishable;
        }

        // ── Unity Lifecycle ─────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _ai = GetComponent<EnemyAI>();
            _bossAI = GetComponent<BossAI>();
        }

        // ── EnemyBase Virtual Hooks ─────────────────────────────────────

        protected override void OnDamaged(DamagePacket damage)
        {
            _ai?.NotifyDamaged(damage);

            // Notify BossAI for phase transition checks
            _bossAI?.NotifyDamaged();

            if (_damageFlash != null) StopCoroutine(_damageFlash);
            _damageFlash = StartCoroutine(DamageFlashRoutine());
        }

        protected override void OnStunned()
        {
            _ai?.NotifyStunned();
        }

        protected override void OnRecovery()
        {
            _ai?.NotifyRecovered();
        }

        // ── Visual Feedback ─────────────────────────────────────────────

        private IEnumerator DamageFlashRoutine()
        {
            Color original = Sprite.color;
            Sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            Sprite.color = original;
            _damageFlash = null;
        }
    }
}
