# Changelog

## [Phase 1] — 2026-03-02

### Completed
- **T001: Shared Interfaces, Enums, and Data Structures** — 19 files
  - 6 interfaces: ICombatEvents (13 events), IBuffProvider (10 methods), IPathProvider (8 members), IDamageable (7 members), IAttacker (5 members), IRunProgressionEvents (9 events)
  - 9 enums: CharacterType, DamageType, DamageResponse, PathType, StatType, RitualFamily, RitualCategory, RitualTrigger, TelegraphType
  - 4 data files: CombatEventData (13 readonly structs), DamagePacket, RunEventData (7 structs), PlaceholderTypes
- **T002: CharacterController — movement, jump, dash with Rigidbody2D** — 6 files
  - CharacterMotor: Rigidbody2D physics, coyote time, jump buffer, dash with i-frames
  - MovementStateMachine: plain C# state machine (Grounded/Airborne/Dashing)
  - MovementConfig: ScriptableObject with all tuning params
  - GroundDetector: Physics2D.OverlapBox ground check
  - CharacterInputHandler: Unity new Input System (InputActionReference)
  - MovementState: 3-state enum

### Notes
- All event data uses readonly structs for zero-allocation events
- IBuffProvider integrated into CharacterMotor via SetBuffProvider() — no singletons
- MovementStateMachine will expand with combat states in T003–T007
- PlaceholderTypes created for OnHitEffect, OnTriggerEffect, PathAbility (fleshed out later)

## [Phase 1] — 2026-03-02 (Belt-Scroll Rework — DONE)

### Completed
- **T002: CharacterController — belt-scroll rework** — 5 code files + 3 editor/test files
  - CharacterMotor: rewritten for belt-scroll (XY ground plane, simulated jump height, sprite offset, no gravity)
  - MovementConfig: removed gravity fields, added `depthSpeed`, `jumpGravity`
  - GroundDetector: **deleted** — grounded = `jumpHeight <= 0`
  - CharacterInputHandler: passes full Vector2 (horizontal + depth)
  - MovementStateMachine: unchanged (Grounded/Airborne/Dashing still applies)
  - MovementState: unchanged

### Infrastructure Added
- Assembly definitions: TomatoFighters.Characters, .Editor, .Tests.EditMode
- PlayerPrefabCreator: editor menu script (gravity=0, shadow child, no GroundDetector)
- MovementTestSceneCreator: editor menu script (arena, walls, player, input wiring)
- 17 edit-mode unit tests for MovementStateMachine

### Design Decisions
- Belt-scroll movement model (Streets of Rage style): X=horizontal, Y=depth, jump=sprite offset
- No ground collider — grounded is purely `jumpHeight <= 0`
- Rigidbody2D.gravityScale = 0 always; jump arc simulated with manual `jumpVelocity` / `jumpGravity`
- Sprite child offset by jumpHeight; shadow child stays at feet
- Collider stays on ground plane during jumps (standard for genre)
- Default prefab character = Brutor (SPD 0.7, tankiest)
