# /merge-task

Merge a completed task branch into an integration branch, then push.
Follows the workflow: `pillar{N}/TXXX-*` → `gal` → (you manually merge `gal` → `main`).

## Usage

```
/merge-task T006           ← merge T006's branch into current branch
/merge-task T006 gal       ← merge T006's branch into gal explicitly
```

## Input

Arguments: $ARGUMENTS
- First arg: Task ID (e.g., T006)
- Second arg (optional): Target branch to merge into

## Instructions

### 1. Parse arguments

Extract:
- `TASK_ID` — first argument (e.g., `T006`)
- `TARGET_BRANCH` — second argument if provided, otherwise determine below

### 2. Find the task branch

Run:
```bash
git branch -a | grep -i "TXXX"
```
(replace TXXX with the task ID, case-insensitive)

If exactly one branch matches → that's the task branch.
If multiple branches match → list them and ask the user which one to use.
If no branch matches → report error: "No branch found for TASK_ID. Has it been pushed?"

### 3. Determine the target branch

If `TARGET_BRANCH` was provided as a second argument → use it.

Otherwise:
- Check current branch with `git branch --show-current`
- If current branch is NOT a task branch (i.e., not matching `pillar*/T*` or `shared/T*`) → use current branch as target
- If current branch IS a task branch → **ask the user** which branch to merge into before proceeding.
  Offer these options:
  - `gal` (recommended — standard integration branch)
  - `main` (only if they're sure, warn this bypasses the security review step)
  - Other (let them type a branch name)

### 4. Confirm before merging

Print a clear summary and ask for confirmation:

```
Ready to merge:
  FROM: pillar2/T006-character-base-stats
  INTO: gal

This will:
  1. git checkout gal
  2. git pull origin gal  (ensure it's up to date)
  3. git merge pillar2/T006-character-base-stats --no-ff -m "Merge T006: CharacterBaseStats SO into gal"
  4. git push origin gal

Proceed? (yes / no)
```

Wait for user confirmation before running any git commands.

### 5. Execute the merge

Run these commands in sequence:

```bash
git checkout <TARGET_BRANCH>
git pull origin <TARGET_BRANCH>
git merge <TASK_BRANCH> --no-ff -m "Merge TXXX: <task name> into <TARGET_BRANCH>"
git push origin <TARGET_BRANCH>
```

The task name for the commit message should be read from `tomato-fighters-docs/TASK_BOARD.md`
(find the line matching the task ID and extract the title after the colon).

Use `--no-ff` (no fast-forward) so the merge commit is always created, keeping branch history
visible in git log.

### 6. Handle merge conflicts

If the merge produces conflicts:
- Run `git merge --abort` immediately
- Report which files conflicted
- Tell the user: "Merge aborted. Resolve conflicts manually, then run:
  `git merge <TASK_BRANCH> --no-ff` followed by `git push origin <TARGET_BRANCH>`"
- Do NOT attempt to auto-resolve conflicts

### 7. Return to task branch (optional)

After a successful merge, ask:
"Merge complete. Switch back to `<TASK_BRANCH>` or stay on `<TARGET_BRANCH>`?"

### 8. Report

Output a summary:

```
Merge Complete
══════════════
FROM:  pillar2/T006-character-base-stats  (commit abc1234)
INTO:  gal  (new HEAD: def5678)
PUSH:  origin/gal ✓

Next: gal is ready for you to review and merge into main when you're satisfied.
```

## Safety Rules

- **Never merge directly into `main`** without explicit confirmation and a warning
- **Always `git pull` the target branch first** to avoid pushing stale merges
- **Never force-push** (`--force`) under any circumstances
- **Always use `--no-ff`** — preserves task branch history in the merge graph
- **Stop and report** if any git command exits with a non-zero status
