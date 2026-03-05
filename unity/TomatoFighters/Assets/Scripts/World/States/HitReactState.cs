using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Entered when the enemy takes damage. Waits for stagger duration then
    /// returns to Chase. If stunned, stays until EnemyAI.NotifyRecovered() triggers exit.
    /// </summary>
    public class HitReactState : EnemyStateBase
    {
        private readonly bool _isStun;
        private float _staggerTimer;

        /// <param name="isStun">True if this is a full stun (wait for NotifyRecovered), false for brief stagger.</param>
        public HitReactState(EnemyAI context, bool isStun) : base(context)
        {
            _isStun = isStun;
        }

        public override void Enter()
        {
            Context.Rb.linearVelocity = Vector2.zero;
            Context.SetActiveAttack(null);

            // Drive animator reaction triggers
            var animator = Context.Animator;
            if (animator != null)
            {
                if (_isStun)
                    animator.SetTrigger("StunTrigger");
                else
                    animator.SetTrigger("Hurt");
            }

            if (!_isStun)
            {
                _staggerTimer = Context.Data.hitReactDuration;
            }
            // If stun, timer is managed by EnemyBase.StunRoutine — we wait for NotifyRecovered
        }

        public override void Tick(float dt)
        {
            // Stun: stay in this state until NotifyRecovered calls TransitionTo
            if (_isStun) return;

            // Brief stagger: count down then resume
            _staggerTimer -= dt;
            if (_staggerTimer <= 0f)
            {
                Context.TransitionTo(new ChaseState(Context));
            }
        }
    }
}
