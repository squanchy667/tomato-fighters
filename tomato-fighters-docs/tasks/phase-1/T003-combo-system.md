# T003: ComboSystem — Light/Heavy Chains, Finishers

| Field | Value |
|-------|-------|
| **Task ID** | T003 |
| **Phase** | 1 — Foundation |
| **Owner** | Dev 1 (Combat) |
| **Agent** | combat-agent |
| **Status** | DONE |
| **Completed** | 2026-03-02 |
| **Depends On** | T001 |
| **Blocks** | T005, T006, T007, T017, T053 |

## Description

Combo input and chain-tracking system for beat 'em up combat. Players press light and heavy attack buttons; the system chains sequential hits into combos with timing windows, input buffering, and finishers at the end of chains. Each character archetype gets a ScriptableObject-defined combo set with different chain lengths, branching paths, and timing.

## Design Decisions

**DD-1: Branching tree combo structure**
Combo chains use a branching tree — inputs can branch (L→L→H = launcher, L→L→L = sweep). Each ComboStep has `nextOnLight` and `nextOnHeavy` index pointers. More expressive than linear chains, matches the "deep combat" vision.

**DD-2: Flat array with index pointers**
ComboDefinition stores all steps in a single `ComboStep[]` array. Each step references the next step by array index (-1 = no branch). Simple to author in the inspector, avoids nested SO proliferation.

**DD-3: Local C# events for motor communication**
ComboController fires `AttackStarted`, `ComboDropped`, `FinisherStarted`, `ComboEnded` events. CharacterMotor subscribes to lock/unlock movement. Matches the event pattern from T002 (Jumped, Dashed). Allows T006/T007 to subscribe without ComboController knowing about them.

**DD-4: Plain C# state machine with Tick(dt)**
ComboStateMachine is a plain C# class. Combo window timer ticks via `Tick(deltaTime)` called from ComboController.Update(). All other transitions are animation-event-driven. Keeps combo logic testable without Unity runtime.

## Combo States

| State | Description | Movement? | Input? |
|-------|-------------|-----------|--------|
| Idle | Not attacking. Combo reset. | Yes | Starts new chain |
| Attacking | Attack animation playing. Committed. | No | Buffered |
| ComboWindow | Window after attack for chaining. | Limited | Consumed → next step |
| Finisher | Finisher animation. Locked. | No | Ignored |

## File Plan (7 files)

### Combat/Combo (6 new)
- `Combat/Combo/AttackType.cs` — enum: Light, Heavy
- `Combat/Combo/ComboState.cs` — enum: Idle, Attacking, ComboWindow, Finisher
- `Combat/Combo/ComboStep.cs` — serializable struct: attack type, animation trigger, damage multiplier, branching indices, finisher flag
- `Combat/Combo/ComboDefinition.cs` — ScriptableObject: flat step array, root indices, default combo window
- `Combat/Combo/ComboStateMachine.cs` — plain C# class: state tracking, input buffering, combo window timer, animation event callbacks
- `Combat/Combo/ComboController.cs` — MonoBehaviour: wires input → state machine → animation → events

### Characters (1 modified)
- `Characters/CharacterInputHandler.cs` — add lightAttackAction, heavyAttackAction InputActionReferences

## Acceptance Criteria
- [x] Branching combo tree with light/heavy paths per step
- [x] Input buffering during attack animations
- [x] Combo window timer with configurable duration per step
- [x] Animation-event-driven transitions (OnComboWindowOpen, OnFinisherEnd)
- [x] Finisher detection when chain reaches terminal step
- [x] Local C# events: AttackStarted, ComboDropped, FinisherStarted, ComboEnded
- [x] ComboDefinition ScriptableObject with flat step array
- [x] Plain C# ComboStateMachine (testable without Unity runtime)
- [x] CharacterInputHandler updated with attack input actions
- [x] No singletons — [SerializeField] injection throughout
- [x] No cross-pillar imports (Combat imports only Shared)
- [x] XML doc comments on all public members
