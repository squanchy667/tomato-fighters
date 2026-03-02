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
