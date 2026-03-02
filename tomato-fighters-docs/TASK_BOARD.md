# Task Board

> Last synced: 2026-03-02

## Phase 1 — Foundation (T001–T013)

| Task | Title | Owner | Status | Depends On |
|------|-------|-------|--------|------------|
| T001 | Shared Interfaces, Enums, and Data Structures | ALL | DONE | — |
| T002 | CharacterController — movement, jump, dash | Dev 1 | DONE | T001 |
| T003 | ComboSystem — light/heavy chains, finishers | Dev 1 | PENDING | T001 |
| T004 | HitboxManager — animation-driven hitbox activation | Dev 1 | PENDING | T002 |
| T005 | AttackData SO — damage, knockback, launch, timing | Dev 1 | PENDING | T002, T003 |
| T006 | DefenseSystem — deflect, clash, dodge | Dev 1 | PENDING | T002, T003 |
| T007 | PressureSystem — stun meter, punish windows | Dev 1 | PENDING | T002, T003 |
| T008 | PathData SO — tiers, stat bonuses, ability unlocks | Dev 2 | PENDING | T001 |
| T009 | StatCalculator — base stats + path + buff stacking | Dev 2 | PENDING | T001 |
| T010 | EnemyData SO — stats, AI config, attack patterns | Dev 3 | PENDING | T001 |
| T011 | WaveSpawner — area wave definitions, spawn logic | Dev 3 | PENDING | T001 |
| T012 | IslandManager — area sequence, boss gate, shop | Dev 3 | PENDING | T001 |
| T013 | CameraController — follow, screen shake, boundaries | Dev 3 | PENDING | T001 |

## Phase 2 — Core Combat + Path Framework (T014–T025)

| Task | Title | Owner | Status | Depends On |
|------|-------|-------|--------|------------|
| T014 | DamageCalculator — multipliers, crit, type bonuses | Dev 1 | PENDING | T005, T009 |
| T015 | KnockbackSystem — Rigidbody2D force application | Dev 1 | PENDING | T002, T005 |
| T016 | LaunchSystem — vertical launch + air combos | Dev 1 | PENDING | T002, T005 |
| T017 | RepetitiveActionPenalty — diminishing returns | Dev 1 | PENDING | T003, T005 |
| T018 | PathSelectionUI — main + secondary path choice | Dev 2 | PENDING | T008 |
| T019 | PathProgressionManager — tier unlocks, XP tracking | Dev 2 | PENDING | T008 |
| T020 | RitualData SO — family, category, trigger, effects | Dev 2 | PENDING | T001 |
| T021 | RitualSystem — equip, trigger, stacking | Dev 2 | PENDING | T020 |
| T022 | EnemyAI — basic state machine, aggro, attack | Dev 3 | PENDING | T010 |
| T023 | BossAI — phase transitions, special attacks | Dev 3 | PENDING | T010, T022 |
| T024 | HUDManager — health, mana, pressure bars | Dev 3 | PENDING | T001 |
| T025 | NavigationGraph — island map, area transitions | Dev 3 | PENDING | T012 |

## Phase 3 — Defensive Depth + Build Crafting (T026–T034)

| Task | Title | Owner | Status | Depends On |
|------|-------|-------|--------|------------|
| T026 | DeflectSystem — timing window, telegraph reading | Dev 1 | PENDING | T006 |
| T027 | ClashSystem — simultaneous hit resolution | Dev 1 | PENDING | T006 |
| T028 | PathAbilityExecutor — combat ability activation | Dev 1 | PENDING | T008, T019 |
| T029 | TrinketData SO — stat mods, conditions | Dev 2 | PENDING | T001 |
| T030 | TrinketManager — equip, stack, condition eval | Dev 2 | PENDING | T029 |
| T031 | InspirationData SO — character+path synergies | Dev 2 | PENDING | T008 |
| T032 | ShopSystem — buy, sell, reroll | Dev 3 | PENDING | T012 |
| T033 | LootDropManager — enemy drops, rarity weights | Dev 3 | PENDING | T010 |
| T034 | VFXManager — hit effects, elemental particles | Dev 3 | PENDING | T001 |

## Phase 4 — Advanced Combat + Meta-Progression (T035–T044)

| Task | Title | Owner | Status | Depends On |
|------|-------|-------|--------|------------|
| T035 | ArcanaSystem — mana ultimates per character | Dev 1 | PENDING | T014 |
| T036 | CharacterPassives — unique per-character mechanics | Dev 1 | PENDING | T009 |
| T037 | AirComboSystem — juggle, slam, air-dash | Dev 1 | PENDING | T016 |
| T038 | CombatEventDispatcher — ICombatEvents impl | Dev 1 | PENDING | T001 |
| T039 | CurrencyManager — crystals, tomato coins | Dev 2 | PENDING | T001 |
| T040 | MetaProgressionManager — permanent unlocks | Dev 2 | PENDING | T039 |
| T041 | SaveSystem — run state, meta progress | Dev 2 | PENDING | T039, T040 |
| T042 | QuestData SO — triggers, conditions, rewards | Dev 3 | PENDING | T001 |
| T043 | QuestManager — track, complete, reward | Dev 3 | PENDING | T042 |
| T044 | MiniBossEncounters — mid-island challenges | Dev 3 | PENDING | T023 |

## Phase 5 — Content + Co-op (T045–T052)

| Task | Title | Owner | Status | Depends On |
|------|-------|-------|--------|------------|
| T045 | Brutor full kit — all 3 paths, abilities | Dev 1 | PENDING | T028, T036 |
| T046 | Slasher full kit — all 3 paths, abilities | Dev 1 | PENDING | T028, T036 |
| T047 | Mystica full kit — all 3 paths, abilities | Dev 1 | PENDING | T028, T036 |
| T048 | Viper full kit — all 3 paths, abilities | Dev 1 | PENDING | T028, T036 |
| T049 | TwinRitual SO — cross-family ritual combos | Dev 2 | PENDING | T021 |
| T050 | CoopManager — shared screen, player 2 input | Dev 3 | PENDING | T002 |
| T051 | Island1Content — enemies, waves, boss | Dev 3 | PENDING | T022, T023 |
| T052 | Island2Content — enemies, waves, boss | Dev 3 | PENDING | T022, T023 |

## Phase 6 — Polish + Full Loop (T053–T060)

| Task | Title | Owner | Status | Depends On |
|------|-------|-------|--------|------------|
| T053 | AnimationController — blend trees, overrides | Dev 1 | PENDING | T003 |
| T054 | ScreenShakeSystem — impact-driven camera shake | Dev 1 | PENDING | T013 |
| T055 | HitpauseSystem — freeze frames on impact | Dev 1 | PENDING | T014 |
| T056 | BalancePass — stat tuning across all content | Dev 2 | PENDING | T045–T048 |
| T057 | TutorialSystem — onboarding flow | Dev 2 | PENDING | T024 |
| T058 | AudioManager — SFX, music, spatial audio | Dev 3 | PENDING | T001 |
| T059 | ParallaxBackground — multi-layer scrolling | Dev 3 | PENDING | T013 |
| T060 | FullLoopTest — play start to finish | ALL | PENDING | T053–T059 |

## Legend

| Status | Meaning |
|--------|---------|
| DONE | All files exist and pass validation |
| IN_PROGRESS | Some files exist, work underway |
| PENDING | Not started |
| BLOCKED | Dependency not met |
