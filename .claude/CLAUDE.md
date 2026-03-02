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

## Available Commands

| Command | Purpose |
|---------|---------|
| `/do-task` | Execute a single task through the 8-step pipeline |
| `/task-execute TXXX` | Execute a task from the task board autonomously |
| `/execute-phase N` | Run all tasks in a phase with parallel batching |
| `/build-app` | Full orchestration across all phases |
| `/scan-repo` | Index codebase for smarter context selection |
| `/capture-learnings` | Extract patterns from completed phases |
| `/generate-task-specs` | Generate detailed task specs from TASK_BOARD.md |
| `/check-pillar` | Verify no cross-pillar import violations |
| `/sync-docs` | Update docs repo (modes: full, status, changelog, summary, tasks, validate) |
| `/plan-task TXXX` | Interactive planning conversation before executing a task |
| `/dump` | Save current task context before ending a session (handoff) |
| `/fetch` | Resume work by loading a dump file and project context |

## Available Agents

**Project agents** (code generation): `shared-contracts`, `combat-agent`, `roguelite-agent`, `world-agent`, `so-architect`, `ability-agent`, `integration-agent`, `balance-agent`

**Meta agents** (pipeline): `task-spec-writer`, `task-planner`, `phase-orchestrator`, `phase-planner`, `task-analyzer`, `quality-gate`, `test-validator`, `documenter`, `docs-writer`, `repo-scanner`, `agent-tailor`

Use the `agent-tailor` agent to create new specialized agents when needed.

## Getting Started (Partners)

1. Clone the repo and open in Unity 2022 LTS
2. Read this file and your crew guide: `tomato-fighters-docs/developer/dev{N}-*.md`
3. Use `/task-execute TXXX` to run your assigned tasks
4. Use `/check-pillar {your-pillar}` to verify pillar boundaries
5. Use `/sync-docs` after completing tasks to update documentation
