# Generate Task Specs

Generate detailed, execution-ready task specification files from TASK_BOARD.md summaries.

## Usage

```
/generate-task-specs 1              ← All tasks in Phase 1 (T001-T013)
/generate-task-specs T001           ← Single task
/generate-task-specs T014-T025      ← Range of tasks
```

## Instructions

You are invoking the `task-spec-writer` agent. Follow its full workflow:

### Step 1: Parse the input
- Phase number → look up all tasks in that phase from `tomato-fighters-docs/TASK_BOARD.md`
- Task ID → find that specific task
- Range → find all tasks in the range

### Step 2: Read source documents
Read ALL of these before writing any spec:
- `tomato-fighters-docs/TASK_BOARD.md` — task summaries
- `tomato-fighters-docs/design-specs/CHARACTER-ARCHETYPES.md` — character stats, paths, abilities
- `tomato-fighters-docs/architecture/system-overview.md` — module map
- `tomato-fighters-docs/developer/coding-standards.md` — conventions
- `tomato-fighters-docs/development-agents.md` — agent assignments, batching
- `tomato-fighters/.claude/CLAUDE.md` — non-negotiable rules

Also read any architecture docs referenced by the tasks (interface-contracts.md, data-flow.md).

### Step 3: Check for existing specs
Look in `tomato-fighters-docs/tasks/phase-{N}/` for existing specs. If a spec already exists for a task:
- Skip it (default)
- Or overwrite if the user explicitly requests regeneration

### Step 4: Generate specs
For each task, use the `task-spec-writer` agent format:
- Metadata table (phase, type, priority, owner, agent, deps, blocks, status, branch)
- Objective (1-2 sentences)
- Context (reference docs and prior tasks)
- Requirements (numbered, specific, with concrete values from design docs)
- File Plan (exact paths)
- Implementation Notes (patterns, gotchas, code snippets)
- Acceptance Criteria (checklist, testable)
- References (links to docs)

Write each spec to: `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-{slug}.md`

### Step 5: Report
After generating, print a summary:

```
Task Specs Generated
═══════════════════════
Phase 1: 13 specs written

  T001-shared-contracts.md      → shared-contracts agent
  T002-character-controller.md  → combat-agent
  T003-input-buffer.md          → combat-agent
  ...

Files: tomato-fighters-docs/tasks/phase-1/
```
