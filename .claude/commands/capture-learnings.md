# /capture-learnings

Extract reusable patterns from completed phases to improve future execution.

## Usage

```
/capture-learnings
/capture-learnings --phase 1
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--phase` | number | all | Specific phase to analyze |

## Instructions

### Step 1: Collect Results
- Read `tomato-fighters-docs/TASK_BOARD.md` for task statuses
- Read completed task specs from `tomato-fighters-docs/tasks/phase-{N}/`
- Read the actual code produced in `Assets/Scripts/`
- Note which agents were used and how they performed

### Step 2: Analyze Patterns

**What worked:**
- Which agents produced clean output on first try?
- Which task specs were detailed enough for autonomous execution?
- Which pillar isolation patterns held up?

**What didn't work:**
- Which tasks needed rework and why?
- Were there pillar boundary violations?
- Did any task specs have missing or ambiguous requirements?
- Were token budgets sufficient?

### Step 3: Extract Learnings

Categories to capture:
- **Architecture patterns** — SO patterns, event channel patterns, interface implementations
- **Agent effectiveness** — Which agents need more/less context
- **Task spec quality** — What level of detail prevents agent confusion
- **Dependency insights** — Were dependency chains correct? Any missing deps?
- **Unity gotchas** — Editor-only tasks, serialization issues, scene references

### Step 4: Generate Retrospective

Write to `tomato-fighters-docs/resources/retrospective-phase-{N}.md`:

```markdown
# Phase {N} Retrospective

## Summary
- Tasks completed: X/Y
- Average quality: XX/100
- Duration: X hours
- Token usage: XXK

## What Worked
- [Pattern 1]
- [Pattern 2]

## What Didn't Work
- [Issue 1] → [Fix applied]
- [Issue 2] → [Fix applied]

## Patterns Discovered
- [Reusable pattern 1]
- [Reusable pattern 2]

## Recommendations for Next Phase
- [Recommendation 1]
- [Recommendation 2]
```

### Step 5: Update Config
If agent configurations need tuning based on learnings, suggest specific changes to the agent `.md` files in `.claude/agents/`.
