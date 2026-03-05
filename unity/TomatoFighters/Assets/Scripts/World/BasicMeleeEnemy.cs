using System.Collections;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Concrete melee enemy using the <see cref="EnemyAI"/> state machine.
    /// Forwards EnemyBase virtual hooks to EnemyAI for state transitions.
    /// Overrides IAttacker properties based on the active attack from AI.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class BasicMeleeEnemy : EnemyBase
    {
        private EnemyAI _ai;

        // ── IAttacker Overrides (driven by EnemyAI.ActiveAttack) ─────────

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

        // ── Unity Lifecycle ──────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _ai = GetComponent<EnemyAI>();
        }

        // ── EnemyBase Virtual Hooks → EnemyAI ───────────────────────────

        protected override void OnDamaged(DamagePacket damage)
        {
            _ai?.NotifyDamaged(damage);

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

        private Coroutine _damageFlash;

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
