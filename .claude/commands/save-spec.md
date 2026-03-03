# /save-spec

Write the current planning session's design decisions to the task spec file and stop.

## Usage

```
/save-spec
/save-spec T015
```

## Input

Task ID: $ARGUMENTS (optional — auto-detected from the current `/plan-task` conversation)

## Instructions

This command is meant to be used **during a `/plan-task` conversation** when you're done planning and want to persist the spec before ending the session.

### Step 1: Identify the task
- If a task ID is provided, use it
- Otherwise, use the task ID from the current `/plan-task` conversation
- If neither is available, ask the user

### Step 2: Write the spec
Update the task spec at `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-*.md`:
1. Add or update the `## Design Decisions` section with all decisions from this conversation:
   - Numbered decisions (DD-1, DD-2, etc.)
   - Each with rationale and code snippet where relevant
2. Update the `## File Plan` if any decisions changed it
3. Update the `## Implementation Notes` if relevant patterns were discussed
4. Update the `## Acceptance Criteria` if scope changed during planning

If no spec file exists yet, generate the full spec using the `task-spec-writer` agent format.

### Step 3: Confirm and stop
Print:
```
Spec saved!
════════════
File: tomato-fighters-docs/tasks/phase-{N}/T{XXX}-{slug}.md
Design Decisions: {count} documented

To execute: start a new session and run /task-execute TXXX
```

**Do NOT offer to execute. Do NOT continue planning. The session is done.**
