using NUnit.Framework;
using TomatoFighters.Paths;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Paths
{
    /// <summary>
    /// Edit-mode unit tests for CharacterStatCalculator.
    /// Pure C# class — no Unity scene required.
    ///
    /// Formula under test: (Base + PathBonus) × RitualMultiplier × TrinketMultiplier × SoulTreeBonus
    /// </summary>
    [TestFixture]
    public class CharacterStatCalculatorTests
    {
        private CharacterStatCalculator _calc;
        private CharacterBaseStats _brutorStats;  // non-Viper: rangedAttack = -1
        private CharacterBaseStats _viperStats;   // Viper: rangedAttack >= 0

        [SetUp]
        public void SetUp()
        {
            _calc = new CharacterStatCalculator();

            // Brutor stats from CHARACTER-ARCHETYPES.md
            _brutorStats = ScriptableObject.CreateInstance<CharacterBaseStats>();
            _brutorStats.health         = 200;
            _brutorStats.defense        = 25;
            _brutorStats.attack         = 0.7f;
            _brutorStats.rangedAttack   = -1f;   // non-Viper sentinel
            _brutorStats.throwableAttack = 0.9f;
            _brutorStats.speed          = 0.7f;
            _brutorStats.mana           = 50;
            _brutorStats.manaRegen      = 2f;
            _brutorStats.critChance     = 0.05f;
            _brutorStats.stunRate       = 1.0f;

            // Viper stats from CHARACTER-ARCHETYPES.md
            _viperStats = ScriptableObject.CreateInstance<CharacterBaseStats>();
            _viperStats.health          = 80;
            _viperStats.defense         = 10;
            _viperStats.attack          = 1.8f;
            _viperStats.rangedAttack    = 1.8f;  // Viper has ranged
            _viperStats.throwableAttack = 0.9f;
            _viperStats.speed           = 1.1f;
            _viperStats.mana            = 120;
            _viperStats.manaRegen       = 4f;
            _viperStats.critChance      = 0.05f;
            _viperStats.stunRate        = 1.0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_brutorStats);
            Object.DestroyImmediate(_viperStats);
        }

        // ── Base passthrough ───────────────────────────────────────────────────

        [Test]
        public void Calculate_NoModifiers_ReturnsBaseStats()
        {
            var input  = StatModifierInput.Default(_brutorStats);
            var result = _calc.Calculate(input);

            Assert.AreEqual(200,   result.health);
            Assert.AreEqual(25,    result.defense);
            Assert.AreEqual(0.7f,  result.attack,    0.001f);
            Assert.AreEqual(0.7f,  result.speed,     0.001f);
            Assert.AreEqual(50,    result.mana);
            Assert.AreEqual(0.05f, result.critChance, 0.001f);
            Assert.AreEqual(1.0f,  result.stunRate,  0.001f);
        }

        // ── Additive path bonuses ──────────────────────────────────────────────

        [Test]
        public void Calculate_PathBonus_AddsToBase()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.Health] = 30f;  // Warden T1 +30 HP

            var result = _calc.Calculate(input);

            Assert.AreEqual(230, result.health);
        }

        [Test]
        public void Calculate_PathBonus_AppliedBeforeMultipliers()
        {
            // (0.7 + 0.3) * 2.0 = 2.0, NOT 0.7 * 2.0 + 0.3 = 1.7
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.Attack]      = 0.3f;
            input.ritualMultipliers[(int)StatType.Attack] = 2.0f;

            var result = _calc.Calculate(input);

            Assert.AreEqual((0.7f + 0.3f) * 2.0f, result.attack, 0.001f);
        }

        // ── Multiplicative modifiers ───────────────────────────────────────────

        [Test]
        public void Calculate_RitualMultiplier_MultipliesResult()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.ritualMultipliers[(int)StatType.Attack] = 1.5f;

            var result = _calc.Calculate(input);

            Assert.AreEqual(0.7f * 1.5f, result.attack, 0.001f);
        }

        [Test]
        public void Calculate_TrinketMultiplier_MultipliesResult()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.trinketMultipliers[(int)StatType.Speed] = 1.2f;

            var result = _calc.Calculate(input);

            Assert.AreEqual(0.7f * 1.2f, result.speed, 0.001f);
        }

        [Test]
        public void Calculate_SoulTreeBonus_MultipliesResult()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.soulTreeBonuses[(int)StatType.Health] = 1.1f;

            var result = _calc.Calculate(input);

            Assert.AreEqual(220, result.health);  // 200 * 1.1 = 220
        }

        [Test]
        public void Calculate_AllModifiersStacked_AppliesFullFormula()
        {
            // (0.7 + 0.2) * 1.2 * 1.1 * 1.05 = 0.9 * 1.386 = 1.2474
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.Attack]       = 0.2f;
            input.ritualMultipliers[(int)StatType.Attack]  = 1.2f;
            input.trinketMultipliers[(int)StatType.Attack] = 1.1f;
            input.soulTreeBonuses[(int)StatType.Attack]    = 1.05f;

            float expected = (0.7f + 0.2f) * 1.2f * 1.1f * 1.05f;
            var result = _calc.Calculate(input);

            Assert.AreEqual(expected, result.attack, 0.001f);
        }

        // ── Integer rounding ──────────────────────────────────────────────────

        [Test]
        public void Calculate_Health_RoundsToNearestInt_Exact()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.ritualMultipliers[(int)StatType.Health] = 1.1f;  // 200 * 1.1 = 220 exact

            var result = _calc.Calculate(input);

            Assert.AreEqual(220, result.health);
        }

        [Test]
        public void Calculate_Health_RoundsDown_WhenFractionBelow0Point5()
        {
            // 200 base, +1 path = 201, * 1.05 = 211.05 → rounds to 211
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.Health]       = 1f;
            input.ritualMultipliers[(int)StatType.Health] = 1.05f;

            var result = _calc.Calculate(input);

            Assert.AreEqual(211, result.health);
        }

        [Test]
        public void Calculate_Mana_RoundsCorrectly()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.ritualMultipliers[(int)StatType.Mana] = 1.5f;  // 50 * 1.5 = 75 exact

            var result = _calc.Calculate(input);

            Assert.AreEqual(75, result.mana);
        }

        // ── CritChance clamping ────────────────────────────────────────────────

        [Test]
        public void Calculate_CritChance_ClampsToOneWhenOvercapped()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.CritChance]        = 0.5f;   // 0.05 + 0.5 = 0.55
            input.ritualMultipliers[(int)StatType.CritChance]  = 3.0f;   // 0.55 * 3 = 1.65

            var result = _calc.Calculate(input);

            Assert.AreEqual(1.0f, result.critChance, 0.001f);
        }

        [Test]
        public void Calculate_CritChance_NotClampedWhenBelow1()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.CritChance] = 0.1f;  // 0.05 + 0.1 = 0.15

            var result = _calc.Calculate(input);

            Assert.AreEqual(0.15f, result.critChance, 0.001f);
        }

        // ── RangedAttack sentinel ─────────────────────────────────────────────

        [Test]
        public void Calculate_NonViper_RangedAttackIsMinusOne_IgnoresModifiers()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.RangedAttack]      = 0.5f;   // must be ignored
            input.ritualMultipliers[(int)StatType.RangedAttack] = 2.0f;  // must be ignored

            var result = _calc.Calculate(input);

            Assert.AreEqual(-1f, result.rangedAttack, 0.001f);
        }

        [Test]
        public void Calculate_Viper_RangedAttackCalculatesCorrectly()
        {
            var input = StatModifierInput.Default(_viperStats);
            input.pathBonuses[(int)StatType.RangedAttack] = 0.3f;  // Marksman T1

            var result = _calc.Calculate(input);

            Assert.AreEqual(1.8f + 0.3f, result.rangedAttack, 0.001f);
        }

        // ── CalculateSingleStat consistency ───────────────────────────────────

        [Test]
        public void CalculateSingleStat_MatchesCalculate_ForHealth()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.Health] = 50f;

            var full   = _calc.Calculate(input);
            var single = _calc.CalculateSingleStat(StatType.Health, input);

            Assert.AreEqual(full.health, (int)single);
        }

        [Test]
        public void CalculateSingleStat_MatchesCalculate_ForAttack()
        {
            var input = StatModifierInput.Default(_brutorStats);
            input.pathBonuses[(int)StatType.Attack]      = 0.3f;
            input.ritualMultipliers[(int)StatType.Attack] = 1.2f;

            var full   = _calc.Calculate(input);
            var single = _calc.CalculateSingleStat(StatType.Attack, input);

            Assert.AreEqual(full.attack, single, 0.001f);
        }

        [Test]
        public void CalculateSingleStat_CancelWindow_AlwaysReturnsZero()
        {
            var input = StatModifierInput.Default(_brutorStats);

            var result = _calc.CalculateSingleStat(StatType.CancelWindow, input);

            Assert.AreEqual(0f, result);
        }

        // ── FinalStats.GetAttackForMode ────────────────────────────────────────

        [Test]
        public void GetAttackForMode_NonViper_FallsBackToMeleeForRangedQuery()
        {
            var input  = StatModifierInput.Default(_brutorStats);
            var result = _calc.Calculate(input);

            // Non-Viper has rangedAttack = -1; should fall back to melee attack
            Assert.AreEqual(result.attack, result.GetAttackForMode(AttackMode.Ranged), 0.001f);
        }

        [Test]
        public void GetAttackForMode_Viper_ReturnsRangedAttack()
        {
            var input = StatModifierInput.Default(_viperStats);
            input.pathBonuses[(int)StatType.RangedAttack] = 0.5f;

            var result = _calc.Calculate(input);

            Assert.AreEqual(result.rangedAttack, result.GetAttackForMode(AttackMode.Ranged), 0.001f);
        }

        [Test]
        public void GetAttackForMode_Throwable_ReturnsThrowableAttack()
        {
            var input  = StatModifierInput.Default(_brutorStats);
            var result = _calc.Calculate(input);

            Assert.AreEqual(result.throwableAttack, result.GetAttackForMode(AttackMode.Throwable), 0.001f);
        }
    }
}
