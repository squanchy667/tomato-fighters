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
| `/merge-task TXXX` | Merge a completed task branch into gal (or specified branch) |

## Available Agents

**Project agents** (code generation): `shared-contracts`, `combat-agent`, `roguelite-agent`, `world-agent`, `so-architect`, `ability-agent`, `integration-agent`, `balance-agent`

**Meta agents** (pipeline): `task-spec-writer`, `task-planner`, `phase-orchestrator`, `phase-planner`, `task-analyzer`, `quality-gate`, `test-validator`, `documenter`, `docs-writer`, `repo-scanner`, `agent-tailor`

Use the `agent-tailor` agent to create new specialized agents when needed.

## Directory Structure

> For docs↔code cross-references, see `.claude/CROSS_REFERENCE.md`

```
tomato-fighters/                              ← Code repo root
├── .claude/
│   ├── CLAUDE.md                             ← This file
│   ├── CROSS_REFERENCE.md                    ← Docs↔code navigation map
│   ├── TOOLKIT.md
│   ├── agents/                               (20 agents)
│   │   ├── shared-contracts.md               ← Cross-pillar interfaces
│   │   ├── combat-agent.md                   ← Dev 1 pillar
│   │   ├── roguelite-agent.md                ← Dev 2 pillar
│   │   ├── world-agent.md                    ← Dev 3 pillar
│   │   ├── so-architect.md                   ← ScriptableObject design
│   │   ├── ability-agent.md                  ← Path ability impl
│   │   ├── integration-agent.md              ← Cross-pillar integration
│   │   ├── balance-agent.md                  ← Tuning & balance
│   │   └── (12 meta agents: task-*, phase-*, quality-gate, etc.)
│   ├── commands/                             (13 commands)
│   │   ├── do-task.md, task-execute.md       ← Task execution
│   │   ├── execute-phase.md, build-app.md    ← Orchestration
│   │   ├── check-pillar.md                   ← Pillar boundary check
│   │   ├── sync-docs.md                      ← Docs repo sync
│   │   ├── plan-task.md, merge-task.md       ← Task workflow
│   │   └── dump.md, fetch.md                 ← Session handoff
│   └── skills/                               (4 skills)
│       ├── game-architecture/SKILL.md        ← 3-pillar rules
│       ├── context-handoff/SKILL.md
│       ├── token-budgeting/SKILL.md
│       └── workspace/SKILL.md
├── unity/TomatoFighters/Assets/
│   ├── Scripts/
│   │   ├── Shared/                           ← ALL devs — cross-pillar contracts
│   │   │   ├── Interfaces/                   (6 interfaces: ICombatEvents, IBuffProvider, etc.)
│   │   │   ├── Enums/                        (11 enums: CharacterType, PathType, StatType, etc.)
│   │   │   └── Data/                         (6 data: CharacterBaseStats, DamagePacket, AttackData, etc.)
│   │   ├── Combat/                           ← Dev 1 pillar
│   │   │   ├── Combo/                        (ComboController, ComboStateMachine, ComboDefinition, etc.)
│   │   │   └── Movement/                     (CharacterMotor, MovementStateMachine, MovementConfig)
│   │   ├── Characters/                       (CharacterInputHandler)
│   │   ├── Paths/                            (CharacterStatCalculator, FinalStats, StatModifierInput)
│   │   ├── Roguelite/                        ← Dev 2 pillar (pending)
│   │   └── World/                            ← Dev 3 pillar (pending)
│   ├── ScriptableObjects/
│   │   ├── Characters/                       (BrutorStats, SlasherStats, MysticaStats, ViperStats)
│   │   ├── ComboDefinitions/                 (Brutor_ComboDefinition)
│   │   └── MovementConfigs/                  (Brutor_MovementConfig)
│   ├── Scenes/                               (MovementTest, SampleScene)
│   ├── Prefabs/Player/                       (Player.prefab)
│   ├── Editor/Prefabs/                       (scene/prefab generators)
│   └── Tests/EditMode/Combat/                (ComboStateMachine, MovementStateMachine tests)
└── tomato-fighters-docs/                     ← Synced docs copy (subset)
    ├── SUMMARY.md, TASK_BOARD.md
    └── tasks/phase-1/                        (T001–T003)
```

### Sibling Docs Repo

```
../tomato-fighters-docs/                      ← Docs repo (GitBook)
├── TASK_BOARD.md                             ← Master: 60 tasks, 6 phases
├── PLAN.md                                   ← Architecture vision
├── TASK_LOGBOOK.md                           ← Execution history
├── development-agents.md                     ← Agent strategy
├── architecture/                             (system-overview, interface-contracts, data-flow)
├── developer/                                (setup, standards, dev1/dev2/dev3 guides)
├── design-specs/                             (CHARACTER-ARCHETYPES, PROJECT-TALAMH-CHARACTERIZATION)
├── product/                                  (features, roadmap)
├── resources/                                (tech-stack, changelog, known-issues)
├── testing/                                  (test-plan)
└── tasks/phase-1/                            (T001–T013 detailed specs)
```

## Getting Started (Partners)

1. Clone the repo and open in Unity 2022 LTS
2. Read this file and your crew guide: `tomato-fighters-docs/developer/dev{N}-*.md`
3. Use `/task-execute TXXX` to run your assigned tasks
4. Use `/check-pillar {your-pillar}` to verify pillar boundaries
5. Use `/sync-docs` after completing tasks to update documentation
