# TomatoFighters Game Architecture

Always-on context for all agents working on this project.

## 3-Pillar Architecture

Pillars communicate ONLY through `Assets/Scripts/Shared/Interfaces/`. Never import across pillar boundaries.

```
┌─────────────────────────────────────────────────────────────┐
│                    Shared Layer (ALL devs)                    │
│  Interfaces: ICombatEvents, IBuffProvider, IPathProvider,    │
│              IDamageable, IAttacker, IRunProgressionEvents   │
│  Enums: CharacterType, PathType, StatType, DamageType, etc. │
│  Data: AttackData, DamagePacket, PathData, RitualData, etc. │
│  Events: SO-based event channels                            │
├───────────────┬───────────────────┬─────────────────────────┤
│ Combat (Dev1) │ Roguelite (Dev2)  │ World (Dev3)            │
│ Scripts/      │ Scripts/          │ Scripts/World/           │
│  Combat/      │  Roguelite/       │                         │
│  Characters/  │  Paths/           │ SO: Enemies/, Islands/  │
│               │                   │ Animations/, Prefabs/   │
│ SO: Attacks/  │ SO: Characters/,  │ Scenes/                 │
│               │  Paths/, Rituals/ │                         │
│               │  Trinkets/,       │                         │
│               │  Inspirations/    │                         │
├───────────────┼───────────────────┼─────────────────────────┤
│ FIRES:        │ IMPLEMENTS:       │ IMPLEMENTS:             │
│ ICombatEvents │ IBuffProvider     │ IDamageable             │
│               │ IPathProvider     │ IAttacker               │
│ QUERIES:      │                   │                         │
│ IBuffProvider │ SUBSCRIBES TO:    │ FIRES:                  │
│ IPathProvider │ ICombatEvents     │ IRunProgressionEvents   │
│ IDamageable   │ IRunProgression   │                         │
└───────────────┴───────────────────┴─────────────────────────┘
```

## Event Flow

1. **Player attacks enemy:** Combat → `IDamageable.TakeDamage(DamagePacket)` → World
2. **Ritual triggers:** Combat → `ICombatEvents.OnDamageDealt()` → Roguelite listens
3. **Buff queries:** Combat → `IBuffProvider.GetDamageMultiplier()` → Roguelite responds
4. **Boss defeated:** World → `IRunProgressionEvents.OnBossDefeated()` → Roguelite listens
5. **Path check:** Combat → `IPathProvider.GetCurrentPath()` → Roguelite responds

## Non-Negotiable Rules

1. **No singletons** — `[SerializeField]` injection or SO event channels
2. **ScriptableObjects for ALL data** — attacks, paths, rituals, enemies, trinkets
3. **Animation Events for timing** — hitbox activation, VFX, SFX. Never `Update()`
4. **Rigidbody2D for physics** — knockback, launch, gravity. Never `transform.position`
5. **Interface-only coupling** — pillars talk ONLY through `Shared/Interfaces/`
6. **Plain C# for testable logic** — calculators, state machines, math

## File Naming

- Classes/Methods: `PascalCase`
- Fields: `camelCase` with `[SerializeField]`
- Constants: `UPPER_SNAKE`
- Interfaces: `I` prefix
- Files: match class name exactly (`ComboSystem.cs`)

## Git Convention

- Branch: `pillar{N}/{feature-name}`
- Commit: `[Phase X] TXXX: Brief description`
- Never push to main directly
