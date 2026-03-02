# /sync-docs

Keep the documentation repo in sync with development progress.

## Usage

```
/sync-docs                     ← Full sync (status + changelog + summary)
/sync-docs status              ← Update TASK_BOARD.md statuses only
/sync-docs changelog           ← Append to changelog only
/sync-docs summary             ← Rebuild SUMMARY.md navigation
/sync-docs tasks               ← Update task spec statuses
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `mode` | string | `full` | Sync mode: `full`, `status`, `changelog`, `summary`, `tasks` |

## Instructions

You are the docs sync agent. Your job is to keep `tomato-fighters-docs/` in sync with what's been built in `tomato-fighters/`.

### Mode: `status`
Update task statuses in `tomato-fighters-docs/TASK_BOARD.md`:

1. Read `TASK_BOARD.md` and parse all task entries
2. For each task marked PENDING or IN_PROGRESS, check if its files exist in the code repo:
   - Read the task spec's File Plan
   - Check if those files exist under `Assets/Scripts/`
   - If all files exist and have content → mark DONE
   - If some files exist → mark IN_PROGRESS
   - If blocked by unfinished dependency → mark BLOCKED
3. Write the updated TASK_BOARD.md

### Mode: `changelog`
Append recent work to `tomato-fighters-docs/resources/changelog.md`:

1. Read git log from the code repo for recent commits
2. Group by phase and task
3. Append a dated entry:

```markdown
## [Phase 1] — {date}

### Completed
- T001: Shared Interfaces — 18 files, all contracts defined
- T002: CharacterController — movement, jump, dash with Rigidbody2D

### In Progress
- T005: AttackData SO — base structure done, needs combo data

### Notes
- Decided to use readonly structs for all event data
```

### Mode: `summary`
Rebuild `tomato-fighters-docs/SUMMARY.md` to reflect current docs:

1. Walk all `.md` files in the docs repo
2. Build a GitBook-compatible table of contents:

```markdown
# Summary

* [Overview](README.md)
* [Plan](PLAN.md)
* [Task Board](TASK_BOARD.md)
* [Development Agents](development-agents.md)

## Architecture
* [System Overview](architecture/system-overview.md)
* [Interface Contracts](architecture/interface-contracts.md)
* [Data Flow](architecture/data-flow.md)

## Design Specs
* [Character Archetypes](design-specs/CHARACTER-ARCHETYPES.md)

## Developer
* [Setup Guide](developer/setup-guide.md)
* [Coding Standards](developer/coding-standards.md)
* [Dev 1: Combat Guide](developer/dev1-combat-guide.md)
* [Dev 2: Roguelite Guide](developer/dev2-roguelite-guide.md)
* [Dev 3: World Guide](developer/dev3-world-guide.md)

## Tasks
* [Phase 1](tasks/phase-1/README.md)
  * [T001: Shared Contracts](tasks/phase-1/T001-shared-contracts.md)
  ...

## Resources
* [Changelog](resources/changelog.md)
```

### Mode: `tasks`
Update individual task spec statuses:

1. For each task spec in `tasks/phase-{N}/`, check if the task is done
2. Update the `| **Status** |` field in the metadata table
3. If done, note the completion date

### Mode: `full`
Run all modes in sequence: `status` → `tasks` → `changelog` → `summary`

## Output

```
Docs Sync Complete
══════════════════
Mode: full

TASK_BOARD.md:
  ✅ T001: PENDING → DONE
  ✅ T002: PENDING → DONE
  → T003: PENDING (no files yet)

Changelog:
  + Added Phase 1 entry (2 tasks completed)

SUMMARY.md:
  + Rebuilt with 24 entries

Task Specs:
  ✅ T001-shared-contracts.md: Status → DONE
  ✅ T002-character-controller.md: Status → DONE
```
