# Tomato Fighters — Project Conventions

## What This Is
2D side-scrolling beat 'em up roguelite. 4 characters (Tank/Melee/Mage/Range) with 3 upgrade paths each. Main + Secondary path selection. Defensive combat depth (deflect/clash/punish). 8 elemental ritual families.

## Tech Stack
- Unity 2022 LTS (2D URP)
- C# with ScriptableObject-driven data
- Unity Input System (new)
- DOTween for juice
- Rigidbody2D for all physics

## Architecture: 3 Pillars
- **Combat (Dev 1):** `Scripts/Combat/`, `Scripts/Characters/`
- **Roguelite (Dev 2):** `Scripts/Roguelite/`, `Scripts/Paths/`
- **World (Dev 3):** `Scripts/World/`
- **Shared (ALL):** `Scripts/Shared/` — interfaces, data, enums, events

Pillars communicate ONLY through `Shared/Interfaces/`. Never import across pillar boundaries.

## Non-Negotiable Rules
- No singletons — use `[SerializeField]` injection or SO event channels
- ScriptableObjects for ALL data (attacks, paths, rituals, enemies, trinkets)
- Animation Events for hitbox/VFX/SFX timing — never Update()
- Rigidbody2D for physics — never transform.position for knockback/launch
- Plain C# classes for testable logic (calculators, state machines)
- Comments: WHY not WHAT. Public APIs get `<summary>` XML docs

## Naming
- PascalCase: classes, methods, properties
- camelCase: fields, local variables
- UPPER_SNAKE: constants
- I-prefix: interfaces

## Key Interfaces
- `ICombatEvents` — Combat fires, Roguelite subscribes (ritual triggers)
- `IBuffProvider` — Roguelite provides, Combat queries (damage multipliers)
- `IPathProvider` — Roguelite provides, Combat+World query (path state)
- `IDamageable` — Combat defines, World implements on enemies
- `IAttacker` — Combat defines, World implements on enemies
- `IRunProgressionEvents` — World fires, Roguelite subscribes (area/boss events)

## 4 Characters
| Char | HP | DEF | ATK | SPD | MNA | Passive |
|------|-----|-----|-----|-----|-----|---------|
| Brutor | 200 | 25 | 0.7 | 0.7 | 50 | Thick Skin (15% DR) |
| Slasher | 100 | 8 | 2.0 | 1.3 | 60 | Bloodlust (+3% ATK/hit) |
| Mystica | 50 | 5 | 0.5 | 1.0 | 150 | Arcane Resonance (+5% team dmg/cast) |
| Viper | 80 | 10 | 1.8r | 1.1 | 120 | Distance Bonus (+2%/unit) |

## Git Convention
- Branch: `pillar{N}/{feature-name}`
- Commit: `[Phase X] TXXX: Brief description`
- Never push to main directly — use integration branch

## Task Execution
Use workspace meta-commands: `/do-task`, `/task-execute`, `/execute-phase`, `/build-app`
