# /build-app

Full orchestration: plan → configure → execute → document → learn.

## Usage

```
/build-app
/build-app --resume
/build-app --phase 2
```

## Description

End-to-end project builder that chains all capabilities:
1. **Plan** — Load task board and build dependency graph
2. **Cluster** — Group tasks by pillar and context affinity
3. **Budget** — Assign token budgets per phase with handoff overhead
4. **Execute** (`/execute-phase`) — Run each phase sequentially with parallel task batches
5. **Handoff** — Pass relevant artifacts between phases
6. **Report** — Quality gate per phase, aggregate metrics
7. **Learn** (`/capture-learnings`) — Extract patterns for future projects

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--resume` | flag | false | Resume from last incomplete phase |
| `--phase` | number | none | Start from a specific phase |
| `--budget` | number | 10000 | Default token budget per task |
| `--dry-run` | flag | false | Show full plan without executing |

## Pipeline

### Phase-by-Phase Execution
For each phase (1 → 1.5 → 2 → 3 → 4 → 5):

1. Load phase tasks from `tomato-fighters-docs/TASK_BOARD.md`
2. Load detailed specs from `tomato-fighters-docs/tasks/phase-{N}/`
3. Build execution batches respecting dependencies
4. Execute via `/execute-phase {N}`
5. Quality gate check (60% pass rate, 50 avg quality minimum)
6. User checkpoint — review results before proceeding
7. Prepare handoff artifacts for next phase

### Resumption Logic
The pipeline detects progress and resumes:
- Check task statuses in TASK_BOARD.md
- Find first phase with incomplete tasks
- Resume from that phase

### Quality Gate Per Phase
| Metric | Threshold | Description |
|--------|-----------|-------------|
| Pass rate | ≥ 60% | Minimum percentage of tasks that must succeed |
| Avg quality | ≥ 50 | Minimum average quality score across tasks |

### User Checkpoints
Between every phase, pause and show:
- Phase results summary
- Any tasks needing rework
- Next phase preview
- Option to continue, rework, or stop

## Pillar Execution Strategy

```
Phase 1: Foundation
  Batch 1: T001 (Shared) → ALL devs agree on interfaces
  Batch 2: T002-T013 (Combat + Roguelite + World in parallel)

Phase 2: Core Features
  Batch 1: T014, T016, T020 (one per pillar, parallel)
  Batch 2: T015, T017, T021 (dependent on batch 1)
  ...

Phase 3-5: Follow dependency graph
```
