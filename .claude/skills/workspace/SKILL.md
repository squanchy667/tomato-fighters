# TomatoFighters Workspace

This skill provides context about the project structure, available tools, and navigation.

## Repository Layout

```
TomatoFighters/
├── tomato-fighters/               ← Code repository (Unity project)
│   ├── .claude/
│   │   ├── CLAUDE.md              ← Project conventions (non-negotiable rules)
│   │   ├── agents/                ← 20 specialized agents
│   │   ├── commands/              ← 12 workflow commands
│   │   └── skills/                ← 4 always-on context modules
│   ├── unity/TomatoFighters/      ← Unity project root
│   │   ├── Assets/Scripts/
│   │   │   ├── Shared/            ← Cross-pillar contracts (ALL devs)
│   │   │   │   ├── Interfaces/    ← ICombatEvents, IBuffProvider, etc.
│   │   │   │   ├── Enums/         ← CharacterType, PathType, etc.
│   │   │   │   ├── Data/          ← DamagePacket, event structs
│   │   │   │   └── Events/        ← SO event channels
│   │   │   ├── Combat/            ← Dev 1: movement, combos, hitbox
│   │   │   ├── Characters/        ← Dev 1: character controllers, stats
│   │   │   ├── Roguelite/         ← Dev 2: rituals, currency, shop
│   │   │   ├── Paths/             ← Dev 2: 12 paths, abilities, tiers
│   │   │   └── World/             ← Dev 3: enemies, bosses, waves, HUD
│   │   ├── ScriptableObjects/     ← SO assets (attacks, paths, rituals)
│   │   ├── Animations/            ← Animation clips and controllers
│   │   ├── Prefabs/               ← Character, enemy, UI prefabs
│   │   └── Scenes/                ← Game scenes
│   └── project-context/           ← Repo scan index
│
└── tomato-fighters-docs/          ← Documentation repository (GitBook)
    ├── README.md                  ← Project overview
    ├── SUMMARY.md                 ← Table of contents
    ├── PLAN.md                    ← Architecture vision
    ├── TASK_BOARD.md              ← 60 tasks across 6 phases
    ├── development-agents.md      ← Agent strategy and batch plan
    ├── architecture/              ← System overview, data flow, interfaces
    ├── design-specs/              ← Character archetypes, combat design
    ├── developer/                 ← Setup guide, coding standards, crew guides
    ├── tasks/phase-{N}/           ← Individual task specs (TXXX-name.md)
    └── resources/                 ← Changelog, retrospectives

```

## Available Commands

| Command | Purpose |
|---------|---------|
| `/do-task` | Execute a single task through the 8-step pipeline |
| `/task-execute` | Execute a task from the task board autonomously |
| `/execute-phase` | Run all tasks in a phase with parallel batching |
| `/build-app` | Full orchestration across all phases |
| `/scan-repo` | Index codebase for smarter context selection |
| `/capture-learnings` | Extract patterns from completed phases |
| `/generate-task-specs` | Generate detailed task specs from TASK_BOARD.md |
| `/check-pillar` | Verify no cross-pillar import violations |
| `/sync-docs` | Update docs repo (modes: full, status, changelog, summary, tasks, validate) |
| `/plan-task` | Interactive planning conversation before executing a task |
| `/dump` | Save current task context before ending a session (handoff) |
| `/fetch` | Resume work by loading a dump file and project context |

## Available Agents

### Project Agents (code generation)
| Agent | Domain | Model |
|-------|--------|-------|
| `shared-contracts` | Interfaces, enums, data | sonnet |
| `combat-agent` | Movement, combos, hitbox, defense | sonnet |
| `roguelite-agent` | Stats, paths, rituals, meta, save | sonnet |
| `world-agent` | Enemies, bosses, waves, camera, HUD | sonnet |
| `so-architect` | ScriptableObject definitions | haiku |
| `ability-agent` | 36 path abilities | sonnet |
| `integration-agent` | Cross-pillar wiring | sonnet |
| `balance-agent` | Tuning, difficulty, economy | haiku |

### Meta Agents (pipeline & tooling)
| Agent | Purpose | Model |
|-------|---------|-------|
| `task-spec-writer` | Generate detailed task specs | sonnet |
| `phase-orchestrator` | Coordinate phase execution | sonnet |
| `phase-planner` | Build execution plans | sonnet |
| `task-planner` | Interactive planning conversations | sonnet |
| `task-analyzer` | Analyze task metadata | haiku |
| `quality-gate` | Quality checking | haiku |
| `test-validator` | Validate against acceptance criteria | sonnet |
| `documenter` | Audit trail generation | haiku |
| `docs-writer` | Docs repo updates (GitBook format) | haiku |
| `repo-scanner` | Index the repository | sonnet |
| `agent-tailor` | Create new specialized agents | sonnet |

## Developer Assignment

| Developer | Pillar | Directories | Branch Prefix |
|-----------|--------|-------------|---------------|
| Dev 1 | Combat + Characters | `Scripts/Combat/`, `Scripts/Characters/` | `pillar1/` |
| Dev 2 | Roguelite + Paths | `Scripts/Roguelite/`, `Scripts/Paths/` | `pillar2/` |
| Dev 3 | World | `Scripts/World/` | `pillar3/` |
| ALL | Shared | `Scripts/Shared/` | `shared/` |

## Quick Start for Partners

1. Clone the repo: `git clone {repo-url}`
2. Open in Unity 2022 LTS
3. Read `.claude/CLAUDE.md` for rules
4. Read your crew guide: `tomato-fighters-docs/developer/dev{N}-*.md`
5. Use `/task-execute TXXX` to run your assigned tasks
6. Use `/check-pillar {your-pillar}` to verify boundaries
7. Use `/agent-tailor` if you need a new specialized agent
