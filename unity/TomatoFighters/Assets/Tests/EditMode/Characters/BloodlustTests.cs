using NUnit.Framework;
using TomatoFighters.Characters.Passives;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Characters
{
    [TestFixture]
    public class BloodlustTests
    {
        private PassiveConfig _config;
        private Bloodlust _passive;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PassiveConfig>();
            _config.bloodlustAtkPerStack = 0.03f;
            _config.bloodlustMaxStacks = 10;
            _config.bloodlustDecayTime = 3f;
            _passive = new Bloodlust(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void InitialState_ZeroStacks()
        {
            Assert.AreEqual(0, _passive.Stacks);
        }

        [Test]
        public void DamageMultiplier_AtZeroStacks_Returns1()
        {
            var context = new HitContext { damageType = DamageType.Physical };
            Assert.AreEqual(1f, _passive.GetDamageMultiplier(context));
        }

        [Test]
        public void OnHitLanded_IncrementsStack()
        {
            _passive.OnHitLanded();
            Assert.AreEqual(1, _passive.Stacks);
        }

        [Test]
        public void DamageMultiplier_AtOneStack_Returns103()
        {
            _passive.OnHitLanded();
            var context = new HitContext { damageType = DamageType.Physical };
            Assert.AreEqual(1.03f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void DamageMultiplier_AtMaxStacks_Returns130()
        {
            for (int i = 0; i < 10; i++)
                _passive.OnHitLanded();

            var context = new HitContext { damageType = DamageType.Physical };
            Assert.AreEqual(1.30f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void Stacks_CappedAtMax()
        {
            for (int i = 0; i < 15; i++)
                _passive.OnHitLanded();

            Assert.AreEqual(10, _passive.Stacks);
        }

        [Test]
        public void Stacks_DecayAfterTimeout()
        {
            _passive.OnHitLanded();
            _passive.OnHitLanded();
            Assert.AreEqual(2, _passive.Stacks);

            // Advance past decay time
            _passive.Tick(3.0f);
            Assert.AreEqual(0, _passive.Stacks);
        }

        [Test]
        public void Stacks_DoNotDecayBeforeTimeout()
        {
            _passive.OnHitLanded();
            _passive.OnHitLanded();

            _passive.Tick(2.9f);
            Assert.AreEqual(2, _passive.Stacks);
        }

        [Test]
        public void HitResetsDecayTimer()
        {
            _passive.OnHitLanded();
            _passive.Tick(2.5f); // 2.5s elapsed
            _passive.OnHitLanded(); // resets timer
            _passive.Tick(2.5f); // only 2.5s since last hit

            Assert.AreEqual(2, _passive.Stacks);
        }

        [Test]
        public void DefenseAndKnockback_Neutral()
        {
            Assert.AreEqual(1f, _passive.GetDefenseMultiplier());
            Assert.AreEqual(1f, _passive.GetKnockbackMultiplier());
        }

        [Test]
        public void MultipleHits_StacksIncrementSequentially()
        {
            var context = new HitContext { damageType = DamageType.Physical };

            for (int i = 1; i <= 5; i++)
            {
                _passive.OnHitLanded();
                float expected = 1f + (0.03f * i);
                Assert.AreEqual(expected, _passive.GetDamageMultiplier(context), 0.001f,
                    $"Expected {expected} at stack {i}");
            }
        }
    }
}
