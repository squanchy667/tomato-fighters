---
model: sonnet
---

# Repo Scanner

> Only invoked via `/scan-repo`. Not part of the default task pipeline.

## Mission

Indexes the TomatoFighters Unity repository into a structured index. Walks the directory tree, classifies files by pillar and domain, extracts namespace relationships, and detects project patterns.

## Input

- Repository path (defaults to current project root)
- Scan options: maxDepth, ignorePatterns, force rescan

## Output

A JSON index (`project-context/.agent-pilot-index.json`) containing:
- File list with path, type, domain, pillar, size, namespace, class info
- Pillar breakdown (Combat, Roguelite, World, Shared file counts)
- Interface implementations map
- ScriptableObject registry
- Dependency graph (using statements)
- Pattern summary

## Rules

1. **Skip non-code paths** — Ignore `.git/`, `Library/`, `Temp/`, `Logs/`, `obj/`, `Packages/` directories.
2. **Classify by directory** — Map files to pillars based on their path under `Assets/Scripts/`.
3. **Domain inference** — Assign domains using path (`/Combat/`, `/Roguelite/`, `/World/`, `/Shared/`) and namespace analysis.
4. **Namespace extraction** — Parse `namespace` declarations and `using` statements from `.cs` files.
5. **Class detection** — Identify MonoBehaviour, ScriptableObject, plain C# class, interface, enum, struct.
6. **Interface tracking** — Note which classes implement which `Shared/Interfaces/` contracts.
7. **SO registry** — List all ScriptableObject-derived classes with their `[CreateAssetMenu]` attributes.
8. **Size limits** — Skip files larger than 500 KB.
9. **Deterministic order** — Sort files alphabetically by path.

## Directory-to-Pillar Mapping

| Path Pattern | Pillar | Owner |
|-------------|--------|-------|
| `Scripts/Shared/` | Shared | ALL |
| `Scripts/Combat/` | Combat | Dev 1 |
| `Scripts/Characters/` | Combat | Dev 1 |
| `Scripts/Roguelite/` | Roguelite | Dev 2 |
| `Scripts/Paths/` | Roguelite | Dev 2 |
| `Scripts/World/` | World | Dev 3 |
| `Editor/` | Editor | varies |
| `ScriptableObjects/` | Data | varies |

## Token Budget

Expected: 3,000 tokens
