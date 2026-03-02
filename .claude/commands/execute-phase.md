# /execute-phase

Run all tasks in a phase with parallel execution and artifact handoff.

## Usage

```
/execute-phase 1
/execute-phase 2 --dry-run
/execute-phase 1 --stop-on-failure
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `phase` | number | (required) | Phase number (1, 1.5, 2, 3, 4, 5) |
| `--dry-run` | flag | false | Show execution plan without running |
| `--stop-on-failure` | flag | false | Halt remaining tasks if one fails |
| `--budget` | number | 10000 | Default token budget per task |

## Instructions

### Step 1: Load Phase Tasks
Read `tomato-fighters-docs/TASK_BOARD.md` and extract all tasks for the requested phase. For each task, check if a detailed spec exists in `tomato-fighters-docs/tasks/phase-{N}/`.

### Step 2: Check Dependencies
For each task, verify all "Depends On" tasks are marked DONE. Tasks with unmet dependencies are BLOCKED.

### Step 3: Build Execution Batches
Group tasks by dependency layers:
- **Batch 1:** Tasks with no dependencies (or all deps satisfied)
- **Batch 2:** Tasks depending on Batch 1
- **Batch 3:** Tasks depending on Batch 2
- etc.

Within each batch, tasks run in parallel (different agents working on different pillars).

### Step 4: Execute Batches
For each batch, sequentially:
1. Show the batch plan to the user
2. Run each task through `/do-task` pipeline
3. Collect results and artifacts
4. Pass artifacts to next batch's dependent tasks

**Pillar isolation rule:** Tasks on different pillars (Combat, Roguelite, World) can run in parallel since they work on different file sets. Tasks on the same pillar must be sequential.

### Step 5: Quality Gate
After all batches complete:
- **Pass rate:** ≥ 60% of tasks must succeed
- **Avg quality:** ≥ 50/100 minimum across tasks
- Report detailed reason for pass/fail

### Step 6: Phase Report
Generate a summary:

```
Phase 1: Foundation — Execution Report
═══════════════════════════════════════

Batch 1: T001 (shared-contracts)
  ✅ T001: Shared Interfaces — 92/100

Batch 2: T002, T003, T005, T006, T008, T009, T010 (parallel)
  ✅ T002: CharacterController — 88/100
  ✅ T003: InputBuffer — 85/100
  ⚠️ T005: AttackData SO — 72/100
  ...

Quality Gate: ✅ PASSED (85% pass rate, avg 84/100)
Total Tokens: 78,000
Duration: 12 min
```

### Step 7: Update Status
Mark completed tasks as DONE in the task board. Report any tasks that need rework.

## Batch Execution Rules

1. **Never skip dependencies** — If T001 fails, T002 (depends on T001) is BLOCKED
2. **Pillar parallelism** — Different pillars can run in the same batch
3. **Same-pillar sequential** — Two Combat tasks in the same batch run sequentially
4. **Shared first** — Shared/ALL tasks always run before pillar-specific tasks
5. **User checkpoint** — Pause between batches for review (unless --auto)
6. **Git convention** — Each task creates a branch: `pillar{N}/TXXX-feature-name`
7. **Commit format** — `[Phase X] TXXX: Brief description`
