# /merge-task

Merge a completed task branch into an integration branch in **both repos**
(code + docs), then push both.

Workflow: `pillar{N}/TXXX-*` → `gal` → (you manually merge `gal` → `main`)
The same target branch is used in both repos.

## Usage

```
/merge-task T006           ← merge T006 into current branch in both repos
/merge-task T006 gal       ← merge T006 into gal in both repos explicitly
```

## Input

Arguments: $ARGUMENTS
- First arg: Task ID (e.g., T006)
- Second arg (optional): Target branch to merge into (same for both repos)

## Repo Paths

- Code repo:  `tomato-fighters/`          (relative to project parent)
- Docs repo:  `tomato-fighters-docs/`     (relative to project parent)

Both repos follow the same branch naming and target branch convention.

## Instructions

### 1. Parse arguments

Extract:
- `TASK_ID` — first argument (e.g., `T006`)
- `TARGET_BRANCH` — second argument if provided, otherwise determine in step 3

### 2. Find the task branch in each repo

For each repo run:
```bash
git -C <repo-path> branch -a | grep -i "TASK_ID"
```

For each repo, record:
- `CODE_TASK_BRANCH` — matching branch in code repo (may not exist if no code was written)
- `DOCS_TASK_BRANCH` — matching branch in docs repo (may not exist if docs were committed directly to gal)

If a repo has no matching task branch, note it as "no task branch — changes already on target branch"
and skip the merge step for that repo (but still pull + push to ensure it's current).

If multiple branches match in either repo — list them and ask the user which one to use.

### 3. Determine the target branch

If `TARGET_BRANCH` was provided as a second argument → use it for both repos.

Otherwise:
- Check current branch in the code repo: `git -C <code-repo> branch --show-current`
- If current branch is NOT a task branch (not matching `pillar*/T*` or `shared/T*` or `docs/T*`)
  → use it as target for both repos
- If current branch IS a task branch → **ask the user** which branch to merge into.
  Offer:
  - `gal` (recommended — standard integration branch)
  - `main` (warn: this bypasses the security review step — are you sure?)
  - Other (free input)

### 4. Confirm before touching anything

Print a full plan covering both repos and ask for confirmation:

```
Ready to merge:

CODE REPO (tomato-fighters):
  FROM: pillar2/T006-character-base-stats
  INTO: gal
  Steps: checkout gal → pull → merge --no-ff → push

DOCS REPO (tomato-fighters-docs):
  FROM: (no task branch — changes already on gal)
  INTO: gal
  Steps: checkout gal → pull → push

Proceed? (yes / no)
```

Wait for explicit confirmation before running any git commands.

### 5. Execute — Code Repo

```bash
cd <code-repo>
git checkout <TARGET_BRANCH>
git pull origin <TARGET_BRANCH>

# Only if a task branch exists:
git merge <CODE_TASK_BRANCH> --no-ff -m "Merge TXXX: <task title> into <TARGET_BRANCH>"

git push origin <TARGET_BRANCH>
```

### 6. Execute — Docs Repo

```bash
cd <docs-repo>
git checkout <TARGET_BRANCH>
git pull origin <TARGET_BRANCH>

# Only if a task branch exists:
git merge <DOCS_TASK_BRANCH> --no-ff -m "Merge TXXX: <task title> (docs) into <TARGET_BRANCH>"

git push origin <TARGET_BRANCH>
```

The task title for merge commit messages comes from `tomato-fighters-docs/TASK_BOARD.md`
(find the `### TXXX:` line and extract the title).

### 7. Handle merge conflicts

If any merge step produces conflicts:
- Run `git merge --abort` in that repo immediately
- Report which files conflicted and in which repo
- Provide the manual resolution commands
- Do NOT attempt to auto-resolve conflicts
- Continue with the other repo if it has no conflicts (report clearly which succeeded and which didn't)

### 8. Report

```
Merge Complete
══════════════
CODE REPO:
  FROM:  pillar2/T006-character-base-stats  (abc1234)
  INTO:  gal  (new HEAD: def5678)
  PUSH:  origin/gal ✓

DOCS REPO:
  No task branch — changes already on gal
  PUSH:  origin/gal ✓ (pulled + pushed to ensure current)

Next: both gal branches are ready for your review.
      Merge gal → main in each repo when satisfied.
```

## Safety Rules

- **Never merge directly into `main`** without explicit confirmation and a warning
- **Always `git pull` the target branch first** in both repos before merging
- **Never force-push** (`--force`) under any circumstances
- **Always use `--no-ff`** — preserves task branch history in the merge graph
- **Stop and report** if any git command exits with a non-zero status
- **Both repos use the same target branch** — never merge code to `gal` and docs to `main`
