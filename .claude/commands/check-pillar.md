# Check Pillar Boundaries

Verify that no pillar imports from another pillar directly. All cross-pillar communication must go through `Shared/Interfaces/`.

## Usage
```
/check-pillar
/check-pillar combat
/check-pillar roguelite
/check-pillar world
```

## Instructions

Scan the specified pillar (or all pillars if none specified) for violations:

1. **Read all `.cs` files** in the pillar's directories
2. **Check `using` statements** — flag any that reference another pillar's namespace:
   - Combat files must NOT import from `Roguelite`, `Paths`, or `World`
   - Roguelite/Paths files must NOT import from `Combat`, `Characters`, or `World`
   - World files must NOT import from `Combat`, `Characters`, `Roguelite`, or `Paths`
3. **Check for direct references** — flag any class from another pillar used directly
4. **Allow** imports from `Shared.Interfaces`, `Shared.Data`, `Shared.Enums`, `Shared.Events`

## Output

```
Pillar Boundary Check
═══════════════════════
Combat:    ✅ Clean (12 files checked)
Roguelite: ⚠️ 1 violation
  → RitualSystem.cs:5 imports Combat.ComboSystem — use ICombatEvents instead
World:     ✅ Clean (8 files checked)
```
