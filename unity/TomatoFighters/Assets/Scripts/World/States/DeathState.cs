namespace TomatoFighters.World.States
{
    /// <summary>
    /// Terminal state entered when the enemy dies. Disables AI processing.
    /// EnemyBase handles collider disabling, death animation, and Destroy.
    /// </summary>
    public class DeathState : EnemyStateBase
    {
        public DeathState(EnemyAI context) : base(context) { }

        public override void Enter()
        {
            Context.SetActiveAttack(null);
            Context.Rb.linearVelocity = UnityEngine.Vector2.zero;
        }

        public override void Tick(float dt)
        {
            // Terminal state — no transitions
        }
    }
}
