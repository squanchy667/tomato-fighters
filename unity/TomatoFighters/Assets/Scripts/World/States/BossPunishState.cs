using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Post-big-attack vulnerability window. The boss is immobile and exposed
    /// for a configurable duration. Sets <see cref="BossEnemy.IsInPunishableState"/>
    /// to true so combat systems can apply bonus damage.
    /// </summary>
    public class BossPunishState : EnemyStateBase
    {
        private readonly float _duration;
        private float _timer;

        /// <param name="duration">Seconds the boss remains vulnerable.</param>
        public BossPunishState(EnemyAI context, float duration) : base(context)
        {
            _duration = duration;
        }

        public override void Enter()
        {
            _timer = _duration;
            Context.Rb.linearVelocity = Vector2.zero;
            Context.SetActiveAttack(null);

            // Signal punishable state on BossEnemy if present
            var bossEnemy = Context.EnemyBase as BossEnemy;
            if (bossEnemy != null)
                bossEnemy.SetPunishable(true);

            // Visual: yellow tint to indicate vulnerability
            var sprite = Context.EnemyBase.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
                sprite.color = new Color(1f, 0.9f, 0.2f);
        }

        public override void Tick(float dt)
        {
            _timer -= dt;
            if (_timer <= 0f)
            {
                Context.TransitionTo(new ChaseState(Context));
            }
        }

        public override void Exit()
        {
            var bossEnemy = Context.EnemyBase as BossEnemy;
            if (bossEnemy != null)
                bossEnemy.SetPunishable(false);

            // Restore color — BossAI will re-apply enrage tint if needed
            var telegraphCtrl = Context.TelegraphVisual;
            if (telegraphCtrl != null)
                telegraphCtrl.RestoreColor();
            else
            {
                var sprite = Context.EnemyBase.GetComponentInChildren<SpriteRenderer>();
                if (sprite != null) sprite.color = Color.white;
            }
        }
    }
}
