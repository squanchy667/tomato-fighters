# T001: Shared Interfaces, Enums, and Data Structures

| Field | Value |
|-------|-------|
| **Task ID** | T001 |
| **Phase** | 1 — Foundation |
| **Owner** | ALL |
| **Agent** | shared-contracts |
| **Status** | DONE |
| **Completed** | 2026-03-02 |
| **Depends On** | — |
| **Blocks** | T002–T013 |

## Description

Define all shared interfaces, enums, and data structures that pillars use to communicate. This is the foundation layer — everything goes through `Scripts/Shared/`.

## File Plan (19 files)

### Interfaces (6)
- `Shared/Interfaces/ICombatEvents.cs` — 13 combat event signatures
- `Shared/Interfaces/IBuffProvider.cs` — 10 buff query methods
- `Shared/Interfaces/IPathProvider.cs` — 8 path state members
- `Shared/Interfaces/IDamageable.cs` — 7 damage processing members
- `Shared/Interfaces/IAttacker.cs` — 5 attacker state members
- `Shared/Interfaces/IRunProgressionEvents.cs` — 9 run progression events

### Enums (9)
- `Shared/Enums/CharacterType.cs` — Brutor, Slasher, Mystica, Viper
- `Shared/Enums/DamageType.cs` — Physical + 8 elemental
- `Shared/Enums/DamageResponse.cs` — Hit, Deflected, Clashed, Dodged
- `Shared/Enums/PathType.cs` — 12 paths (3 per character)
- `Shared/Enums/StatType.cs` — 10 stat types
- `Shared/Enums/RitualFamily.cs` — 8 elemental families
- `Shared/Enums/RitualCategory.cs` — Core, General, Enhancement, Twin
- `Shared/Enums/RitualTrigger.cs` — 11 combat triggers
- `Shared/Enums/TelegraphType.cs` — Normal, Unstoppable

### Data (4)
- `Shared/Data/CombatEventData.cs` — 13 readonly structs for combat events
- `Shared/Data/DamagePacket.cs` — immutable damage payload
- `Shared/Data/RunEventData.cs` — 7 readonly structs for run progression
- `Shared/Data/PlaceholderTypes.cs` — OnHitEffect, OnTriggerEffect, PathAbility

## Acceptance Criteria
- [x] All 6 interfaces defined with XML doc comments
- [x] All 9 enums defined matching design doc values
- [x] All data structs are readonly
- [x] DamagePacket includes type, amount, knockback, launch, source
- [x] Zero cross-pillar imports (Shared only depends on UnityEngine)
- [x] Namespace: `TomatoFighters.Shared.{Interfaces|Enums|Data}`
