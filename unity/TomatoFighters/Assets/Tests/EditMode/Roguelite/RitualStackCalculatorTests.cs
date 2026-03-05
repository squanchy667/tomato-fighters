using NUnit.Framework;
using TomatoFighters.Roguelite;

namespace TomatoFighters.Tests.EditMode.Roguelite
{
    [TestFixture]
    public class RitualStackCalculatorTests
    {
        // ── Level multiplier tests ──────────────────────────────────────────

        [Test]
        public void GetLevelMultiplier_Level1_Returns1()
        {
            Assert.AreEqual(1.0f, RitualStackCalculator.GetLevelMultiplier(1));
        }

        [Test]
        public void GetLevelMultiplier_Level2_Returns1Point5()
        {
            Assert.AreEqual(1.5f, RitualStackCalculator.GetLevelMultiplier(2));
        }

        [Test]
        public void GetLevelMultiplier_Level3_Returns2()
        {
            Assert.AreEqual(2.0f, RitualStackCalculator.GetLevelMultiplier(3));
        }

        [Test]
        public void GetLevelMultiplier_InvalidLevel_Returns1()
        {
            Assert.AreEqual(1.0f, RitualStackCalculator.GetLevelMultiplier(0));
            Assert.AreEqual(1.0f, RitualStackCalculator.GetLevelMultiplier(99));
        }

        // ── Compute — basic formula ─────────────────────────────────────────

        [Test]
        public void Compute_Level1_ZeroStacks_DefaultPower_ReturnsBaseValue()
        {
            // 10 × 1.0 × (1.2^0=1.0) × 1.0 = 10.0
            float result = RitualStackCalculator.Compute(10f, level: 1, currentStacks: 0,
                stackingMultiplier: 1.2f);
            Assert.AreEqual(10f, result, 0.001f);
        }

        [Test]
        public void Compute_Level2_WithStacks_AppliesFormula()
        {
            // 10 × 1.5 × (1.2^3 = 1.728) × 1.0 = 25.92
            float result = RitualStackCalculator.Compute(10f, level: 2, currentStacks: 3,
                stackingMultiplier: 1.2f);
            Assert.AreEqual(25.92f, result, 0.01f);
        }

        [Test]
        public void Compute_Level3_WithRitualPower_MultipliesAll()
        {
            // 20 × 2.0 × (1.1^2 = 1.21) × 1.5 = 72.6
            float result = RitualStackCalculator.Compute(20f, level: 3, currentStacks: 2,
                stackingMultiplier: 1.1f, ritualPower: 1.5f);
            Assert.AreEqual(72.6f, result, 0.01f);
        }

        // ── Compute — edge cases ────────────────────────────────────────────

        [Test]
        public void Compute_NegativeStacks_ClampedToZero()
        {
            // Negative stacks treated as 0: 10 × 1.0 × 1.0 × 1.0 = 10.0
            float result = RitualStackCalculator.Compute(10f, level: 1, currentStacks: -5,
                stackingMultiplier: 1.2f);
            Assert.AreEqual(10f, result, 0.001f);
        }

        [Test]
        public void Compute_InvalidLevel_UsesLevel1Multiplier()
        {
            // Invalid level defaults to 1.0 multiplier: 10 × 1.0 × 1.0 × 1.0 = 10.0
            float result = RitualStackCalculator.Compute(10f, level: 0, currentStacks: 0,
                stackingMultiplier: 1.0f);
            Assert.AreEqual(10f, result, 0.001f);
        }
    }
}
