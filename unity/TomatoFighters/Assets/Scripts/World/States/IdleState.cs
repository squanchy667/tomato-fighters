using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Enemy waits for a configurable duration, then transitions to Patrol.
    /// Immediately transitions to Chase if a player enters aggro range.
    /// </summary>
    public class IdleState : EnemyStateBase
    {
        private float _timer;

        public IdleState(EnemyAI context) : base(context) { }

        public override void Enter()
        {
            _timer = Context.Data.idleDuration;
            Context.Rb.linearVelocity = Vector2.zero;
        }

        public override void Tick(float dt)
        {
            if (Context.IsPlayerInAggroRange())
            {
                Context.TransitionTo(new ChaseState(Context));
                return;
            }

            _timer -= dt;
            if (_timer <= 0f)
            {
                Context.TransitionTo(new PatrolState(Context));
            }
        }
    }
}
