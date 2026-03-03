# /plan-task

Have an interactive planning conversation about a task before executing it.

## Usage

```
/plan-task T001
/plan-task T014
/plan-task T035
```

## Input

Task ID: $ARGUMENTS (e.g., T001, T014, T035)

## Instructions

You are invoking the `task-planner` agent. Follow its full workflow:

### Step 1: Load all context
Read ALL of these before starting the conversation:
- Task spec from `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-*.md` (if exists)
- Task summary from `tomato-fighters-docs/TASK_BOARD.md`
- `tomato-fighters-docs/architecture/interface-contracts.md`
- `tomato-fighters-docs/architecture/data-flow.md`
- `tomato-fighters-docs/architecture/system-overview.md`
- `tomato-fighters-docs/design-specs/CHARACTER-ARCHETYPES.md`
- `tomato-fighters-docs/developer/coding-standards.md`
- `tomato-fighters/.claude/CLAUDE.md`
- Any existing code in `Assets/Scripts/` that this task depends on or modifies

If no task spec exists, generate one first using the `task-spec-writer` agent workflow, then proceed.

### Step 2: Present the plan
Give the developer a clear breakdown:

1. **What we're building** — plain-language summary of the deliverables
2. **Why it matters** — what depends on this, what breaks without it
3. **File-by-file walkthrough** — what each file contains, key decisions baked in
4. **Design decisions to discuss** — surface choices that could go multiple ways, with tradeoffs and recommendations
5. **Risks and gotchas** — things that could bite later (forward references, interface bloat, Unity Editor needs)
6. **Execution order** — suggested sequence for writing the files

### Step 3: Discuss with the developer
Ask targeted questions ONE AT A TIME about:
- **Game design alignment** — do the interfaces match the combat/roguelite/world vision?
- **Architecture tradeoffs** — struct vs class, single file vs split, forward-declare vs generic
- **Scope boundaries** — what's in this task vs deferred to later tasks
- **Developer preferences** — file organization, naming, patterns

### Step 4: Write the spec to file (MANDATORY)
When the developer says they're happy with the plan, or invokes `/save-spec`, you MUST write the spec to disk before doing anything else:
1. Update the task spec at `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-*.md` with a `## Design Decisions` section containing:
   - Numbered decisions (DD-1, DD-2, etc.)
   - Each with rationale and code snippet where relevant
2. Update the File Plan if decisions changed it
3. Confirm to the developer: "Spec written to `{file path}`. Start a new session and run `/task-execute TXXX` to execute."

**Do NOT offer to execute in this session.** The spec is the deliverable of `/plan-task`. Execution happens in a clean session via `/task-execute`.

## Conversation Rules
- Be a knowledgeable collaborator, not a yes-machine
- Surface tradeoffs the developer might not have considered
- Reference specific game design details (character stats, path tiers, ritual families)
- Keep responses focused — don't dump everything at once
- Use code snippets to illustrate decisions being discussed
- Ask ONE design question at a time
