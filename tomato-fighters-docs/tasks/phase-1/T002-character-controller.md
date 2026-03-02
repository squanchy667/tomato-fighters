# T002: CharacterController — Belt-Scroll Movement, Jump, Dash

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

Belt-scroll beat 'em up character movement using Rigidbody2D. Free movement on XY ground plane (left/right + depth), simulated jump height with sprite offset, dash with i-frames and cooldown. ScriptableObject config for per-character tuning. Unity new Input System integration.

## File Plan (7 files)

### Combat/Movement (4)
- `Combat/Movement/MovementState.cs` — 3-state enum (Grounded, Airborne, Dashing)
- `Combat/Movement/MovementStateMachine.cs` — plain C# state machine with action permissions
- `Combat/Movement/MovementConfig.cs` — ScriptableObject with all tuning params (belt-scroll: depthSpeed, jumpGravity)
- `Combat/Movement/CharacterMotor.cs` — Rigidbody2D motor with belt-scroll move/jump/dash

### Characters (1)
- `Characters/CharacterInputHandler.cs` — Unity Input System reader (InputActionReference), passes Vector2 movement

### Tests (1)
- `Tests/EditMode/Combat/Movement/MovementStateMachineTests.cs` — 17 unit tests for state machine

### Editor (1)
- `Editor/Prefabs/PlayerPrefabCreator.cs` — Menu script to create wired Player prefab with shadow sprite

## Acceptance Criteria
- [x] Belt-scroll ground movement: X = left/right, Y = depth (up/down on ground plane)
- [x] Simulated jump: manual `jumpHeight` variable with sprite offset, no physics gravity
- [x] Jump with coyote time and jump buffer (configurable durations)
- [x] Dash with i-frames, cooldown, directional input on XY ground plane
- [x] Shadow sprite child stays at feet during jump
- [x] Sprite child offset by `jumpHeight` on local Y
- [x] MovementConfig ScriptableObject for per-character tuning
- [x] IBuffProvider integration for speed multipliers (SetBuffProvider injection)
- [x] No singletons — [SerializeField] injection throughout
- [x] No cross-pillar imports (Combat imports only Shared)
- [x] XML doc comments on all public members
- [x] Unity new Input System with InputActionReference fields
- [x] Plain C# MovementStateMachine (testable without Unity runtime)
- [x] Local C# events (Jumped, Dashed, Landed) for combat system wiring
- [x] Player prefab with all components wired via editor script
- [x] Edit-mode unit tests for MovementStateMachine (17 tests)
- [x] Movement test scene with arena, walls, and input wiring

## Design Decisions

### DD-1: Belt-scroll movement model (not platformer)
**Decision:** Use belt-scroll (Streets of Rage style) instead of platformer (Hollow Knight style).
**Rationale:** Tomato Fighters is a 2D beat 'em up with free movement on a ground plane. Characters walk left/right and up/down (depth). Jump is a visual effect, not a physics interaction.

**Coordinate mapping:**
| Action | Axis | Mechanism |
|--------|------|-----------|
| Walk left/right | X | `rb.velocity.x` |
| Walk up/down (depth) | Y | `rb.velocity.y` |
| Jump | Visual Y offset | `spriteTransform.localPosition.y = jumpHeight` |

### DD-2: No ground collider — grounded is `jumpHeight <= 0`
**Decision:** Delete `GroundDetector.cs`. No physics ground check needed.
**Rationale:** In belt-scroll, there's no floor collider. The character is always on the ground plane unless jumping. Landing is determined by the simulated jump arc returning to height 0.

```csharp
public bool IsGrounded => jumpHeight <= 0f;
```

### DD-3: Rigidbody2D gravity = 0, jump arc simulated manually
**Decision:** Set `rb.gravityScale = 0`. Simulate jump gravity with manual `jumpVelocity` and `jumpGravity` fields.
**Rationale:** The Y axis is used for depth movement, not gravity. Jump is a visual sprite offset with a manually computed arc. Remove `defaultGravityScale` and `fallGravityMultiplier` from MovementConfig; replace with `jumpGravity` and `depthSpeed`.

### DD-4: Sprite offset for jump height (Option A — direct reference)
**Decision:** Motor holds `[SerializeField] Transform spriteTransform` and sets `localPosition.y = jumpHeight` directly each frame.
**Rationale:** Simpler than event-based. The motor already handles visual concerns (sprite flip). No event overhead for per-frame updates.

### DD-5: Prefab hierarchy with shadow sprite
**Decision:** Prefab structure:
```
Player (root) — Rigidbody2D, BoxCollider2D, CharacterMotor, CharacterInputHandler
  ├─ Sprite    — SpriteRenderer, offset by jumpHeight
  └─ Shadow    — SpriteRenderer (black, alpha 0.3), stays at (0,0,0)
```
**Rationale:** Root stays on ground plane for collisions. Sprite child visually lifts during jump. Shadow provides ground reference.

### DD-6: Collider stays on ground plane during jump
**Decision:** The player's BoxCollider2D remains at the root position even during jumps.
**Rationale:** Standard for belt-scroll games (Streets of Rage). Enemies can walk into the player's ground position while jumping. Air state is tracked via `JumpHeight` property for combat systems (air combos T037).

### DD-7: Assembly definitions for module boundaries
**Decision:** Added `.asmdef` files: TomatoFighters.Shared, TomatoFighters.Combat, TomatoFighters.Characters, TomatoFighters.Editor, TomatoFighters.Tests.EditMode.
**Rationale:** Enables proper test referencing, faster incremental compilation, and enforces pillar boundaries at compile time.

### DD-8: Default prefab character = Brutor
**Decision:** Player prefab defaults to Brutor (SPD 0.7, tankiest character).
**Rationale:** Slowest character makes movement easier to visually verify. Character-specific configs for Slasher/Mystica/Viper created later.

## Architecture Notes
- CharacterMotor fires local `Action` events — CombatEventDispatcher (T038) will relay these as ICombatEvents
- Sprite flip uses `transform.localScale.x` (visual only, not physics)
- MovementStateMachine will expand with combat states (Attacking, Hitstun, Deflecting) in T003–T007
- `JumpHeight` property exposed for combat system (air combos, juggle in T037)
- Sorting order by Y position (characters lower on screen render in front) is a separate concern — handled in T034 or a small utility
