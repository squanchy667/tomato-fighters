# TomatoFighters — Cross-Reference Map

> Docs repo ↔ Code repo navigation. Use this to jump between specs and implementation.

## Pillar Ownership

| Pillar | Owner | Code Path | Docs Guide |
|--------|-------|-----------|------------|
| Combat | Dev 1 | `Scripts/Combat/`, `Scripts/Characters/` | `developer/dev1-combat-guide.md` |
| Roguelite | Dev 2 | `Scripts/Roguelite/`, `Scripts/Paths/` | `developer/dev2-roguelite-guide.md` |
| World | Dev 3 | `Scripts/World/` | `developer/dev3-world-guide.md` |
| Shared | ALL | `Scripts/Shared/` | `architecture/interface-contracts.md` |

## Shared Interfaces → Docs

| Interface | Purpose | Docs |
|-----------|---------|------|
| `ICombatEvents` | Combat fires, Roguelite subscribes | `architecture/interface-contracts.md` |
| `IBuffProvider` | Roguelite provides, Combat queries | `architecture/interface-contracts.md` |
| `IPathProvider` | Roguelite provides, Combat+World query | `architecture/interface-contracts.md` |
| `IDamageable` | Combat defines, World implements | `architecture/interface-contracts.md` |
| `IAttacker` | Combat defines, World implements | `architecture/interface-contracts.md` |
| `IRunProgressionEvents` | World fires, Roguelite subscribes | `architecture/interface-contracts.md` |

## Phase 1 Tasks → Code Files

| Task | Spec (docs repo) | Implementation (code repo) |
|------|-------------------|---------------------------|
| T001 Shared Contracts | `tasks/phase-1/T001-shared-contracts.md` | `Scripts/Shared/Interfaces/*.cs`, `Scripts/Shared/Enums/*.cs`, `Scripts/Shared/Data/*.cs` |
| T002 CharacterController | `tasks/phase-1/T002-character-controller.md` | `Scripts/Combat/Movement/CharacterMotor.cs`, `MovementStateMachine.cs`, `MovementConfig.cs` |
| T003 ComboSystem | `tasks/phase-1/T003-combo-chain.md` | `Scripts/Combat/Combo/ComboController.cs`, `ComboStateMachine.cs`, `ComboDefinition.cs`, `ComboStep.cs` |
| T004 HitboxManager | `tasks/phase-1/T004-combo-chain.md` | `Scripts/Combat/` (pending) |
| T005 AttackData SO | `tasks/phase-1/T005-attack-data-so.md` | `Scripts/Shared/Data/AttackData.cs`, `ScriptableObjects/Attacks/` (pending) |
| T006 CharacterBaseStats | `tasks/phase-1/T006-character-base-stats.md` | `Scripts/Shared/Data/CharacterBaseStats.cs`, `ScriptableObjects/Characters/*.asset` |
| T007 StatCalculator | `tasks/phase-1/T007-stat-calculator.md` | `Scripts/Paths/CharacterStatCalculator.cs`, `FinalStats.cs`, `StatModifierInput.cs` |
| T008 PathData SO | `tasks/phase-1/T008-path-data-so.md` | `Scripts/Shared/Data/PathData.cs` (pending) |
| T009 CurrencyManager | `tasks/phase-1/T009-currency-manager.md` | `Scripts/Roguelite/CurrencyManager.cs` (pending) |
| T010 WaveManager | `tasks/phase-1/T010-wave-manager.md` | `Scripts/World/WaveManager.cs` (pending) |
| T011 EnemyBase | `tasks/phase-1/T011-enemy-base.md` | `Scripts/World/EnemyBase.cs` (pending) |
| T012 CameraController | `tasks/phase-1/T012-camera-controller.md` | `Scripts/World/CameraController2D.cs` (pending) |
| T013 Test Scene | `tasks/phase-1/T013-test-scene.md` | `Scenes/MovementTest.unity` |

## Animation Pipeline → Code Files

| Step | Menu Command | Editor Script | Output |
|------|-------------|---------------|--------|
| 1. Import sprites | `TomatoFighters > Import Sprite Sheets` | `Editor/Animation/SpriteSheetImporter.cs` | Sliced sprites in PNG sub-assets |
| 2. Build anims | `TomatoFighters > Build Animations` | `Editor/Animation/AnimationBuilder.cs` | `.anim` clips + `.controller` in `Animations/TomatoFighter/` |
| 3. Build prefab | `TomatoFighters > Create Player Prefab` | `Editor/Prefabs/PlayerPrefabCreator.cs` | `Prefabs/Player/Player.prefab` |
| 4. Build scene | `TomatoFighters > Create Movement Test Scene` | `Editor/Prefabs/MovementTestSceneCreator.cs` | `Scenes/MovementTest.unity` |

| Editor Support Script | Purpose |
|-----------------------|---------|
| `Editor/Animation/AnimationForgeMetadata.cs` | Shared metadata.json parser (Newtonsoft.Json, Dictionary-based) |

| Runtime Script | Purpose |
|----------------|---------|
| `Scripts/Combat/Animation/CharacterAnimationBridge.cs` | Feeds motor state (idle/walk/run) → Animator |
| `Scripts/Combat/Animation/TomatoFighterAnimatorParams.cs` | Animator parameter string constants |

### Adding New Animations (data-driven pipeline)

1. Drop new PNG into `Assets/animations/tomato_fighter_animations/Sprites/`
2. Add entry to `metadata.json` under `"animations"`: set `"loop": true` for locomotion, `"loop": false` for action
3. Run **Import Sprite Sheets** → **Build Animations** — no code changes needed
4. Locomotion anims auto-wire with Speed transitions; action anims get trigger parameters

| Source Data | Location |
|-------------|----------|
| Sprite sheets (PNG) | `Assets/animations/tomato_fighter_animations/Sprites/` |
| Animation Forge metadata | `Assets/animations/tomato_fighter_animations/metadata.json` |
| Controller scaffold (JSON) | `Assets/animations/tomato_fighter_animations/Animator/tomato_fighter_controller.json` |

## ScriptableObjects → Design Specs

| SO Asset | Code Definition | Design Source (docs repo) |
|----------|----------------|--------------------------|
| `Characters/BrutorStats.asset` | `Shared/Data/CharacterBaseStats.cs` | `design-specs/CHARACTER-ARCHETYPES.md` |
| `Characters/SlasherStats.asset` | `Shared/Data/CharacterBaseStats.cs` | `design-specs/CHARACTER-ARCHETYPES.md` |
| `Characters/MysticaStats.asset` | `Shared/Data/CharacterBaseStats.cs` | `design-specs/CHARACTER-ARCHETYPES.md` |
| `Characters/ViperStats.asset` | `Shared/Data/CharacterBaseStats.cs` | `design-specs/CHARACTER-ARCHETYPES.md` |
| `ComboDefinitions/Brutor_ComboDefinition.asset` | `Combat/Combo/ComboDefinition.cs` | `tasks/phase-1/T004-combo-chain.md` |
| `ComboDefinitions/Mystica_ComboDefinition.asset` | `Combat/Combo/ComboDefinition.cs` | Placeholder (L-L-L + H-H) |
| `MovementConfigs/Brutor_MovementConfig.asset` | `Combat/Movement/MovementConfig.cs` | `tasks/phase-1/T002-character-controller.md` |
| `MovementConfigs/Mystica_MovementConfig.asset` | `Combat/Movement/MovementConfig.cs` | SPD=1.0 mage tuning |

## Tests → Task Coverage

| Test File | Tests For | Task |
|-----------|-----------|------|
| `Tests/EditMode/Combat/Combo/ComboStateMachineTests.cs` | ComboStateMachine | T003 |
| `Tests/EditMode/Combat/Movement/MovementStateMachineTests.cs` | MovementStateMachine | T002 |

## Architecture Docs → Code Modules

| Doc (docs repo) | Code Modules |
|-----------------|-------------|
| `architecture/system-overview.md` | All `Scripts/` pillars |
| `architecture/interface-contracts.md` | `Scripts/Shared/Interfaces/*.cs` |
| `architecture/data-flow.md` | `Scripts/Shared/Data/*.cs`, `Scripts/Shared/Enums/*.cs` |
| `design-specs/CHARACTER-ARCHETYPES.md` | `ScriptableObjects/Characters/*.asset`, `Scripts/Shared/Data/CharacterBaseStats.cs` |
| `design-specs/PROJECT-TALAMH-CHARACTERIZATION.md` | Overall game design reference |
| `testing/test-plan.md` | `Tests/EditMode/**` |

## Path Shortcuts

All code paths below are relative to `tomato-fighters/unity/TomatoFighters/Assets/`.
All docs paths are relative to `tomato-fighters-docs/`.

| Shorthand | Full Path |
|-----------|-----------|
| `Scripts/` | `unity/TomatoFighters/Assets/Scripts/` |
| `ScriptableObjects/` | `unity/TomatoFighters/Assets/ScriptableObjects/` |
| `Scenes/` | `unity/TomatoFighters/Assets/Scenes/` |
| `Prefabs/` | `unity/TomatoFighters/Assets/Prefabs/` |
| `Tests/` | `unity/TomatoFighters/Assets/Tests/` |
| `Editor/` | `unity/TomatoFighters/Assets/Editor/` |
