# Context Dump: PlayMode Cancel Buffer Tests

**Date:** 2026-03-03
**Task:** Create PlayMode tests for cancel input buffering in ComboController
**Branch:** `tal`

---

## What to Create

### File 1: `Assets/Tests/PlayMode/TomatoFighters.Tests.PlayMode.asmdef`

Based on the EditMode asmdef at `Assets/Tests/EditMode/TomatoFighters.Tests.EditMode.asmdef`:

```json
{
    "name": "TomatoFighters.Tests.PlayMode",
    "rootNamespace": "TomatoFighters.Tests.PlayMode",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "TomatoFighters.Combat",
        "TomatoFighters.Shared"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Key difference from EditMode: `includePlatforms` is empty (NOT `["Editor"]`).

### File 2: `Assets/Tests/PlayMode/Combat/Combo/CancelBufferTests.cs`

11 `[UnityTest]` coroutine tests for the cancel buffer feature.

---

## Key APIs Under Test (from ComboController.cs)

```csharp
// Cancel buffer methods — these are what we test
public bool TryDashCancel(Vector2 direction)  // Returns true if immediate, false if buffered
public bool TryJumpCancel()                   // Returns true if immediate, false if buffered
public void OnHitConfirmed()                  // Calls stateMachine.OnHitConfirmed() then TryConsumeBufferedCancels()
public void ForceResetCombo()                 // Clears buffers + resets state machine

// Events to track
public event Action DashCancelTriggered;
public event Action JumpCancelTriggered;

// State
public bool CanDashCancel => stateMachine.CanDashCancel;
public bool CanJumpCancel => stateMachine.CanJumpCancel;
public ComboState CurrentState => stateMachine.CurrentState;
public bool IsComboActive => stateMachine.CurrentState != ComboState.Idle;

// Existing combo methods
public void RequestLightAttack()
public void RequestHeavyAttack()
public void OnComboWindowOpen()  // Animation event callback
public void OnFinisherEnd()      // Animation event callback

// SerializeField private fields (need reflection):
// - comboDefinition (ComboDefinition)
// - interactionConfig (ComboInteractionConfig)
// - motor (CharacterMotor)
// - cancelBufferWindow (float, default 0.15f)
// - animator (Animator) — can be null, just logs error
// - characterType (CharacterType)
```

### TryConsumeBufferedCancels() internal logic (private):
```csharp
private void TryConsumeBufferedCancels()
{
    float now = Time.time;
    bool dashBuffered = lastDashCancelTime >= 0f && (now - lastDashCancelTime) <= cancelBufferWindow;
    bool jumpBuffered = lastJumpCancelTime >= 0f && (now - lastJumpCancelTime) <= cancelBufferWindow;
    // Priority from interactionConfig.cancelPriority (DashOverJump or JumpOverDash)
    // If both buffered, pick one based on priority
    // Then attempt RequestDashCancel() or RequestJumpCancel()
}
```

Buffer timestamps use `Time.time` — this is WHY we need PlayMode tests (Time.time doesn't advance in EditMode).

---

## Dependencies

### CharacterMotor (Assets/Scripts/Combat/Movement/CharacterMotor.cs)
- Has `[RequireComponent(typeof(Rigidbody2D))]` — auto-adds Rigidbody2D
- Needs `MovementConfig` SO via `[SerializeField] private MovementConfig config`
- Methods called by ComboController: `RequestDash(Vector2)`, `RequestJump()`, `SetAttackLock(bool)`

### ComboInteractionConfig (Assets/Scripts/Combat/Combo/ComboInteractionConfig.cs)
```csharp
public class ComboInteractionConfig : ScriptableObject
{
    public CancelPriority cancelPriority = CancelPriority.DashOverJump;
    public bool dashCancelResetsCombo = true;
    public bool jumpCancelResetsCombo;
    public bool resetOnStagger = true;
    public bool resetOnDeath = true;
    public bool lockMovementDuringAttack = true;
    public bool lockMovementDuringFinisher = true;
}

public enum CancelPriority { DashOverJump, JumpOverDash }
```

### ComboDefinition (Assets/Scripts/Combat/Combo/ComboDefinition.cs)
ScriptableObject with:
- `ComboStep[] steps`
- `int rootLightIndex`, `int rootHeavyIndex`
- `float defaultComboWindow = 0.3f`

### ComboStep (struct)
```csharp
public struct ComboStep {
    public AttackType attackType;
    public string animationTrigger;
    public float damageMultiplier;
    public float comboWindowDuration;  // 0 = use default
    public int nextOnLight;   // -1 = no branch
    public int nextOnHeavy;   // -1 = no branch
    public bool canDashCancelOnHit;
    public bool canJumpCancelOnHit;
    public bool isFinisher;
}
```

### MovementConfig (Assets/Scripts/Combat/Movement/MovementConfig.cs)
ScriptableObject with sane defaults — just `CreateInstance<MovementConfig>()` works fine.

---

## Test Combo Definition (reuse from ComboStateMachineTests)

7-step tree:
```
[0] L1 — canDashCancelOnHit=false, canJumpCancelOnHit=false, nextL=1, nextH=3
[1] L2 — canDashCancelOnHit=true, canJumpCancelOnHit=true, nextL=2, nextH=4
[2] L3 finisher (sweep)
[3] H (launcher) — nextL=-1, nextH=5
[4] H finisher (slam)
[5] H finisher (heavy ender)
[6] H1 (root heavy) — comboWindowDuration=0.5, nextL=-1, nextH=5
```

rootLightIndex=0, rootHeavyIndex=6, defaultComboWindow=0.4f

**Step 1 (index 1) is the key test step** — it has both `canDashCancelOnHit=true` and `canJumpCancelOnHit=true`.

---

## Setup Pattern

PlayMode tests need real GameObjects. To avoid Awake running before fields are set:

```csharp
// 1. Create GO inactive
var go = new GameObject("TestPlayer");
go.SetActive(false);

// 2. Add components (CharacterMotor auto-adds Rigidbody2D via RequireComponent)
var motor = go.AddComponent<CharacterMotor>();
var controller = go.AddComponent<ComboController>();

// 3. Create SOs
var comboDef = ScriptableObject.CreateInstance<ComboDefinition>();
// ... populate steps ...
var interactionConfig = ScriptableObject.CreateInstance<ComboInteractionConfig>();
var movementConfig = ScriptableObject.CreateInstance<MovementConfig>();

// 4. Set private [SerializeField] fields via reflection
SetPrivateField(controller, "comboDefinition", comboDef);
SetPrivateField(controller, "interactionConfig", interactionConfig);
SetPrivateField(controller, "motor", motor);
SetPrivateField(motor, "config", movementConfig);
// Don't set animator — it's fine as null (just logs error)

// 5. Activate → Awake fires with correct deps
go.SetActive(true);

// 6. Wait one frame
yield return null;
```

Reflection helper:
```csharp
private static void SetPrivateField(object target, string fieldName, object value)
{
    var field = target.GetType().GetField(fieldName,
        BindingFlags.NonPublic | BindingFlags.Instance);
    field.SetValue(target, value);
}
```

---

## AdvanceToStep1() Helper

Gets combo to step 1 (which has both cancel flags enabled):

```csharp
private void AdvanceToStep1()
{
    controller.RequestLightAttack();   // → step 0 (Attacking)
    controller.OnComboWindowOpen();    // → ComboWindow
    controller.RequestLightAttack();   // → step 1 (Attacking, both cancels available on hit)
}
```

---

## 11 Test Cases

| # | Name | Key Flow |
|---|------|----------|
| 1 | `TryDashCancel_Immediate_WhenWindowOpen` | AdvanceToStep1 → OnHitConfirmed → TryDashCancel → returns true, DashCancelTriggered fires |
| 2 | `TryDashCancel_Buffers_WhenWindowClosed` | AdvanceToStep1 (no hit confirm) → TryDashCancel → returns false, combo still active |
| 3 | `TryJumpCancel_Immediate_WhenWindowOpen` | Same as #1 for jump |
| 4 | `TryJumpCancel_Buffers_WhenWindowClosed` | Same as #2 for jump |
| 5 | `BufferedDash_ConsumedOnHitConfirm` | AdvanceToStep1 → TryDashCancel (buffers) → OnHitConfirmed → DashCancelTriggered fires, state=Idle |
| 6 | `BufferedJump_ConsumedOnHitConfirm` | AdvanceToStep1 → TryJumpCancel (buffers) → OnHitConfirmed → JumpCancelTriggered fires, state=Idle |
| 7 | `BufferedCancel_Expires_AfterWindow` | AdvanceToStep1 → TryDashCancel → yield WaitForSeconds(0.2f) → OnHitConfirmed → cancel NOT consumed, combo stays |
| 8 | `Priority_DashOverJump_Default` | AdvanceToStep1 → TryDashCancel + TryJumpCancel → OnHitConfirmed → only DashCancelTriggered fires |
| 9 | `Priority_JumpOverDash_WhenConfigured` | Set cancelPriority=JumpOverDash → AdvanceToStep1 → buffer both → OnHitConfirmed → only JumpCancelTriggered fires |
| 10 | `Buffers_ClearedOnForceReset` | AdvanceToStep1 → TryDashCancel → ForceResetCombo → new combo to step1 → OnHitConfirmed → no cancel fires |
| 11 | `Buffers_ClearedOnComboDrop` | AdvanceToStep1 → TryDashCancel → OnComboWindowOpen → wait for window expiry (0.5s) → new combo to step1 → OnHitConfirmed → no cancel fires |

---

## TearDown

Destroy GO and all SOs in TearDown:
```csharp
Object.Destroy(go);
Object.DestroyImmediate(comboDef);
Object.DestroyImmediate(interactionConfig);
Object.DestroyImmediate(movementConfig);
```

---

## File Paths Summary

| File | Path |
|------|------|
| New asmdef | `unity/TomatoFighters/Assets/Tests/PlayMode/TomatoFighters.Tests.PlayMode.asmdef` |
| New test | `unity/TomatoFighters/Assets/Tests/PlayMode/Combat/Combo/CancelBufferTests.cs` |
| Class under test | `unity/TomatoFighters/Assets/Scripts/Combat/Combo/ComboController.cs` |
| ComboStateMachine | `unity/TomatoFighters/Assets/Scripts/Combat/Combo/ComboStateMachine.cs` |
| ComboDefinition | `unity/TomatoFighters/Assets/Scripts/Combat/Combo/ComboDefinition.cs` |
| ComboStep | `unity/TomatoFighters/Assets/Scripts/Combat/Combo/ComboStep.cs` |
| ComboInteractionConfig | `unity/TomatoFighters/Assets/Scripts/Combat/Combo/ComboInteractionConfig.cs` |
| CharacterMotor | `unity/TomatoFighters/Assets/Scripts/Combat/Movement/CharacterMotor.cs` |
| MovementConfig | `unity/TomatoFighters/Assets/Scripts/Combat/Movement/MovementConfig.cs` |
| Reference tests | `unity/TomatoFighters/Assets/Tests/EditMode/Combat/Combo/ComboStateMachineTests.cs` |
| Reference asmdef | `unity/TomatoFighters/Assets/Tests/EditMode/TomatoFighters.Tests.EditMode.asmdef` |

---

## Pending Task List Items (from before dump)

These task list items were created but not started:
1. Create PlayMode test assembly definition
2. Write CancelBufferTests.cs with all 11 tests
