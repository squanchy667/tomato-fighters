# /dump

Save current task context before ending a session. Creates a handoff file so you (or a teammate) can resume seamlessly.

## Usage

```
/dump                          ← Auto-detect current task from git branch
/dump T001                     ← Explicit task ID
/dump T001 --user dev1         ← Tag with your name
```

## Input

Task ID: $ARGUMENTS (optional — auto-detected from branch name if on a `pillar{N}/TXXX-*` or `shared/TXXX-*` branch)

## Instructions

### Step 1: Identify the task
- If task ID provided, use it
- If on a task branch (`pillar1/T002-character-controller`), extract the task ID
- If neither, ask the user which task they're working on

### Step 2: Gather current state
Collect everything a resuming developer needs:

1. **Git state:**
   - Current branch name
   - Modified/staged/untracked files (`git status`)
   - Recent commits on this branch (`git log --oneline -10`)
   - Diff summary of uncommitted changes

2. **Task progress:**
   - Read the task spec from `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-*.md`
   - Check each acceptance criterion — mark which are done vs remaining
   - List files from the File Plan — mark which exist vs missing

3. **Errors and blockers:**
   - Ask the user: "What's blocking you or what errors did you hit?"
   - If they describe errors, capture the exact error messages
   - If they say nothing, note "No blockers reported"

4. **Context notes:**
   - Ask the user: "Anything else the next person should know?"
   - Capture any design decisions made but not yet documented
   - Note any deviations from the task spec

### Step 3: Write the dump file
Save to: `tomato-fighters-docs/tasks/phase-{N}/dumps/T{XXX}-dump-{user}-{timestamp}.md`

Format:

```markdown
# Dump: TXXX — {Task Title}

## Session Info
| Field | Value |
|-------|-------|
| **Task** | TXXX — {title} |
| **User** | {user or "unknown"} |
| **Date** | {YYYY-MM-DD HH:MM} |
| **Branch** | {branch name} |
| **Phase** | {N} |

## Progress
### Acceptance Criteria
- [x] {Completed criterion 1}
- [x] {Completed criterion 2}
- [ ] {Remaining criterion 3}
- [ ] {Remaining criterion 4}

### Files
| File | Status |
|------|--------|
| `Shared/Interfaces/ICombatEvents.cs` | Done |
| `Shared/Interfaces/IBuffProvider.cs` | Done |
| `Shared/Enums/CharacterType.cs` | Not started |

## Blockers / Errors
{Description of what went wrong or what's blocking progress}

```
{Exact error messages if any}
```

## Uncommitted Changes
{Summary of git diff — what's been changed but not committed}

## Context Notes
{Anything else the next person should know — decisions, gotchas, WIP reasoning}

## Recommended Next Steps
1. {First thing to do when resuming}
2. {Second thing}
3. {Third thing}
```

### Step 4: Commit the dump
```
cd tomato-fighters-docs
git add tasks/phase-{N}/dumps/
git commit -m "[Dump] TXXX: Session handoff by {user}"
git push
```

### Step 5: Confirm
```
Dump saved!
═══════════
File: tomato-fighters-docs/tasks/phase-1/dumps/T001-dump-dev1-20260302.md
Task: T001 — Shared Interfaces (5/14 criteria done)
Blockers: 1 reported
Next: /fetch T001 to resume
```
