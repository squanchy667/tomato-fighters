# TomatoFighters Toolkit Reference

## Commands

| Command | Args | What it does |
|---------|------|--------------|
| `/build-app` | `--resume`, `--phase` | Full orchestration from planning to production. Runs the entire pipeline: plan → cluster → budget → execute → handoff → learn. |
| `/execute-phase N` | phase #, `--dry-run`, `--budget`, `--stop-on-failure` | Runs all tasks in a phase. Groups them into parallel batches respecting dependencies, spawns agents, collects results. |
| `/do-task` | task ID, `--budget`, `--skip-test`, `--verbose` | Executes a single task through the 8-step pipeline (analyze → context → plan → implement → validate → test → commit → report). |
| `/task-execute TXXX` | task ID (e.g., T001) | Lightweight autonomous task execution. Reads the task spec, picks the right agent, implements, and commits. |
| `/generate-task-specs` | phase #, task ID, or range (e.g., T014-T025) | Generates detailed execution-ready spec files from TASK_BOARD.md one-liner summaries. |
| `/check-pillar` | pillar name or `all` | Scans C# `using` statements to verify no cross-pillar imports. Catches Combat→Roguelite leaks etc. |
| `/sync-docs` | `full\|status\|changelog\|tasks\|summary` | Updates the docs repo to match current code state — task statuses, changelog entries, SUMMARY.md navigation. |
| `/scan-repo` | `--force` | Indexes the codebase by pillar, domain, and namespace so agents pick better context. |
| `/capture-learnings` | `--phase N` | Extracts reusable patterns, gotchas, and retrospective notes from completed work. |

## Agents — Code Generation

| Agent | Model | What it does |
|-------|-------|--------------|
| `shared-contracts` | sonnet | Writes cross-pillar interfaces, enums, data structs, and SO event channels in `Scripts/Shared/`. |
| `combat-agent` | sonnet | Implements combat systems: movement, combo chains, hitboxes, defense mechanics, pressure gauge, ability execution. |
| `roguelite-agent` | sonnet | Builds progression systems: stat calculation, path trees, ritual triggers, currency, persistence, meta-unlocks. |
| `world-agent` | sonnet | Creates world systems: enemy AI, boss patterns, wave spawning, camera, UI/HUD, room navigation, co-op wiring. |
| `so-architect` | haiku | Designs ScriptableObject data classes with `[CreateAssetMenu]` attributes. Fast and focused on the data layer. |
| `ability-agent` | sonnet | Implements all 36 path abilities (4 characters × 3 paths × 3 tiers) and integrates them into the combat system. |
| `integration-agent` | sonnet | Wires systems across the 3 pillars. Creates test scenes, verifies interface connections, resolves cross-pillar handshakes. |
| `balance-agent` | haiku | Tunes numeric values: damage curves, stat scaling, difficulty progression, economy balance, cooldown timings. |

## Agents — Pipeline & Meta

| Agent | Model | What it does |
|-------|-------|--------------|
| `task-planner` | sonnet | Interactive planning sessions. Discuss approach, surface tradeoffs, flag risks, align on strategy before coding. |
| `task-spec-writer` | sonnet | Transforms one-liner task summaries into detailed specs with acceptance criteria, file lists, and dependencies. |
| `task-analyzer` | haiku | Analyzes a task description and extracts: domains touched, pillar ownership, task type, and recommended agent. |
| `phase-planner` | sonnet | Reads the task board and builds an optimized execution plan: batch grouping, dependency ordering, budget estimates. |
| `phase-orchestrator` | sonnet | Coordinates phase execution at runtime. Sequences and parallelizes tasks, handles handoffs between agents. |
| `quality-gate` | haiku | Final checks before commit: code style, naming conventions, pillar boundaries, missing XML docs. |
| `test-validator` | sonnet | Validates completed work against acceptance criteria, coding standards, and TomatoFighters conventions. |
| `documenter` | haiku | Generates audit trail markdown: what was built, what changed, decisions made, caveats for future devs. |
| `docs-writer` | haiku | Writes and maintains GitBook-formatted documentation pages with consistent linking and SUMMARY.md navigation. |
| `repo-scanner` | sonnet | Indexes the Unity repo into a structured map by pillar, domain, namespace, and file relationships. |
| `agent-tailor` | sonnet | Creates new specialized agents on demand, following existing conventions and prompt patterns. |

## Typical Workflow

```
1. /generate-task-specs 1          ← spec out Phase 1 tasks
2. use task-planner for T001       ← discuss complex tasks before coding
3. /task-execute T001              ← execute a single task
4. /execute-phase 1                ← or run the whole phase in batches
5. /check-pillar all               ← verify no cross-pillar violations
6. /sync-docs full                 ← update documentation
7. /capture-learnings --phase 1    ← extract patterns for future phases
```
