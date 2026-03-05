using TomatoFighters.Shared.Events;
using TomatoFighters.World.States;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Phase-based boss AI that sits alongside <see cref="EnemyAI"/> as a companion component.
    /// Monitors HP%, swaps attack pools via <see cref="EnemyAI.SetAttackPool"/>,
    /// adjusts tempo, and triggers phase transition cinematics.
    /// Regular enemies are unaffected — BossAI is opt-in.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class BossAI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private BossData bossData;

        [Header("Events")]
        [Tooltip("Fired on phase transitions for camera/UI integration.")]
        [SerializeField] private VoidEventChannel onBossPhaseChanged;

        private EnemyAI _enemyAI;
        private EnemyBase _enemyBase;
        private SpriteRenderer _sprite;

        private int _currentPhaseIndex;
        private bool _isTransitioning;
        private Color _baseColor;

        /// <summary>The current phase index (0-based).</summary>
        public int CurrentPhaseIndex => _currentPhaseIndex;

        /// <summary>The BossData driving this boss.</summary>
        public BossData Data => bossData;

        /// <summary>Whether a phase transition cinematic is playing.</summary>
        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            _enemyAI = GetComponent<EnemyAI>();
            _enemyBase = GetComponent<EnemyBase>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            if (bossData == null || bossData.phases == null || bossData.phases.Length == 0)
            {
                Debug.LogError("[BossAI] No BossData or phases assigned. Disabling.", this);
                enabled = false;
                return;
            }

            if (_sprite != null)
                _baseColor = _sprite.color;

            // Apply initial phase (phase 0)
            ApplyPhase(0);
        }

        private void OnEnable()
        {
            if (_enemyBase != null)
                _enemyBase.OnDied += HandleDeath;
        }

        private void OnDisable()
        {
            if (_enemyBase != null)
                _enemyBase.OnDied -= HandleDeath;
        }

        /// <summary>
        /// Called by BossEnemy.OnDamaged to check for phase transitions.
        /// Uses current HP ratio to find the correct phase — skips intermediate
        /// phases if a massive hit crosses multiple thresholds (DD-4).
        /// </summary>
        public void NotifyDamaged()
        {
            if (_isTransitioning) return;
            if (_enemyBase.CurrentHealth <= 0f) return;

            float hpPercent = _enemyBase.CurrentHealth / _enemyBase.MaxHealth;
            int targetPhase = FindPhaseForHpPercent(hpPercent);

            if (targetPhase > _currentPhaseIndex)
            {
                BeginPhaseTransition(targetPhase);
            }
        }

        /// <summary>
        /// Finds the highest phase index whose HP threshold has been reached.
        /// Phases are ordered 0..N where higher index = lower HP threshold.
        /// </summary>
        private int FindPhaseForHpPercent(float hpPercent)
        {
            var phases = bossData.phases;
            int best = _currentPhaseIndex;

            for (int i = _currentPhaseIndex + 1; i < phases.Length; i++)
            {
                if (hpPercent <= phases[i].hpThreshold)
                    best = i;
            }

            return best;
        }

        private void BeginPhaseTransition(int newPhaseIndex)
        {
            _isTransitioning = true;

            var transitionState = new BossPhaseTransitionState(
                _enemyAI,
                bossData.phaseTransitionDuration,
                bossData.transitionBlinkCount,
                onTransitionComplete: () =>
                {
                    ApplyPhase(newPhaseIndex);
                    _isTransitioning = false;

                    onBossPhaseChanged?.Raise();
                }
            );

            _enemyAI.TransitionTo(transitionState);
        }

        private void ApplyPhase(int phaseIndex)
        {
            _currentPhaseIndex = phaseIndex;
            var phase = bossData.phases[phaseIndex];

            // Swap attack pool
            _enemyAI.SetAttackPool(phase.attacks);

            // Apply tempo
            _enemyAI.SetTempoMultiplier(phase.tempoMultiplier);

            // Enrage visual
            if (_sprite != null)
            {
                _sprite.color = phase.enableEnrage ? bossData.enrageColor : _baseColor;
            }

            Debug.Log($"[BossAI] Phase {phaseIndex}: '{phase.phaseName}' — " +
                      $"tempo {phase.tempoMultiplier}x, attacks: {phase.attacks?.Length ?? 0}, " +
                      $"enrage: {phase.enableEnrage}");
        }

        private void HandleDeath()
        {
            _isTransitioning = false;
            enabled = false;
        }
    }
}
