# Clash Cancellation — Mutual No-Damage + Reciprocal Immunity

**Date:** 2026-03-04
**Status:** Spec finalized, ready for execution
**Branch:** `tal`

## Summary

Change clash behavior from 50% reduced damage to **0% mutual cancel**. When attack A clashes with entity B:
1. A deals **0 damage** to B (was 50%)
2. B's subsequent attack **skips A entirely** (reciprocal immunity)
3. A's attack can still hit other targets C, D (per-target cancellation)
4. Clash events (`OnClash`) and character bonuses still fire normally

## Design Decisions

### DD-1: New `ClashTracker` component in Shared pillar
A lightweight `MonoBehaviour` on each entity root that tracks which `IDamageable` targets are immune to this entity's current attack. Follows the same `HashSet` pattern as `HitboxDamage._hitThisActivation`.

**Rationale:** Both Combat (`HitboxManager`) and World (`TestDummyEnemy`) need to read/write clash immunity. Shared pillar is the only valid location. Keeps clash logic separate from the pure collision detection layer (`HitboxDamage`).

### DD-2: Immunity is per-target, not full hitbox deactivation
When A clashes with B, only B is marked immune on A's `ClashTracker`. A's hitbox stays active and can still hit C normally.

**Rationale:** User requirement. A clash between two entities should not protect bystanders.

### DD-3: Immunity clears on next attack activation
`ClearImmunities()` is called when a new attack cycle begins (player: `ActivateHitbox()`, enemy: `PerformAttack()`). Prevents stale immunity leaking across attacks.

### DD-4: Clash events and bonuses still fire
`NotifyTargetDefense()` is still called with `DamageResponse.Clashed`. `DefenseSystem.ProcessDefenseResult()` still invokes `OnClash` and applies character-specific `defenseBonus`. A clash is a successful defensive action that earns rewards — just no chip damage.

### DD-5: `OnHitConfirmed` still fires on clash
The attack connected (even though 0 damage). Combo cancel windows should still open.

### DD-6: Reciprocal immunity early-return skips everything
When the immunity check triggers, the hit handler returns immediately — no `ResolveIncoming`, no damage, no events. The entity is invisible to that hitbox for this activation. This is correct because the clash already fired events on the first resolution.

## Sequence Diagram

### Case A: Player hits enemy first

```
1. Player hitbox fires → OnTriggerEnter2D → hits Enemy B
2. HitboxManager.HandleHitDetected(B):
   a. ownerClashTracker.HasClashImmunity(B)? → No
   b. B.ResolveIncoming() → Clashed (B in HeavyStartup, facing player)
   c. 0% damage (no TakeDamage call)
   d. NotifyTargetDefense → OnClash fires, bonuses apply
   e. B.ClashTracker.AddClashImmunity(playerDamageable) ← KEY
   f. comboController.OnHitConfirmed()

... B's clash window closes, B's hitbox activates ...

3. Enemy hitbox fires → OnTriggerEnter2D → hits Player A
4. TestDummyEnemy.HandleHitDetected(A):
   a. clashTracker.HasClashImmunity(A)? → YES
   b. RETURN (skip entirely — no damage, no events)

5. Enemy hitbox fires → hits Player C (hypothetical)
6. TestDummyEnemy.HandleHitDetected(C):
   a. clashTracker.HasClashImmunity(C)? → No
   b. Normal resolution → damage applied
```

### Case B: Enemy hits player first

```
1. Enemy hitbox fires → hits Player A
2. TestDummyEnemy.HandleHitDetected(A):
   a. clashTracker.HasClashImmunity(A)? → No
   b. A.ResolveIncoming() → Clashed (A in HeavyStartup)
   c. 0% damage
   d. Player.ClashTracker.AddClashImmunity(enemyDamageable) ← KEY

... Player's hitbox activates ...

3. Player hitbox fires → hits Enemy B
4. HitboxManager.HandleHitDetected(B):
   a. ownerClashTracker.HasClashImmunity(B)? → YES
   b. RETURN (skip entirely)
```

## File Plan

### 1. Create: `Assets/Scripts/Shared/Components/ClashTracker.cs`

```csharp
using System.Collections.Generic;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Shared.Components
{
    /// <summary>
    /// Tracks per-activation clash immunity. When entity A clashes with entity B,
    /// B registers A as immune on its own ClashTracker. When B's hitbox later fires,
    /// B's hit resolver checks HasClashImmunity(A) and skips the hit.
    /// </summary>
    public class ClashTracker : MonoBehaviour
    {
        private readonly HashSet<IDamageable> _clashImmune = new();

        /// <summary>
        /// Register a target as immune to this entity's current attack due to a clash.
        /// </summary>
        public void AddClashImmunity(IDamageable target) => _clashImmune.Add(target);

        /// <summary>
        /// Check if a target has clash immunity against this entity's attack.
        /// </summary>
        public bool HasClashImmunity(IDamageable target) => _clashImmune.Contains(target);

        /// <summary>
        /// Clear all clash immunities. Call when a new attack cycle begins.
        /// </summary>
        public void ClearImmunities() => _clashImmune.Clear();
    }
}
```

### 2. Edit: `Assets/Scripts/Combat/Hitbox/HitboxManager.cs`

Add fields:
- `[SerializeField] private ClashTracker ownerClashTracker;`
- `private IDamageable _ownerDamageable;` (cached in `Awake()` via `GetComponentInParent<IDamageable>()`)

Changes to `Awake()`:
- Add: `_ownerDamageable = GetComponentInParent<IDamageable>();`

Changes to `HandleHitDetected()`:
- **Top of method (after entry log):** Add immunity early-return check
  ```csharp
  // Clash immunity: skip targets that were already part of a clash resolution
  if (ownerClashTracker != null && ownerClashTracker.HasClashImmunity(target))
  {
      Debug.Log($"[HitboxManager] Skipping {target} — clash immunity active");
      return;
  }
  ```
- **`case DamageResponse.Clashed:`** — Replace entire case body. Remove `target.TakeDamage(clashPacket)`. Keep `NotifyTargetDefense`. Add reciprocal immunity registration:
  ```csharp
  case DamageResponse.Clashed:
      // No damage on clash — mutual cancel
      NotifyTargetDefense(target, response, packet, characterType);
      // Register reciprocal immunity: target's future hitbox should skip this entity
      if (_ownerDamageable != null && target is MonoBehaviour tmb)
      {
          var targetTracker = tmb.GetComponentInChildren<ClashTracker>();
          targetTracker?.AddClashImmunity(_ownerDamageable);
      }
      break;
  ```

Changes to `ActivateHitbox()`:
- Add `ownerClashTracker?.ClearImmunities();` at the top (first line, before `DeactivateActiveHitbox()`)

### 3. Edit: `Assets/Scripts/World/TestDummyEnemy.cs`

Add field:
- `[Header("Test Dummy — Clash")]`
- `[SerializeField] private ClashTracker clashTracker;`

Changes to `HandleHitDetected()`:
- **Top of method (after attackData null check):** Add immunity early-return check
  ```csharp
  // Clash immunity: skip targets that were already part of a clash resolution
  if (clashTracker != null && clashTracker.HasClashImmunity(target))
  {
      Debug.Log($"[TestDummyEnemy] Skipping {target} — clash immunity active");
      return;
  }
  ```
- **`case DamageResponse.Clashed:`** — Replace entire case body. Remove `target.TakeDamage(clashPacket)`. Add reciprocal immunity:
  ```csharp
  case DamageResponse.Clashed:
      // No damage on clash — mutual cancel
      // Register reciprocal immunity: target's future hitbox should skip this entity
      if (target is MonoBehaviour tmb)
      {
          var targetTracker = tmb.GetComponentInChildren<ClashTracker>();
          targetTracker?.AddClashImmunity(this); // EnemyBase : IDamageable
      }
      break;
  ```

Changes to `PerformAttack()`:
- Add `clashTracker?.ClearImmunities();` at the top (after the existing null checks for attackData and hitbox)

### 4. Edit: `Assets/Editor/Prefabs/PlayerPrefabCreator.cs`

- After adding `HitboxManager`, also add `ClashTracker` component to player root
- Wire `HitboxManager.ownerClashTracker` → the new `ClashTracker` via SerializedObject
- Pattern: follow how `ownerDefenseSystem` is already wired

### 5. Edit: `Assets/Editor/Prefabs/TestDummyPrefabCreator.cs`

- After adding `TestDummyEnemy`, also add `ClashTracker` component to TestDummy root
- Wire `TestDummyEnemy.clashTracker` → the new `ClashTracker` via SerializedObject

### 6. Edit: `../tomato-fighters-docs/developer/defense-timing-reference.md`

Update Comparison table row for "Damage on success":
```
| **Damage on success** | 0% | 0% (mutual cancel) | 0% |
```

Add new section after "Comparison" (before "Current Timing Values"):

```markdown
## Clash Cancellation

When an attack resolves as `Clashed`, both sides' attacks are cancelled **against each other**:

- The attacking hitbox deals **0 damage** to the clashing defender (no `TakeDamage` call)
- The defender's future hitbox **skips the attacker** entirely (reciprocal immunity via `ClashTracker`)
- Other targets (bystanders) are **not affected** — both hitboxes can still hit them normally

### How it works

Each entity has a `ClashTracker` component that maintains a `HashSet<IDamageable>` of immune targets. When hit resolver A detects a clash against target B:

1. A applies 0% damage to B
2. A registers itself as immune on B's `ClashTracker`
3. When B's hitbox fires and B's resolver runs, it checks `ClashTracker.HasClashImmunity(A)` → skip

Immunities clear at the start of each new attack activation, preventing carry-over.

### Events still fire

Clash events (`OnClash`) and character-specific defense bonuses still activate on the first resolution. The reciprocal skip (step 3) fires no events — the clash was already fully processed in step 1.
```

Also add `ClashTracker` to the Source Files table:
```
| `Scripts/Shared/Components/ClashTracker.cs` | Per-entity clash immunity tracking |
```

## Execution Order

1. `ClashTracker.cs` (create — no dependencies)
2. `HitboxManager.cs` (edit — depends on ClashTracker)
3. `TestDummyEnemy.cs` (edit — depends on ClashTracker)
4. `PlayerPrefabCreator.cs` (edit — wire ClashTracker)
5. `TestDummyPrefabCreator.cs` (edit — wire ClashTracker)
6. `defense-timing-reference.md` (edit — documentation sync)

## Post-Code Steps (In Unity)

1. `TomatoFighters > Characters > Create Slasher` → Regenerate with ClashTracker
2. `TomatoFighters > Prefabs > Create TestDummy` → Regenerate with ClashTracker
3. `TomatoFighters > Create Movement Test Scene` → Regenerate scene
4. Test: player heavy attack during enemy telegraph → 0 damage both ways
5. Test: player normal attack on non-clashing enemy → full damage

## Dependencies

- T016 (DefenseSystem) — DONE
- T015 (DamagePipeline / HitboxManager) — DONE
- Existing `HitboxDamage`, `PlayerPrefabCreator`, `TestDummyPrefabCreator` — DONE

## Risks

- **`GetComponentInChildren<ClashTracker>()`** at resolve time: O(n) on hierarchy, but clashes are rare events. Acceptable.
- **Entities without ClashTracker**: Null checks degrade gracefully — clash still resolves as 0% damage, just no reciprocal immunity. Fine for non-attacking entities (destructibles).
- **Both hitboxes active simultaneously** (misconfigured timings): First-to-resolve wins and registers immunity. Second hit is skipped. Correct behavior.
- **`OnHitConfirmed` on immune skip**: The early-return in step 2/3 skips `OnHitConfirmed()`. This is correct — the immune entity was already "hit confirmed" during the original clash.
