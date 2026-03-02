# Token Budgeting

Always-on context for token budget management during task execution.

## Default Budget: 10,000 Tokens

A focused 10K-token agent with exactly the right context outperforms a 100K-token agent drowning in irrelevant information. The sweet spot is enough context for the task, no more.

## Allocation Split

| Agent | Share | Default | Purpose |
|-------|-------|---------|---------|
| Task agent | 70% | 7,000 | Primary code generation |
| Test agent | 20% | 2,000 | Validation against acceptance criteria |
| Doc agent | 10% | 1,000 | Audit trail generation |

## Hard Ceiling: 15,000 Tokens

No agent ever exceeds 15K tokens regardless of budget override. Beyond this threshold, quality degrades — agents lose focus and produce inconsistent output.

## Compression Levels

Applied per-file based on relevance score:

| Level | Relevance | What's Included | Example |
|-------|-----------|-----------------|---------|
| FULL | > 0.8 | Complete file content | `ICombatEvents.cs` for a combat task |
| SUMMARY | 0.5–0.8 | Signatures + key types | `CharacterController.cs` → public API only |
| REFERENCE | 0.3–0.5 | File path + class/interface list | `WaveManager.cs` → path + public methods |
| SKIP | < 0.3 | Excluded entirely | `CurrencyManager.cs` for a combat-only task |

## Progressive Compression

When assembled context exceeds budget:

1. Start with all relevant files at FULL
2. Calculate total tokens
3. If over budget: downgrade lowest-relevance FULL files to SUMMARY
4. Recalculate — still over? Downgrade lowest SUMMARY to REFERENCE
5. Still over? SKIP lowest REFERENCE files
6. Continue until within budget

## Context Relevance by Pillar

| Task Pillar | FULL Context | SUMMARY Context | REFERENCE Context |
|-------------|-------------|-----------------|-------------------|
| Shared | All Shared/ files | CLAUDE.md, interface-contracts.md | Character archetypes |
| Combat | Combat/ files, relevant Shared/ | InputBuffer, CharacterController | Roguelite stats (via IBuffProvider) |
| Roguelite | Roguelite/ + Paths/ files, relevant Shared/ | StatBlock, CurrencyManager | Combat events (via ICombatEvents) |
| World | World/ files, relevant Shared/ | EnemyAI, WaveManager | Combat interfaces (via IDamageable) |

## Budget Override

Users can override via `--budget`:
- `--budget 5000` — Tight budget, aggressive compression
- `--budget 15000` — Maximum allowed, minimal compression
- Default 10K is right for 80% of tasks
