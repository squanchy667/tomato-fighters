namespace TomatoFighters.World
{
    /// <summary>
    /// Abstract base class for all enemy AI states.
    /// States follow Enter/Tick/Exit lifecycle driven by <see cref="EnemyAI"/>.
    /// </summary>
    public abstract class EnemyStateBase
    {
        protected readonly EnemyAI Context;

        protected EnemyStateBase(EnemyAI context)
        {
            Context = context;
        }

        /// <summary>Called once when the state machine enters this state.</summary>
        public virtual void Enter() { }

        /// <summary>Called every frame while this state is active.</summary>
        public abstract void Tick(float dt);

        /// <summary>Called once when the state machine exits this state.</summary>
        public virtual void Exit() { }
    }
}
