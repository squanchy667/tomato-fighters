using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Enemy picks a random point within patrol radius and moves toward it.
    /// Transitions to Chase if player enters aggro range.
    /// Returns to Idle on arrival or after a timeout.
    /// </summary>
    public class PatrolState : EnemyStateBase
    {
        private Vector2 _patrolTarget;
        private float _timeoutTimer;
        private const float ARRIVAL_THRESHOLD = 0.3f;
        private const float PATROL_TIMEOUT = 5f;

        public PatrolState(EnemyAI context) : base(context) { }

        public override void Enter()
        {
            _patrolTarget = PickPatrolPoint();
            _timeoutTimer = PATROL_TIMEOUT;
        }

        public override void Tick(float dt)
        {
            if (Context.IsPlayerInAggroRange())
            {
                Context.TransitionTo(new ChaseState(Context));
                return;
            }

            // Move toward patrol target
            Vector2 pos = Context.Rb.position;
            Vector2 dir = (_patrolTarget - pos).normalized;
            float speed = Context.Data.movementSpeed * 0.5f; // Patrol at half speed
            Vector2 newPos = pos + dir * speed * dt;
            Context.Rb.MovePosition(newPos);

            // Arrived?
            if (Vector2.Distance(pos, _patrolTarget) < ARRIVAL_THRESHOLD)
            {
                Context.TransitionTo(new IdleState(Context));
                return;
            }

            // Timeout — don't patrol forever
            _timeoutTimer -= dt;
            if (_timeoutTimer <= 0f)
            {
                Context.TransitionTo(new IdleState(Context));
            }
        }

        public override void Exit()
        {
            Context.Rb.linearVelocity = Vector2.zero;
        }

        private Vector2 PickPatrolPoint()
        {
            float radius = Context.Data.patrolRadius;
            Vector2 offset = Random.insideUnitCircle * radius;
            return Context.SpawnPosition + offset;
        }
    }
}
