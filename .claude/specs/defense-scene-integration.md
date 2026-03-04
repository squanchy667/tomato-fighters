# Defense System × Test Scene Integration

**Date:** 2026-03-04
**Status:** Spec finalized, ready for execution
**Branch:** `tal`

## Summary

Integrate the defense system (T016) into the test scene by:
1. Fixing MysticaCharacterCreator to include DefenseConfig
2. Creating BrutorCharacterCreator and ViperCharacterCreator (full character creators)
3. Adding floating defense debug UI to the test scene

After this, running any character creator produces a complete prefab with DefenseSystem wired.

## Design Decisions

### DD-1: Every character creator is self-contained
Running any character creator always produces a complete, up-to-date prefab with DefenseConfig wired. No separate "patch" scripts needed.

### DD-2: Load existing SOs, don't recreate
Brutor/Viper creators use `CreateOrLoad` pattern — load existing ComboDefinitions, AttackData, MovementConfig rather than recreating them. Only create what's missing (DefenseConfig, DefenseBonus, MovementConfig for Viper).

### DD-3: Viper needs Viper_MovementConfig
No MovementConfig exists for Viper yet. Creator will create one with SPD 1.1 values:
- moveSpeed: 8.8f, groundAcceleration: 50, airAcceleration: 25
- jumpForce: 13, dashSpeed: 18, dashDuration: 0.15, dashCooldown: 0.5
- defaultGravityScale: 3.5, fallGravityMultiplier: 5.0

### DD-4: Defense timing tuned per archetype
| Param | Brutor (Tank) | Mystica (Mage) | Viper (Range) | Slasher (existing) |
|-------|--------------|----------------|---------------|-------------------|
| deflectWindowDuration | 0.20 | 0.10 | 0.14 | 0.12 |
| clashWindowStart | 0.01 | 0.03 | 0.02 | 0.015 |
| clashWindowEnd | 0.10 | 0.05 | 0.07 | 0.06 |
| dodgeIFrameStart | 0.08 | 0.03 | 0.04 | 0.04 |
| dodgeIFrameEnd | 0.20 | 0.35 | 0.30 | 0.25 |

Rationale: Brutor gets widest deflect/clash (face-tanking), shortest dodge (big body). Mystica gets widest dodge (primary survival), tightest deflect/clash. Viper is balanced-evasive.

### DD-5: Floating debug UI uses world-space TextMesh
No Canvas dependency. TextMesh spawned at player position, floats up and fades over 1 second. Color-coded: green=deflect, yellow=clash, cyan=dodge.

## File Plan

### 1. Edit: `Assets/Editor/Characters/MysticaCharacterCreator.cs`
- Add constants: `DEFENSE_CONFIG_FOLDER`, `MYSTICA_DEFENSE_PATH`
- Add `CreateOrLoadMysticaDefenseConfig()` method (mirrors Slasher pattern)
  - deflectWindowDuration: 0.10f
  - clashWindowStart: 0.03f, clashWindowEnd: 0.05f
  - dodgeIFrameStart: 0.03f, dodgeIFrameEnd: 0.35f
  - Creates/finds MysticaDefenseBonus.asset
- Wire `defenseConfig` into `CharacterPrefabConfig`

### 2. Create: `Assets/Editor/Characters/BrutorCharacterCreator.cs`
Menu: `TomatoFighters > Characters > Create Brutor`

Loads existing:
- `Brutor_ComboDefinition.asset` (7 steps)
- `Brutor_MovementConfig.asset`
- AttackData SOs: BrutorShieldBash1, BrutorShieldBash2, BrutorSweep, BrutorLauncher, BrutorLauncherSlam, BrutorOverheadSlam, BrutorGroundPound

Creates:
- `Brutor_DefenseConfig.asset` + `BrutorDefenseBonus.asset`
- `Brutor.prefab`

Hitbox definitions:
- `Jab`: Box 0.8x0.6, offset (0.5, 0.5) — forward punch
- `Sweep`: Box 1.3x0.5, offset (0.4, 0.2) — wide low arc
- `Uppercut`: Box 0.7x1.0, offset (0.4, 0.8) — vertical
- `Slam`: Box 1.2x0.8, offset (0.3, 0.3) — overhead wide

Config: baseAttack=14f, useTimerFallback=true, fallbackActiveDuration=0.35f, CharacterType.Brutor

Combo step → AttackData wiring:
```
0: BrutorShieldBash1  → Jab
1: BrutorShieldBash2  → Jab
2: BrutorSweep        → Sweep
3: BrutorLauncher     → Uppercut
4: BrutorLauncherSlam → Slam
5: BrutorOverheadSlam → Slam
6: BrutorGroundPound  → Slam
```

### 3. Create: `Assets/Editor/Characters/ViperCharacterCreator.cs`
Menu: `TomatoFighters > Characters > Create Viper`

Loads existing:
- `Viper_ComboDefinition.asset` (6 steps)
- AttackData SOs: ViperShot1, ViperShot2, ViperRapidBurst, ViperQuickCharged, ViperChargedShot, ViperPiercingShot

Creates:
- `Viper_MovementConfig.asset` (moveSpeed=8.8f, dashSpeed=18f, dashCooldown=0.5f)
- `Viper_DefenseConfig.asset` + `ViperDefenseBonus.asset`
- `Viper.prefab`

Hitbox definitions:
- `Lunge`: Box 1.8x0.35, offset (1.0, 0.5) — long narrow ranged reach
- `Sweep`: Box 1.0x0.6, offset (0.5, 0.4) — short wide burst

Config: baseAttack=18f, useTimerFallback=true, fallbackActiveDuration=0.2f, CharacterType.Viper, lockMovementDuringAttack=false (mobile ranged)

Combo step → AttackData wiring:
```
0: ViperShot1         → Lunge
1: ViperShot2         → Lunge
2: ViperRapidBurst    → Sweep
3: ViperQuickCharged  → Lunge
4: ViperChargedShot   → Lunge
5: ViperPiercingShot  → Lunge
```

### 4. Create: `Assets/Scripts/Combat/Debug/DefenseDebugUI.cs`
MonoBehaviour that:
- Takes `[SerializeField] DefenseSystem defenseSystem`
- Subscribes to OnDeflect, OnClash, OnDodge events
- Spawns world-space TextMesh at player position on each event
- Text floats up (0.5 units over 1 second) and fades alpha to 0
- Color coding: green=DEFLECTED!, yellow=CLASHED!, cyan=DODGED!
- Auto-destroys text after fade completes
- Shows small persistent label with current defense state (None/Dashing/HeavyStartup)

### 5. Edit: `Assets/Editor/Prefabs/MovementTestSceneCreator.cs`
- Add `DefenseDebugUI` component to the player instance
- Wire its `defenseSystem` reference via `SerializedObject`/`SerializedProperty`

## Execution Order

1. `MysticaCharacterCreator.cs` (edit)
2. `BrutorCharacterCreator.cs` (create)
3. `ViperCharacterCreator.cs` (create)
4. `DefenseDebugUI.cs` (create)
5. `MovementTestSceneCreator.cs` (edit)

## Post-Code Steps (In Unity)

1. `TomatoFighters > Characters > Create Brutor` → Brutor.prefab + DefenseConfig
2. `TomatoFighters > Characters > Create Mystica` → Updated Mystica.prefab with DefenseConfig
3. `TomatoFighters > Characters > Create Viper` → Viper.prefab + DefenseConfig + MovementConfig
4. `TomatoFighters > Create Movement Test Scene` → Regenerated scene with debug UI

## Dependencies

- T016 (DefenseSystem) — DONE
- T015 (DamagePipeline) — DONE
- Existing SOs: all ComboDefinitions, AttackData, CharacterStats — DONE
- Missing: Viper_MovementConfig (created by ViperCharacterCreator)

## Risks

- **Animator controllers**: Brutor and Viper may not have animator controllers at `Assets/Animations/{Name}/{Name}_Controller.controller`. Creators should handle this gracefully (skip if missing, log warning).
- **Input actions**: The existing InputActionAsset must be loadable. If not found, log warning and skip wiring.
- **Hitbox shapes are estimated**: Brutor/Viper hitbox dimensions are based on attack descriptions. May need tuning in Unity.
