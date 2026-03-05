using System.Collections.Generic;
using NUnit.Framework;
using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Roguelite
{
    /// <summary>
    /// Edit-mode unit tests for <see cref="RitualPoolSelector"/>.
    /// Uses a deterministic random provider to make weighted selection predictable.
    /// </summary>
    [TestFixture]
    public class RitualPoolSelectorTests
    {
        private RewardConfig _config;
        private List<RitualData> _pool;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RewardConfig>();
            _config.categoryWeights = new CategoryWeight[]
            {
                new CategoryWeight { category = RitualCategory.Core,        weight = 1.0f },
                new CategoryWeight { category = RitualCategory.General,     weight = 2.0f },
                new CategoryWeight { category = RitualCategory.Enhancement, weight = 1.5f },
                new CategoryWeight { category = RitualCategory.Twin,        weight = 0.5f }
            };

            _pool = new List<RitualData>();
            for (int i = 0; i < 5; i++)
            {
                var ritual = ScriptableObject.CreateInstance<RitualData>();
                ritual.ritualName = $"TestRitual_{i}";
                ritual.family = (RitualFamily)(i % 4);
                ritual.category = (RitualCategory)(i % 4);
                _pool.Add(ritual);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            foreach (var r in _pool)
                Object.DestroyImmediate(r);
        }

        // ── Basic selection ──────────────────────────────────────────────────

        [Test]
        public void Select_ReturnsRequestedCount()
        {
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, _config, null, 2);

            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void Select_ReturnsAll_WhenCountExceedsPool()
        {
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, _config, null, 10);

            Assert.AreEqual(5, results.Count);
        }

        [Test]
        public void Select_NoDuplicates()
        {
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, _config, null, 4);

            var set = new HashSet<RitualData>(results);
            Assert.AreEqual(results.Count, set.Count, "Duplicate rituals found in selection.");
        }

        // ── Null and empty handling ──────────────────────────────────────────

        [Test]
        public void Select_NullPool_ReturnsEmpty()
        {
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(null, _config, null, 2);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Select_EmptyPool_ReturnsEmpty()
        {
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(new List<RitualData>(), _config, null, 2);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Select_ZeroCount_ReturnsEmpty()
        {
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, _config, null, 0);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Select_NullConfig_StillWorks_DefaultWeight()
        {
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, null, null, 2);

            Assert.AreEqual(2, results.Count);
        }

        // ── Maxed ritual filtering ───────────────────────────────────────────

        [Test]
        public void Select_FiltersMaxedRituals()
        {
            var maxed = new HashSet<RitualData> { _pool[0], _pool[1] };
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, _config, maxed, 5);

            Assert.AreEqual(3, results.Count);
            Assert.IsFalse(results.Contains(_pool[0]), "Maxed ritual 0 should be excluded.");
            Assert.IsFalse(results.Contains(_pool[1]), "Maxed ritual 1 should be excluded.");
        }

        [Test]
        public void Select_AllMaxed_ReturnsEmpty()
        {
            var maxed = new HashSet<RitualData>(_pool);
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, _config, maxed, 3);

            Assert.AreEqual(0, results.Count);
        }

        // ── Weighted selection ───────────────────────────────────────────────

        [Test]
        public void Select_WeightedSelection_HighWeightPickedFirst()
        {
            // Pool: [0]=Core(w=1), [1]=General(w=2), [2]=Enhancement(w=1.5), [3]=Twin(w=0.5), [4]=Core(w=1)
            // Total weight = 1+2+1.5+0.5+1 = 6.0
            // AlwaysPickHighest picks at 99% of total → cumulative reaches last items first
            // With deterministic random at 99%, we pick the last candidate whose cumulative range includes the roll
            var selector = new RitualPoolSelector(AlwaysNinetyNinePercent);

            var results = selector.Select(_pool, _config, null, 1);

            Assert.AreEqual(1, results.Count);
            // With 99% roll: cumulative must exceed 0.99 * totalWeight
            // The last candidate (index 4, Core w=1) is selected since cumulative reaches 6.0 at the end
            Assert.AreEqual(_pool[4], results[0]);
        }

        [Test]
        public void Select_WeightedSelection_LowRollPicksFirst()
        {
            // With roll = 0, first candidate is always picked
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(_pool, _config, null, 1);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(_pool[0], results[0]);
        }

        // ── Null elements in pool ────────────────────────────────────────────

        [Test]
        public void Select_SkipsNullEntriesInPool()
        {
            var poolWithNulls = new List<RitualData> { null, _pool[0], null, _pool[1] };
            var selector = new RitualPoolSelector(AlwaysZero);

            var results = selector.Select(poolWithNulls, _config, null, 2);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(_pool[0], results[0]);
            Assert.AreEqual(_pool[1], results[1]);
        }

        // ── Deterministic random providers ───────────────────────────────────

        private static float AlwaysZero(float max) => 0f;

        private static float AlwaysNinetyNinePercent(float max) => max * 0.99f;
    }
}
