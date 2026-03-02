# /fetch

Resume work on a task by loading its dump file and project context.

## Usage

```
/fetch                         ← Auto-detect from branch or find latest dump
/fetch T001                    ← Fetch dumps for a specific task
```

## Input

Task ID: $ARGUMENTS (optional — auto-detected from branch or finds the most recent dump)

## Instructions

### Step 1: Find dump files
Look in `tomato-fighters-docs/tasks/phase-{N}/dumps/` for files matching `T{XXX}-dump-*.md`.

- If task ID provided, look for dumps matching that task
- If no task ID, scan all phase dump directories for any dump files
- If no dumps found, report "No dumps found" and offer to run `/plan-task` instead

### Step 2: Handle multiple dumps
If multiple dump files exist for the same task (different users or sessions):

```
Found 2 dumps for T001:
  1. T001-dump-dev1-20260302.md — Dev 1 (5/14 criteria, 1 blocker)
  2. T001-dump-dev2-20260301.md — Dev 2 (3/14 criteria, no blockers)

Which dump should I load?
```

If dumps exist for DIFFERENT tasks:

```
Found dumps for multiple tasks:
  1. T001-dump-dev1-20260302.md — T001: Shared Interfaces (5/14 done)
  2. T014-dump-dev2-20260302.md — T014: Combo System (2/8 done)

Which task should I resume?
```

Let the user pick.

### Step 3: Load context
Read in this order:

1. **The dump file** — understand where things stand, what's blocked, what's next
2. **The task spec** — `tomato-fighters-docs/tasks/phase-{N}/T{XXX}-*.md` for full requirements
3. **CLAUDE.md** — project conventions
4. **Existing code** — any files already created for this task (from the dump's file list)
5. **Design Decisions** — from the task spec's `## Design Decisions` section

### Step 4: Delete the dump file
After reading, remove the dump file so it doesn't accumulate:

```
cd tomato-fighters-docs
git rm tasks/phase-{N}/dumps/T{XXX}-dump-{user}-{timestamp}.md
git commit -m "[Fetch] TXXX: Resumed by {current user}, dump cleared"
git push
```

### Step 5: Brief the developer
Present a concise resumption summary:

```
Resuming T001: Shared Interfaces
═════════════════════════════════
Dumped by: dev1 on 2026-03-02
Branch: shared/T001-shared-contracts

Progress: 5/14 acceptance criteria done
  ✅ ICombatEvents — 13 events defined
  ✅ IBuffProvider — 10 methods defined
  ✅ IPathProvider — 8 members defined
  ✅ IDamageable — 8 members defined
  ✅ IAttacker — 5 members defined
  ⬜ IRunProgressionEvents — not started
  ⬜ Enums (8 files) — not started
  ⬜ Data structs — not started

Blockers from last session:
  → "IAttacker references AttackData which doesn't exist yet — used empty stub but
     getting IntelliSense warnings"

Recommended next steps:
  1. Create IRunProgressionEvents with all 9 events
  2. Create all 8 enum files
  3. Create data structs (DamagePacket, event data, run data)

Design Decisions (from /plan-task):
  DD-1: readonly struct for event data
  DD-3: Forward-declare AttackData as empty SO stub

Ready to continue? I'll pick up from IRunProgressionEvents.
```

### Step 6: Start working
Offer to continue execution from where the dump left off:
- "Want me to continue with `/task-execute T001`?"
- "Want to discuss the blockers first with `/plan-task T001`?"
- "Want to review the existing code first?"
