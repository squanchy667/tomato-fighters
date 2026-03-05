using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Enemy moves toward the current target. Transitions to Attack when within
    /// attack range, or returns to Patrol if the target leaves leash range.
    /// </summary>
    public class ChaseState : EnemyStateBase
    {
        public ChaseState(EnemyAI context) : base(context) { }

        public override void Enter()
        {
            Context.UpdateTarget();
        }

        public override void Tick(float dt)
        {
            // Lost target or too far — go back to patrol
            if (Context.IsPlayerBeyondLeash())
            {
                Context.TransitionTo(new PatrolState(Context));
                return;
            }

            // In attack range — attack
            if (Context.IsPlayerInAttackRange())
            {
                Debug.Log($"[ChaseState] In attack range, transitioning to Attack");
                Context.TransitionTo(new AttackState(Context));
                return;
            }

            // Move toward target
            if (Context.CurrentTarget != null)
            {
                Vector2 dir = Context.DirectionToTarget();
                float speed = Context.Data.movementSpeed;
                Vector2 newPos = Context.Rb.position + dir * speed * dt;
                Context.Rb.MovePosition(newPos);
            }
        }

        public override void Exit()
        {
            Context.Rb.linearVelocity = Vector2.zero;
        }
    }
}
