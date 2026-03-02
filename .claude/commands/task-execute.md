# /task-execute

Execute a task from the TomatoFighters task board autonomously.

## Usage

```
/task-execute T001
/task-execute T014
```

## Input

Task ID: $ARGUMENTS (e.g., T001, T016, T027)

## Instructions

### 1. Load Task Spec
First check for a detailed spec in `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-*.md`. If no spec exists, read the task summary from `tomato-fighters-docs/TASK_BOARD.md`.

Each task spec contains:
- **Metadata** — Type, Priority, Dependencies, Owner, Agent, Branch
- **Requirements** — Numbered list of everything to build
- **File Plan** — Exact files to create/modify
- **Design Decisions** — Agreed architecture choices from `/plan-task` (if present — these are BINDING, follow them exactly)
- **Implementation Notes** — Patterns, gotchas, code snippets
- **Acceptance Criteria** — Checklist to verify against

**IMPORTANT:** If the spec has a `## Design Decisions` section, those decisions override any conflicting guidance elsewhere. They were agreed with the developer during planning.

### 2. Check Dependencies
Verify all "Depends On" tasks are marked DONE. If any are not, report which dependencies are missing and stop.

### 3. Understand Context
- Read `CLAUDE.md` for non-negotiable rules
- Read existing code files that will be modified or extended
- Read `Shared/Interfaces/` for interface contracts
- Read related design docs (CHARACTER-ARCHETYPES.md, interface-contracts.md, etc.)
- Check the task spec's "References" section for required reading

### 4. Plan Implementation
Before writing any code:
- List all files to create/modify
- Identify the implementation sequence
- Note any decisions that need user input
- Verify pillar boundaries won't be violated

### 5. Execute
Implement the task following TomatoFighters conventions:
- **Namespaces:** `TomatoFighters.{Pillar}.{Module}` (e.g., `TomatoFighters.Combat.Movement`)
- **No singletons** — use `[SerializeField]` injection or SO event channels
- **ScriptableObjects for ALL data** — attacks, paths, rituals, enemies, trinkets
- **Animation Events for timing** — hitbox activation, VFX, SFX. Never `Update()`
- **Rigidbody2D for physics** — knockback, launch, gravity. Never `transform.position`
- **Plain C# for testable logic** — calculators, state machines, math
- **XML doc comments** on all public APIs
- **Interface-only coupling** — pillars talk ONLY through `Shared/Interfaces/`

### 6. Verify
- Check acceptance criteria from the task spec
- Verify no cross-pillar imports (run `/check-pillar` mentally)
- Verify XML doc comments on public members
- Verify file naming matches class name exactly

### 7. Report
Output a summary:
- What was implemented
- Files created/modified
- Any decisions made
- Acceptance criteria status (checklist)
- Suggested next steps

## Important
- Always read the full task spec before starting
- Follow the acceptance criteria exactly
- Don't modify files outside the task's scope
- Create a git branch: `pillar{N}/TXXX-feature-name` or `shared/TXXX-feature-name`
- Commit format: `[Phase X] TXXX: Brief description`
- Never import from another pillar's namespace
