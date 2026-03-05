using System;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.World.States;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Drives the enemy AI state machine. Ticks the current state each frame,
    /// handles state transitions, and manages player targeting via Physics2D overlap.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Enemy data SO — same reference as on EnemyBase.")]
        [SerializeField] private EnemyData enemyData;

        [Tooltip("Layer mask for detecting player characters.")]
        [SerializeField] private LayerMask playerLayer;

        // ── Cached References ─────────────────────────────────────────────

        private EnemyBase _enemyBase;
        private Rigidbody2D _rb;
        private EnemyData _data;
        private Vector2 _spawnPosition;

        /// <summary>The EnemyBase component on this GameObject.</summary>
        public EnemyBase EnemyBase => _enemyBase;

        /// <summary>The Rigidbody2D used for physics-based movement.</summary>
        public Rigidbody2D Rb => _rb;

        /// <summary>The EnemyData SO driving all configurable values.</summary>
        public EnemyData Data => _data;

        /// <summary>The position this enemy was at on Awake. Used by PatrolState.</summary>
        public Vector2 SpawnPosition => _spawnPosition;

        // ── State Machine ─────────────────────────────────────────────────

        private EnemyStateBase _currentState;
        private bool _isDead;

        /// <summary>The currently active AI state.</summary>
        public EnemyStateBase CurrentState => _currentState;

        /// <summary>Fired when the AI transitions to a new state. Args: old state, new state.</summary>
        public event Action<EnemyStateBase, EnemyStateBase> OnStateChanged;

        // ── Targeting ─────────────────────────────────────────────────────

        private Transform _currentTarget;
        private Transform _forcedTarget;
        private float _forcedTargetExpiry;
        private float _targetUpdateTimer;
        private const float TARGET_UPDATE_INTERVAL = 0.5f;

        /// <summary>The current target. Returns forced target if taunted, otherwise nearest player.</summary>
        public Transform CurrentTarget
        {
            get
            {
                if (_forcedTarget != null && Time.time < _forcedTargetExpiry)
                    return _forcedTarget;

                // Taunt expired
                if (_forcedTarget != null)
                    _forcedTarget = null;

                return _currentTarget;
            }
        }

        // ── IAttacker State (set by AttackState) ──────────────────────────

        private AttackData _activeAttack;
        private bool _isPerformingUnstoppable;

        /// <summary>The attack currently being executed, or null if not attacking.</summary>
        public AttackData ActiveAttack => _activeAttack;

        /// <summary>Set by AttackState when an attack begins/ends.</summary>
        public void SetActiveAttack(AttackData attack)
        {
            _activeAttack = attack;
            _isPerformingUnstoppable = attack != null &&
                attack.telegraphType == TelegraphType.Unstoppable;
        }

        /// <summary>Whether the enemy is currently performing an Unstoppable attack (super armor).</summary>
        public bool IsPerformingUnstoppable => _isPerformingUnstoppable;

        // ── Unity Lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();
            _rb = GetComponent<Rigidbody2D>();
            _spawnPosition = transform.position;
        }

        private void Start()
        {
            _data = enemyData;

            if (_data == null)
            {
                Debug.LogError("[EnemyAI] No EnemyData assigned. Disabling AI.", this);
                enabled = false;
                return;
            }

            TransitionTo(new IdleState(this));
        }

        private void OnEnable()
        {
            if (_enemyBase != null)
            {
                _enemyBase.OnDied += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_enemyBase != null)
            {
                _enemyBase.OnDied -= HandleDeath;
            }
        }

        private void Update()
        {
            if (_isDead) return;

            _currentState?.Tick(Time.deltaTime);

            // Periodic target updates
            _targetUpdateTimer -= Time.deltaTime;
            if (_targetUpdateTimer <= 0f)
            {
                UpdateTarget();
                _targetUpdateTimer = TARGET_UPDATE_INTERVAL;
            }
        }

        // ── State Transitions ─────────────────────────────────────────────

        /// <summary>Transition to a new AI state. Calls Exit on current, Enter on new.</summary>
        public void TransitionTo(EnemyStateBase newState)
        {
            if (_isDead && newState is not DeathState) return;

            var oldState = _currentState;
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();

            OnStateChanged?.Invoke(oldState, newState);
        }

        // ── Targeting ─────────────────────────────────────────────────────

        /// <summary>
        /// Override position-based targeting. Used by Provoke (T028) and similar abilities.
        /// After duration expires, position-based targeting resumes.
        /// </summary>
        public void ForceTarget(Transform target, float duration)
        {
            _forcedTarget = target;
            _forcedTargetExpiry = Time.time + duration;
        }

        /// <summary>
        /// Scans for the nearest player using Physics2D overlap on the player layer.
        /// Called periodically, not every frame.
        /// </summary>
        public void UpdateTarget()
        {
            // Don't override forced target
            if (_forcedTarget != null && Time.time < _forcedTargetExpiry)
                return;

            if (_forcedTarget != null)
                _forcedTarget = null;

            float aggroRange = _data != null ? _data.aggroRange : 8f;
            var hits = Physics2D.OverlapCircleAll(transform.position, aggroRange, playerLayer);

            if (hits.Length == 0)
            {
                _currentTarget = null;
                return;
            }

            float bestDist = float.MaxValue;
            Transform bestTarget = null;

            foreach (var hit in hits)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = hit.transform;
                }
            }

            _currentTarget = bestTarget;
        }

        // ── Queries ───────────────────────────────────────────────────────

        /// <summary>Whether a player is within aggro range.</summary>
        public bool IsPlayerInAggroRange()
        {
            return CurrentTarget != null &&
                Vector2.Distance(transform.position, CurrentTarget.position) <= _data.aggroRange;
        }

        /// <summary>Whether the current target is within attack range.</summary>
        public bool IsPlayerInAttackRange()
        {
            return CurrentTarget != null &&
                Vector2.Distance(transform.position, CurrentTarget.position) <= _data.attackRange;
        }

        /// <summary>Whether the current target is beyond leash range (too far to chase).</summary>
        public bool IsPlayerBeyondLeash()
        {
            return CurrentTarget == null ||
                Vector2.Distance(transform.position, CurrentTarget.position) > _data.leashRange;
        }

        /// <summary>Normalized direction from this enemy toward the current target.</summary>
        public Vector2 DirectionToTarget()
        {
            if (CurrentTarget == null) return Vector2.zero;
            return ((Vector2)(CurrentTarget.position - transform.position)).normalized;
        }

        // ── EnemyBase Event Handlers ──────────────────────────────────────

        /// <summary>
        /// Called by EnemyBase when this enemy takes damage.
        /// Triggers HitReact unless performing an Unstoppable attack.
        /// </summary>
        public void NotifyDamaged(DamagePacket damage)
        {
            if (_isDead) return;

            // Unstoppable attacks = super armor, ignore hit react
            if (_isPerformingUnstoppable) return;

            // Stun is handled separately by EnemyBase — AI just goes to HitReact
            if (_enemyBase.IsStunned)
            {
                TransitionTo(new HitReactState(this, isStun: true));
                return;
            }

            TransitionTo(new HitReactState(this, isStun: false));
        }

        /// <summary>Called by EnemyBase when stun begins. Forces HitReact even during Unstoppable.</summary>
        public void NotifyStunned()
        {
            if (_isDead) return;
            TransitionTo(new HitReactState(this, isStun: true));
        }

        /// <summary>Called by EnemyBase when stun recovery finishes.</summary>
        public void NotifyRecovered()
        {
            if (_isDead) return;

            // Resume chasing after stun recovery
            if (_currentState is HitReactState)
                TransitionTo(new ChaseState(this));
        }

        private void HandleDeath()
        {
            _isDead = true;
            TransitionTo(new DeathState(this));
        }

        // ── Gizmos ────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            var data = _data != null ? _data : enemyData;
            if (data == null) return;

            // Aggro range (yellow)
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, data.aggroRange);

            // Attack range (red)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, data.attackRange);

            // Patrol radius (green)
            Vector2 origin = Application.isPlaying ? _spawnPosition : (Vector2)transform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(origin, data.patrolRadius);

            // Leash range (blue)
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.1f);
            Gizmos.DrawWireSphere(origin, data.leashRange);
        }
    }
}
