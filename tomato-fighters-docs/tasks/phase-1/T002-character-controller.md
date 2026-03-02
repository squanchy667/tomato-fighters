# T002: CharacterController — Movement, Jump, Dash

| Field | Value |
|-------|-------|
| **Task ID** | T002 |
| **Phase** | 1 — Foundation |
| **Owner** | Dev 1 (Combat) |
| **Agent** | combat-agent |
| **Status** | DONE |
| **Completed** | 2026-03-02 |
| **Depends On** | T001 |
| **Blocks** | T004, T005, T006, T007, T050 |

## Description

Physics-based character movement using Rigidbody2D. Horizontal movement with acceleration, jump with coyote time and buffer, dash with i-frames and cooldown. ScriptableObject config for per-character tuning. Unity new Input System integration.

## File Plan (6 files)

### Combat/Movement (5)
- `Combat/Movement/MovementState.cs` — 3-state enum (Grounded, Airborne, Dashing)
- `Combat/Movement/MovementStateMachine.cs` — plain C# state machine with action permissions
- `Combat/Movement/MovementConfig.cs` — ScriptableObject with all tuning params
- `Combat/Movement/GroundDetector.cs` — Physics2D.OverlapBox ground check
- `Combat/Movement/CharacterMotor.cs` — Rigidbody2D motor with move/jump/dash

### Characters (1)
- `Characters/CharacterInputHandler.cs` — Unity Input System reader (InputActionReference)

## Acceptance Criteria
- [x] Horizontal movement via Rigidbody2D.velocity with acceleration/deceleration
- [x] Jump with coyote time and jump buffer (configurable durations)
- [x] Dash with i-frames, cooldown, directional input or facing fallback
- [x] MovementConfig ScriptableObject for per-character tuning
- [x] IBuffProvider integration for speed multipliers (SetBuffProvider injection)
- [x] No singletons — [SerializeField] injection throughout
- [x] No cross-pillar imports (Combat imports only Shared)
- [x] XML doc comments on all public members
- [x] Unity new Input System with InputActionReference fields
- [x] Plain C# MovementStateMachine (testable without Unity runtime)
- [x] Local C# events (Jumped, Dashed) for combat system wiring
- [x] Fall gravity multiplier for snappier game feel

## Architecture Notes
- CharacterMotor fires local `Action` events — CombatEventDispatcher (T038) will relay these as ICombatEvents
- Sprite flip uses `transform.localScale.x` (visual only, not physics)
- MovementStateMachine will expand with combat states (Attacking, Hitstun, Deflecting) in T003–T007
- GroundDetector uses gizmos for debug visualization in Scene view
