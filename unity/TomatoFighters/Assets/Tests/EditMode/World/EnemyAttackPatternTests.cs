using System.Collections.Generic;
using NUnit.Framework;
using TomatoFighters.Shared.Data;
using TomatoFighters.World;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.World
{
    [TestFixture]
    public class EnemyAttackPatternTests
    {
        private AttackData _slashAttack;
        private AttackData _heavyAttack;

        [SetUp]
        public void SetUp()
        {
            _slashAttack = ScriptableObject.CreateInstance<AttackData>();
            _slashAttack.attackId = "slash";
            _slashAttack.attackName = "Slash";

            _heavyAttack = ScriptableObject.CreateInstance<AttackData>();
            _heavyAttack.attackId = "heavy";
            _heavyAttack.attackName = "Heavy";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_slashAttack);
            Object.DestroyImmediate(_heavyAttack);
        }

        // ── Helper ──────────────────────────────────────────────────────

        private EnemyAttackPattern CreatePattern(string name, float weight,
            float minRange, float maxRange, float cooldown, AttackData attack)
        {
            var p = ScriptableObject.CreateInstance<EnemyAttackPattern>();
            p.patternName = name;
            p.selectionWeight = weight;
            p.minRange = minRange;
            p.maxRange = maxRange;
            p.patternCooldown = cooldown;
            p.steps = new AttackPatternStep[]
            {
                new AttackPatternStep { attack = attack, delayBeforeStep = 0f }
            };
            return p;
        }

        // ── Null / Empty ────────────────────────────────────────────────

        [Test]
        public void Select_NullPatterns_ReturnsNull()
        {
            var result = PatternSelector.Select(null, 1f, new Dictionary<EnemyAttackPattern, float>(), 0f, 0.5f);
            Assert.IsNull(result);
        }

        [Test]
        public void Select_EmptyPatterns_ReturnsNull()
        {
            var result = PatternSelector.Select(
                new EnemyAttackPattern[0], 1f,
                new Dictionary<EnemyAttackPattern, float>(), 0f, 0.5f);
            Assert.IsNull(result);
        }

        // ── Range Filtering ─────────────────────────────────────────────

        [Test]
        public void Select_FiltersOutPatternsTooFar()
        {
            var close = CreatePattern("Close", 1f, 0f, 1f, 0f, _slashAttack);
            var far = CreatePattern("Far", 1f, 3f, 5f, 0f, _heavyAttack);
            var patterns = new[] { close, far };

            // Target at distance 0.5 — only "Close" is valid
            var result = PatternSelector.Select(patterns, 0.5f,
                new Dictionary<EnemyAttackPattern, float>(), 0f, 0.5f);

            Assert.AreEqual(close, result);

            Object.DestroyImmediate(close);
            Object.DestroyImmediate(far);
        }

        [Test]
        public void Select_FiltersOutPatternsTooClose()
        {
            var close = CreatePattern("Close", 1f, 0f, 1f, 0f, _slashAttack);
            var far = CreatePattern("Far", 1f, 3f, 5f, 0f, _heavyAttack);
            var patterns = new[] { close, far };

            // Target at distance 4 — only "Far" is valid
            var result = PatternSelector.Select(patterns, 4f,
                new Dictionary<EnemyAttackPattern, float>(), 0f, 0.5f);

            Assert.AreEqual(far, result);

            Object.DestroyImmediate(close);
            Object.DestroyImmediate(far);
        }

        // ── Cooldown Filtering ──────────────────────────────────────────

        [Test]
        public void Select_FiltersOutPatternsOnCooldown()
        {
            var p1 = CreatePattern("P1", 1f, 0f, 10f, 5f, _slashAttack);
            var p2 = CreatePattern("P2", 1f, 0f, 10f, 0f, _heavyAttack);
            var patterns = new[] { p1, p2 };

            // P1 used at time 8, current time 10 — cooldown 5s not elapsed (only 2s passed)
            var cooldowns = new Dictionary<EnemyAttackPattern, float> { { p1, 8f } };
            var result = PatternSelector.Select(patterns, 1f, cooldowns, 10f, 0.5f);

            Assert.AreEqual(p2, result);

            Object.DestroyImmediate(p1);
            Object.DestroyImmediate(p2);
        }

        [Test]
        public void Select_IncludesPatternsAfterCooldownExpires()
        {
            var p1 = CreatePattern("P1", 100f, 0f, 10f, 5f, _slashAttack);
            var patterns = new[] { p1 };

            // P1 used at time 3, current time 10 — 7s passed, cooldown 5s expired
            var cooldowns = new Dictionary<EnemyAttackPattern, float> { { p1, 3f } };
            var result = PatternSelector.Select(patterns, 1f, cooldowns, 10f, 0.5f);

            Assert.AreEqual(p1, result);

            Object.DestroyImmediate(p1);
        }

        [Test]
        public void IsReady_NoCooldownEntry_ReturnsTrue()
        {
            var p = CreatePattern("P", 1f, 0f, 10f, 5f, _slashAttack);
            var cooldowns = new Dictionary<EnemyAttackPattern, float>();

            Assert.IsTrue(PatternSelector.IsReady(p, cooldowns, 0f));

            Object.DestroyImmediate(p);
        }

        [Test]
        public void IsReady_CooldownNotExpired_ReturnsFalse()
        {
            var p = CreatePattern("P", 1f, 0f, 10f, 5f, _slashAttack);
            var cooldowns = new Dictionary<EnemyAttackPattern, float> { { p, 8f } };

            Assert.IsFalse(PatternSelector.IsReady(p, cooldowns, 10f));

            Object.DestroyImmediate(p);
        }

        // ── Weight-Based Selection ──────────────────────────────────────

        [Test]
        public void Select_WeightedSelection_LowRollPicksFirst()
        {
            var light = CreatePattern("Light", 2f, 0f, 10f, 0f, _slashAttack);
            var heavy = CreatePattern("Heavy", 1f, 0f, 10f, 0f, _heavyAttack);
            var patterns = new[] { light, heavy };

            // randomValue 0 → roll = 0 → first pattern (weight 2 of total 3)
            var result = PatternSelector.Select(patterns, 1f,
                new Dictionary<EnemyAttackPattern, float>(), 0f, 0f);

            Assert.AreEqual(light, result);

            Object.DestroyImmediate(light);
            Object.DestroyImmediate(heavy);
        }

        [Test]
        public void Select_WeightedSelection_HighRollPicksLast()
        {
            var light = CreatePattern("Light", 2f, 0f, 10f, 0f, _slashAttack);
            var heavy = CreatePattern("Heavy", 1f, 0f, 10f, 0f, _heavyAttack);
            var patterns = new[] { light, heavy };

            // randomValue ~1 → roll near totalWeight (3) → second pattern
            var result = PatternSelector.Select(patterns, 1f,
                new Dictionary<EnemyAttackPattern, float>(), 0f, 0.99f);

            Assert.AreEqual(heavy, result);

            Object.DestroyImmediate(light);
            Object.DestroyImmediate(heavy);
        }

        // ── All On Cooldown → Shortest Remaining ────────────────────────

        [Test]
        public void Select_AllOnCooldown_PicksShortestRemaining()
        {
            var p1 = CreatePattern("P1", 1f, 0f, 10f, 10f, _slashAttack);
            var p2 = CreatePattern("P2", 1f, 0f, 10f, 3f, _heavyAttack);
            var patterns = new[] { p1, p2 };

            // P1 used at 5 (remaining = 10 - (10-5) = 5s)
            // P2 used at 8 (remaining = 3 - (10-8) = 1s) — shorter
            var cooldowns = new Dictionary<EnemyAttackPattern, float>
            {
                { p1, 5f },
                { p2, 8f }
            };

            var result = PatternSelector.Select(patterns, 1f, cooldowns, 10f, 0.5f);
            Assert.AreEqual(p2, result);

            Object.DestroyImmediate(p1);
            Object.DestroyImmediate(p2);
        }

        // ── Fallback (no patterns) ──────────────────────────────────────

        [Test]
        public void Select_NoPatternsInRange_ReturnsNull()
        {
            var p = CreatePattern("P", 1f, 0f, 1f, 0f, _slashAttack);
            var patterns = new[] { p };

            // Target at distance 5 — out of range
            var result = PatternSelector.Select(patterns, 5f,
                new Dictionary<EnemyAttackPattern, float>(), 0f, 0.5f);

            Assert.IsNull(result);

            Object.DestroyImmediate(p);
        }

        // ── Step Sequence Validation ────────────────────────────────────

        [Test]
        public void Pattern_MultipleSteps_PreservesOrder()
        {
            var pattern = ScriptableObject.CreateInstance<EnemyAttackPattern>();
            pattern.steps = new AttackPatternStep[]
            {
                new AttackPatternStep { attack = _slashAttack, delayBeforeStep = 0f },
                new AttackPatternStep { attack = _heavyAttack, delayBeforeStep = 0.2f },
                new AttackPatternStep { attack = _slashAttack, delayBeforeStep = 0.1f }
            };

            Assert.AreEqual(3, pattern.steps.Length);
            Assert.AreEqual(_slashAttack, pattern.steps[0].attack);
            Assert.AreEqual(_heavyAttack, pattern.steps[1].attack);
            Assert.AreEqual(_slashAttack, pattern.steps[2].attack);
            Assert.AreEqual(0.2f, pattern.steps[1].delayBeforeStep, 0.001f);

            Object.DestroyImmediate(pattern);
        }

        [Test]
        public void Pattern_SingleStep_IsValid()
        {
            var pattern = ScriptableObject.CreateInstance<EnemyAttackPattern>();
            pattern.steps = new AttackPatternStep[]
            {
                new AttackPatternStep { attack = _slashAttack, delayBeforeStep = 0f }
            };

            Assert.AreEqual(1, pattern.steps.Length);
            Assert.AreEqual(_slashAttack, pattern.steps[0].attack);

            Object.DestroyImmediate(pattern);
        }
    }
}
