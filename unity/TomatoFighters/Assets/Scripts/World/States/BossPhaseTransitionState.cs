using System;
using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Invulnerable cinematic pause during boss phase transitions.
    /// Blinks the sprite white for the configured duration, then resumes chasing.
    /// Fires the phase change callback once on entry so BossAI can raise SO events.
    /// </summary>
    public class BossPhaseTransitionState : EnemyStateBase
    {
        private readonly float _duration;
        private readonly int _blinkCount;
        private readonly Action _onTransitionComplete;

        private float _timer;
        private float _blinkInterval;
        private float _blinkTimer;
        private bool _isWhite;

        private SpriteRenderer _sprite;
        private Color _originalColor;

        /// <param name="duration">Total seconds for the transition cinematic.</param>
        /// <param name="blinkCount">Number of white blinks during the transition.</param>
        /// <param name="onTransitionComplete">Called when the cinematic ends (before ChaseState).</param>
        public BossPhaseTransitionState(EnemyAI context, float duration, int blinkCount,
            Action onTransitionComplete = null) : base(context)
        {
            _duration = duration;
            _blinkCount = Mathf.Max(1, blinkCount);
            _onTransitionComplete = onTransitionComplete;
        }

        public override void Enter()
        {
            _timer = _duration;
            _blinkInterval = _duration / (_blinkCount * 2f);
            _blinkTimer = _blinkInterval;
            _isWhite = false;

            Context.Rb.linearVelocity = Vector2.zero;
            Context.SetActiveAttack(null);

            // Make invulnerable during transition
            Context.EnemyBase.SetInvulnerableExternal(true);

            _sprite = Context.EnemyBase.GetComponentInChildren<SpriteRenderer>();
            if (_sprite != null)
                _originalColor = _sprite.color;
        }

        public override void Tick(float dt)
        {
            _timer -= dt;

            // Blink effect
            _blinkTimer -= dt;
            if (_blinkTimer <= 0f && _sprite != null)
            {
                _isWhite = !_isWhite;
                _sprite.color = _isWhite ? Color.white : _originalColor;
                _blinkTimer = _blinkInterval;
            }

            if (_timer <= 0f)
            {
                _onTransitionComplete?.Invoke();
                Context.TransitionTo(new ChaseState(Context));
            }
        }

        public override void Exit()
        {
            Context.EnemyBase.SetInvulnerableExternal(false);

            if (_sprite != null)
                _sprite.color = _originalColor;
        }
    }
}
