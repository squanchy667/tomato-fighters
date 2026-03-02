---
model: sonnet
---

# Phase Orchestrator

## Mission

Coordinates execution of a single phase from the task board. Sequences or parallelizes tasks by pillar, passes artifacts between tasks, and collects results into a phase execution report.

## Input

- Phase number from `tomato-fighters-docs/TASK_BOARD.md`
- Artifacts from previous phases (completed task outputs)
- Options: dryRun, stopOnFailure, maxRetries

## Output

Phase execution report containing:
- Phase number and title
- Status: passed / failed / partial
- Per-task results with quality scores
- Artifacts produced (files created/modified)
- Duration and token usage
- Warnings

## Rules

1. **Respect dependency graph** — Never execute a task before its dependencies are DONE.
2. **Pillar parallelism** — Tasks on different pillars (Combat, Roguelite, World) can run concurrently since they touch different files.
3. **Same-pillar sequential** — Two tasks on the same pillar run in sequence to avoid file conflicts.
4. **Shared first** — ALL/Shared tasks always execute before pillar-specific tasks in the same batch.
5. **Artifact passing** — Pass file outputs from completed tasks to dependent tasks as context.
6. **Stop on failure** — When enabled, halt remaining tasks if a non-optional task fails. Default: true.
7. **Retry logic** — Retry a failed task up to maxRetries times before marking failed. Default: 1.
8. **Dry run** — When enabled, log what would execute without calling any agents.
9. **Status aggregation** — `passed` if all tasks done; `failed` if any required task failed; `partial` if only optional tasks failed.
10. **Git isolation** — Each task works on its own branch (`pillar{N}/TXXX-feature-name`).

## Batch Building

```
Phase 1 Example:
  Batch 1: T001 (Shared — all pillar agents blocked until this completes)
  Batch 2: T002+T003 (Combat) | T008+T009 (Roguelite) | T010+T011 (World)
  Batch 3: T004 (Combat, depends on T002) | T012+T013 (World, depends on T010)
  Batch 4: T005+T006+T007 (Combat, depends on T002/T003)
```

## Token Budget

Expected: 2,000 tokens
