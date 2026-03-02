# /do-task

Execute a single task through the 8-step pipeline.

## Usage

```
/do-task T001
/do-task "Implement ICombatEvents interface" --budget 12000
/do-task T005 --skip-test --verbose
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `task` | string | (required) | Task ID (TXXX) or plain text task description |
| `--budget` | number | 10000 | Total token budget for this task |
| `--skip-test` | flag | false | Skip the test validation step (step 6) |
| `--verbose` | flag | false | Show detailed logging for each step |
| `--timeout` | number | 120000 | Execution timeout in milliseconds |

## Instructions

### Step 1: Resolve the Task
If a task ID is given (TXXX), load the spec from `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-*.md`. If none exists, fall back to `tomato-fighters-docs/TASK_BOARD.md` for the summary.

If plain text is given, analyze it directly.

**IMPORTANT:** If the task spec has a `## Design Decisions` section (added by `/plan-task`), those decisions are BINDING. Follow them exactly — they were agreed with the developer during planning.

### Step 2: Analyze Task
Parse the task to extract structured metadata:
- **Domains:** combat, roguelite, world, shared, unity-editor, animation, physics, UI, SO-data
- **Task type:** implementation, refactor, test, integration, debug
- **Complexity:** low (1-2 files), medium (3-5 files), high (6+ files or cross-pillar)
- **Owner:** Dev 1 (Combat), Dev 2 (Roguelite), Dev 3 (World), ALL (Shared)

### Step 3: Select Agent
Match the task to the right project agent:

| Domain | Agent |
|--------|-------|
| Shared interfaces/enums/data | `shared-contracts` |
| Combat, Characters | `combat-agent` |
| Roguelite, Paths | `roguelite-agent` |
| World, Enemies, Scenes | `world-agent` |
| ScriptableObject data | `so-architect` |
| Path abilities | `ability-agent` |
| Cross-pillar wiring | `integration-agent` |
| Tuning/balance | `balance-agent` |

### Step 4: Assemble Context
Read relevant files based on the task's domain:
- Always read: `CLAUDE.md`, task spec, interface contracts
- Combat tasks: read existing Combat/ files, character archetypes
- Roguelite tasks: read existing Roguelite/ files, path data
- World tasks: read existing World/ files, enemy data
- Shared tasks: read all existing Shared/ files

Apply token budget compression:
- FULL (relevance > 0.8) → complete file content
- SUMMARY (0.5–0.8) → signatures + key types
- REFERENCE (0.3–0.5) → file path + exports
- SKIP (< 0.3) → excluded

### Step 5: Execute
Run the selected agent with assembled context. Agent writes code following:
- Namespace conventions per CLAUDE.md
- No cross-pillar imports
- ScriptableObjects for data, [SerializeField] injection
- XML doc comments on public APIs

### Step 6: Validate (skippable)
Independent validation checks:
- All acceptance criteria from the task spec
- File plan completeness (every listed file exists)
- No pillar boundary violations
- Compiles with zero warnings (if verifiable)
- XML doc comments on public members

### Step 7: Document
Generate audit trail:
- What was built
- Files created/modified
- Decisions made
- Acceptance criteria status

### Step 8: Report
Output summary with:
- Files created/modified
- Acceptance criteria checklist
- Quality score (0–100)
- Token usage
- Suggested next steps

## Output Structure

```
Task: T001 — Shared Interfaces, Enums, and Data Structures
Agent: shared-contracts
Status: DONE ✅

Files Created:
  ✅ Shared/Interfaces/ICombatEvents.cs
  ✅ Shared/Interfaces/IBuffProvider.cs
  ...

Acceptance Criteria:
  ✅ ICombatEvents with all 13 event signatures
  ✅ IBuffProvider with all 10 methods
  ...

Quality: 92/100
Tokens: 8,200/10,000
```
