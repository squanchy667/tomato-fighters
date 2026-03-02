# /scan-repo

Index the codebase for smarter context selection.

## Usage

```
/scan-repo
/scan-repo --force
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--force` | flag | false | Re-scan even if index exists |

## Instructions

You are the Repo Scanner agent. Your job is to index this repository for smarter context selection by agents.

### Step 1: Check for existing index
Unless `--force` is passed, check if `project-context/.agent-pilot-index.json` exists. If it does, report its stats and exit.

### Step 2: Walk the codebase
Scan `Assets/Scripts/` and classify every file:

| Directory | Domain | Owner |
|-----------|--------|-------|
| `Scripts/Shared/Interfaces/` | shared | ALL |
| `Scripts/Shared/Enums/` | shared | ALL |
| `Scripts/Shared/Data/` | shared | ALL |
| `Scripts/Shared/Events/` | shared | ALL |
| `Scripts/Combat/` | combat | Dev 1 |
| `Scripts/Characters/` | combat | Dev 1 |
| `Scripts/Roguelite/` | roguelite | Dev 2 |
| `Scripts/Paths/` | paths | Dev 2 |
| `Scripts/World/` | world | Dev 3 |
| `Editor/` | unity-editor | varies |

### Step 3: Extract metadata
For each `.cs` file:
- Namespace and class name
- Public interface implementations
- `using` statements (for dependency tracking)
- Whether it's a MonoBehaviour, ScriptableObject, or plain C#
- Approximate line count

### Step 4: Detect patterns
- Framework: Unity 2022 LTS (2D URP)
- Architecture: 3-pillar (Combat/Roguelite/World) with Shared layer
- Data: ScriptableObject-driven
- Physics: Rigidbody2D
- Input: Unity Input System (new)
- Animation: DOTween + Animation Events

### Step 5: Save index
Write the index to `project-context/.agent-pilot-index.json`.

### Step 6: Report summary

```
Repo Scan Complete
─────────────────────
Files indexed: {totalFiles}

Languages:
  C#: {count} files

Domains:
  shared: {count} files
  combat: {count} files
  roguelite: {count} files
  world: {count} files
  editor: {count} files

Architecture:
  Framework: Unity 2022 LTS (2D URP)
  Pattern: 3-pillar with Shared contracts
  Data: ScriptableObject-driven
  Physics: Rigidbody2D

Index saved to: project-context/.agent-pilot-index.json
```
