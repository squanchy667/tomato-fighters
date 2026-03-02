# Context Handoff

Always-on context for managing artifact handoff between phases.

## Two-Layer Phase Reports

Every phase produces a handoff report with exactly two layers:

### Layer 1: Summary (~1K tokens)
Human-readable overview of what was accomplished:
- Tasks completed with brief descriptions
- Key decisions made
- Files created/modified under Assets/Scripts/
- Issues or warnings
- Unity Editor tasks that need manual completion

### Layer 2: Tagged Artifacts (variable size)
Machine-readable artifacts tagged by pillar for selective retrieval:
- Each artifact tagged with pillar: `[shared]`, `[combat]`, `[roguelite]`, `[world]`
- Interface signatures, SO definitions, event contracts
- File paths relative to Assets/
- Only artifacts relevant to downstream phases are included

## Artifact Tagging

Artifacts are tagged by the pillar they belong to:

```
[shared] ICombatEvents: 13 event signatures (OnStrike, OnSkill, ...)
[shared] IBuffProvider: 10 methods (GetDamageMultiplier, ...)
[combat] CharacterController: movement, jump, dash (uses Rigidbody2D)
[combat] InputBuffer: 10-frame buffer, action queue
[roguelite] StatBlock SO: 9 stats per character, modifier stack
[roguelite] CurrencyManager: 3 currencies, persistence rules
[world] EnemyAI: state machine, patrol/chase/attack/stunned
[world] WaveManager: spawn patterns, difficulty curve
```

## Selective Retrieval

Downstream phases receive ONLY relevant artifacts:

- Phase 1 (Foundation) produced: `[shared]`, `[combat]`, `[roguelite]`, `[world]` artifacts
- Phase 2 Combat tasks need: `[shared]` (interfaces) + `[combat]` (controllers, input)
- Phase 2 Roguelite tasks need: `[shared]` (interfaces) + `[roguelite]` (stats, currency)
- Phase 2 World tasks need: `[shared]` (interfaces) + `[world]` (enemies, waves)

Scoring: artifacts scored 0–1 for relevance to next phase's tasks. Only artifacts scoring > 0.3 are included.

## Compression Rules

Handoff always compressed to fit within the receiving task's token budget:
- Summary layer: always included (~1K, non-compressible)
- Artifacts: included by relevance score, highest first
- If over budget: drop lowest-scoring artifacts
- Minimum handoff: summary only (when budget is extremely tight)

## Cross-Pillar Artifact Rules

Artifacts from one pillar are relevant to another pillar ONLY when:
1. They define a Shared/ interface that the other pillar must implement
2. They modify a Shared/ data structure that the other pillar consumes
3. They are integration tasks that wire multiple pillars together

Otherwise, combat artifacts stay with combat tasks, roguelite with roguelite, etc.
