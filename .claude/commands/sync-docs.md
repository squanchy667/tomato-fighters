# /sync-docs

Keep the documentation repo in sync with development progress.

## Usage

```
/sync-docs                     ← Full sync (all modes)
/sync-docs status              ← Update TASK_BOARD.md statuses only
/sync-docs changelog           ← Append to changelog only
/sync-docs summary             ← Rebuild SUMMARY.md navigation
/sync-docs tasks               ← Update task spec statuses
/sync-docs validate            ← Check consistency without changing anything
```

## Input

Mode: $ARGUMENTS (optional, defaults to `full`)

## Instructions

You are the docs sync agent. Your job is to keep `tomato-fighters-docs/` in sync with what's been built in `tomato-fighters/`.

**IMPORTANT:** The Unity project root is `unity/TomatoFighters/`. All script paths are relative to `unity/TomatoFighters/Assets/`. When checking if files exist, look in `tomato-fighters/unity/TomatoFighters/Assets/Scripts/`, NOT `tomato-fighters/Assets/Scripts/`.

---

### Mode: `validate`
Check consistency across all docs WITHOUT making changes. Report discrepancies:

1. **Task spec vs TASK_BOARD.md** — Compare `| **Status** |` in each task spec against the `[STATUS]` tag in TASK_BOARD.md. Flag mismatches.
2. **Task spec vs code** — For each task spec's File Plan, check if those files actually exist in the code repo. Flag specs marked DONE where files are missing, or specs marked PENDING where files exist.
3. **SUMMARY.md completeness** — Walk all `.md` files in the docs repo. Flag any not listed in SUMMARY.md.
4. **Dump files** — Check `tasks/phase-{N}/dumps/` for any active dump files. Report them (someone may need to `/fetch`).
5. **Changelog freshness** — Compare the latest changelog entry date against the latest git commit date. Flag if changelog is stale.

Output format:
```
Docs Validation Report
══════════════════════

Status Consistency:
  ✅ T001: spec=DONE, board=DONE, files=23/23 — consistent
  ⚠️ T002: spec=PENDING, board=PENDING, files=0/1 — consistent but not started
  ❌ T005: spec=DONE, board=PENDING — MISMATCH (board not updated)

SUMMARY.md:
  ✅ 45 files listed, 45 exist
  ⚠️ Missing from SUMMARY.md: developer/workflow-guide.md

Active Dumps:
  📋 tasks/phase-1/dumps/T009-dump-dev1-20260302.md

Changelog:
  ✅ Latest entry: 2026-03-02 (matches latest commit)
```

---

### Mode: `status`
Update task statuses in `tomato-fighters-docs/TASK_BOARD.md`:

1. Read `TASK_BOARD.md` and parse all task entries (format: `### TXXX: Title [STATUS]`)
2. For each task, determine the correct status:

   **Check code existence:**
   - Read the task's File Plan from its spec in `tasks/phase-{N}/T{XXX}-*.md`
   - Check if those files exist under `tomato-fighters/unity/TomatoFighters/Assets/Scripts/`
   - Also check for `.asmdef` files if the spec mentions assembly definitions

   **Status rules:**
   | Condition | Status |
   |-----------|--------|
   | All files in File Plan exist and have content | `DONE` |
   | Some files exist (partial implementation) | `IN_PROGRESS` |
   | No files exist, but all dependencies are DONE | `PENDING` |
   | No files exist, and a dependency is not DONE | `BLOCKED` |
   | Task spec says DONE (manually marked) | Trust it — `DONE` |

3. Update the `[STATUS]` tag in TASK_BOARD.md for any changes
4. **Cross-check:** If a task spec has `| **Status** | DONE |` but TASK_BOARD.md says `[PENDING]`, update the board to match the spec (spec is source of truth for completed tasks)

---

### Mode: `tasks`
Update individual task spec metadata:

1. For each task spec in `tasks/phase-{N}/T{XXX}-*.md`:
   - Check if its files exist in the code repo
   - If all files exist and TASK_BOARD.md says DONE → update spec status to `DONE ({date})`
   - If spec already says DONE, don't change it
2. Cross-reference: if the spec says DONE but TASK_BOARD.md doesn't, flag for `status` mode to fix

---

### Mode: `changelog`
Append recent work to `tomato-fighters-docs/resources/changelog.md`:

1. Read the existing changelog to find the latest entry date
2. Read git log from the **code repo** for commits after that date:
   ```
   git -C tomato-fighters log --oneline --after="{last_date}"
   ```
3. Parse commits for task references (`[Phase X] TXXX:`)
4. Group by phase and task
5. Check: if nothing new since last entry, skip (don't add empty entries)
6. Append a dated entry:

```markdown
## [Phase {N}] — {YYYY-MM-DD}

### Completed
- **TXXX**: {Task title} — {brief summary of files/changes}
  - {detail 1}
  - {detail 2}

### In Progress
- **TXXX**: {Task title} — {what's done so far}

### Design Decisions
- {Any DD-* decisions from task specs that were agreed this session}

### Notes
- {Any notable patterns, gotchas, or architecture decisions}
```

---

### Mode: `summary`
Rebuild `tomato-fighters-docs/SUMMARY.md` to reflect current docs:

1. Walk ALL `.md` files in the docs repo (excluding `.claude/` and `dumps/`)
2. Build a GitBook-compatible table of contents organized by section:

```markdown
# Summary

* [Overview](README.md)
* [Master Plan](PLAN.md)
* [Task Board](TASK_BOARD.md)
* [Task Logbook](TASK_LOGBOOK.md)
* [Development Agents](development-agents.md)

## Architecture
* [System Overview](architecture/system-overview.md)
* [Interface Contracts](architecture/interface-contracts.md)
* [Data Flow](architecture/data-flow.md)

## Developer Guides
* [Setup Guide](developer/setup-guide.md)
* [Workflow Guide](developer/workflow-guide.md)
* [Coding Standards](developer/coding-standards.md)
* [Dev 1 Guide — Combat](developer/dev1-combat-guide.md)
* [Dev 2 Guide — Roguelite](developer/dev2-roguelite-guide.md)
* [Dev 3 Guide — World](developer/dev3-world-guide.md)

## Design Specs
* [{title from H1}](design-specs/{file}.md)

## Tasks — Phase {N}
* [TXXX: {title}](tasks/phase-{N}/T{XXX}-{slug}.md)

## Resources
* [Changelog](resources/changelog.md)
* [{other resources}](resources/{file}.md)

## Testing
* [Test Plan](testing/test-plan.md)
```

**Rules:**
- Read the `# H1` heading from each file for the display title
- Task specs: use the task ID and title from the spec
- Group tasks by phase
- Don't include dump files
- Don't include `.claude/CLAUDE.md` (it has its own purpose)
- Preserve existing section ordering where possible
- Add new files to the appropriate section

---

### Mode: `full`
Run all modes in sequence: `validate` → `status` → `tasks` → `changelog` → `summary`

After all modes complete:
1. Show the full report
2. Commit all changes to the docs repo:
   ```
   git add -A
   git commit -m "[Sync] Update docs: {summary of changes}"
   git push
   ```
3. Report what was committed

---

## Output

```
Docs Sync Complete
══════════════════
Mode: full

Validation:
  ✅ All task statuses consistent
  ⚠️ 1 active dump: T009-dump-dev1-20260302.md

TASK_BOARD.md:
  ✅ T001: DONE (already correct)
  → T002-T013: PENDING (no code yet)

Task Specs:
  ✅ T001: Status = DONE (2026-03-02)

Changelog:
  ✅ Already up to date (latest: 2026-03-02)

SUMMARY.md:
  ✅ 52 entries, all files linked

Committed: [Sync] Update docs: validated T001 DONE, no changes needed
```

## Important Notes

- **Unity project root:** `tomato-fighters/unity/TomatoFighters/` — all file existence checks use this path
- **Spec is source of truth:** If a task spec says DONE, trust it over file existence checks
- **Never downgrade status:** Don't change DONE back to PENDING/IN_PROGRESS
- **Dump files are transient:** Report them but don't modify them (that's `/fetch`'s job)
- **Always commit after changes:** Docs repo changes should be committed and pushed immediately
- **Changelog is append-only:** Never modify existing entries, only add new ones
