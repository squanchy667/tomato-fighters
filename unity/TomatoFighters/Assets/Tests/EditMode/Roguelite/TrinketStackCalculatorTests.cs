using System.Collections.Generic;
using NUnit.Framework;
using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Roguelite
{
    [TestFixture]
    public class TrinketStackCalculatorTests
    {
        private CharacterBaseStats _baseStats;

        [SetUp]
        public void SetUp()
        {
            _baseStats = ScriptableObject.CreateInstance<CharacterBaseStats>();
            _baseStats.health = 100;
            _baseStats.defense = 10;
            _baseStats.attack = 1.0f;
            _baseStats.rangedAttack = -1f;
            _baseStats.throwableAttack = 0.9f;
            _baseStats.speed = 1.0f;
            _baseStats.mana = 60;
            _baseStats.manaRegen = 3f;
            _baseStats.critChance = 0.05f;
            _baseStats.stunRate = 1.0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_baseStats);
        }

        // ── Single percent trinket ────────────────────────────────────────

        [Test]
        public void SinglePercentTrinket_20PercentAttack_Returns1Point2()
        {
            // +20% ATK → multiplier = 1.2
            var trinket = CreateTrinket(StatType.Attack, 0.2f, ModifierType.Percent);
            var entries = new List<ActiveTrinketEntry> { new ActiveTrinketEntry(trinket) };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.2f, result[(int)StatType.Attack], 0.001f);
        }

        // ── Multiple percent trinkets — multiplicative stacking ───────────

        [Test]
        public void TwoPercentTrinkets_SameStat_StackMultiplicatively()
        {
            // +10% ATK × +10% ATK → 1.1 * 1.1 = 1.21 (NOT 1.2)
            var t1 = CreateTrinket(StatType.Attack, 0.1f, ModifierType.Percent);
            var t2 = CreateTrinket(StatType.Attack, 0.1f, ModifierType.Percent);
            var entries = new List<ActiveTrinketEntry>
            {
                new ActiveTrinketEntry(t1),
                new ActiveTrinketEntry(t2)
            };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.21f, result[(int)StatType.Attack], 0.001f);
        }

        // ── Flat trinket conversion ───────────────────────────────────────

        [Test]
        public void FlatTrinket_Plus5Attack_OnBase100HP_ConvertsToMultiplier()
        {
            // +5 flat Health on base 100 → (100+5)/100 = 1.05
            var trinket = CreateTrinket(StatType.Health, 5f, ModifierType.Flat);
            var entries = new List<ActiveTrinketEntry> { new ActiveTrinketEntry(trinket) };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.05f, result[(int)StatType.Health], 0.001f);
        }

        // ── Mixed flat + percent same stat ────────────────────────────────

        [Test]
        public void MixedFlatAndPercent_SameStat_MultiplyTogether()
        {
            // +5 flat Health (base 100) → 1.05
            // +10% Health → 1.1
            // Combined: 1.05 * 1.1 = 1.155
            var flatTrinket = CreateTrinket(StatType.Health, 5f, ModifierType.Flat);
            var pctTrinket = CreateTrinket(StatType.Health, 0.1f, ModifierType.Percent);
            var entries = new List<ActiveTrinketEntry>
            {
                new ActiveTrinketEntry(flatTrinket),
                new ActiveTrinketEntry(pctTrinket)
            };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.155f, result[(int)StatType.Health], 0.001f);
        }

        // ── Inactive conditional trinket ──────────────────────────────────

        [Test]
        public void InactiveConditionalTrinket_ReturnsNeutral()
        {
            // OnKill trinket not triggered → isActive = false → multiplier = 1.0
            var trinket = CreateTrinket(StatType.Attack, 0.2f, ModifierType.Percent,
                TrinketTriggerType.OnKill);
            var entries = new List<ActiveTrinketEntry> { new ActiveTrinketEntry(trinket) };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.0f, result[(int)StatType.Attack], 0.001f);
        }

        // ── Cross-stat independence ───────────────────────────────────────

        [Test]
        public void AttackTrinket_DoesNotAffectDefense()
        {
            var trinket = CreateTrinket(StatType.Attack, 0.2f, ModifierType.Percent);
            var entries = new List<ActiveTrinketEntry> { new ActiveTrinketEntry(trinket) };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.0f, result[(int)StatType.Defense], 0.001f);
            Assert.AreEqual(1.0f, result[(int)StatType.Speed], 0.001f);
        }

        // ── Zero base stat guard ──────────────────────────────────────────

        [Test]
        public void FlatModifier_ZeroBaseStat_NoDivideByZero()
        {
            // CancelWindow has base 0 — flat modifier should be safely skipped
            var trinket = CreateTrinket(StatType.CancelWindow, 5f, ModifierType.Flat);
            var entries = new List<ActiveTrinketEntry> { new ActiveTrinketEntry(trinket) };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.0f, result[(int)StatType.CancelWindow], 0.001f);
        }

        // ── Null/empty list ───────────────────────────────────────────────

        [Test]
        public void NullList_ReturnsAllNeutral()
        {
            float[] result = TrinketStackCalculator.CalculateMultipliers(null, _baseStats);

            for (int i = 0; i < result.Length; i++)
                Assert.AreEqual(1.0f, result[i], 0.001f);
        }

        [Test]
        public void EmptyList_ReturnsAllNeutral()
        {
            float[] result = TrinketStackCalculator.CalculateMultipliers(
                new List<ActiveTrinketEntry>(), _baseStats);

            for (int i = 0; i < result.Length; i++)
                Assert.AreEqual(1.0f, result[i], 0.001f);
        }

        // ── Activated conditional trinket ─────────────────────────────────

        [Test]
        public void ActivatedConditionalTrinket_AppliesModifier()
        {
            var trinket = CreateTrinket(StatType.Attack, 0.2f, ModifierType.Percent,
                TrinketTriggerType.OnKill);
            var entry = new ActiveTrinketEntry(trinket);
            entry.IsActive = true; // simulate trigger
            var entries = new List<ActiveTrinketEntry> { entry };

            float[] result = TrinketStackCalculator.CalculateMultipliers(entries, _baseStats);

            Assert.AreEqual(1.2f, result[(int)StatType.Attack], 0.001f);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private TrinketData CreateTrinket(StatType stat, float value, ModifierType type,
            TrinketTriggerType trigger = TrinketTriggerType.Always)
        {
            var data = ScriptableObject.CreateInstance<TrinketData>();
            data.affectedStat = stat;
            data.modifierValue = value;
            data.modifierType = type;
            data.triggerType = trigger;
            data.buffDuration = 5f;
            return data;
        }
    }
}
