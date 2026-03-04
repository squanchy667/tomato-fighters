using NUnit.Framework;
using TomatoFighters.Characters.Passives;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Characters
{
    [TestFixture]
    public class ArcaneResonanceTests
    {
        private PassiveConfig _config;
        private ArcaneResonance _passive;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PassiveConfig>();
            _config.arcaneResonanceDmgPerStack = 0.05f;
            _config.arcaneResonanceMaxStacks = 3;
            _config.arcaneResonanceStackDuration = 3f;
            _passive = new ArcaneResonance(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void InitialState_ZeroActiveStacks()
        {
            Assert.AreEqual(0, _passive.ActiveStacks);
        }

        [Test]
        public void DamageMultiplier_AtZeroStacks_Returns1()
        {
            var context = new HitContext { damageType = DamageType.Physical };
            Assert.AreEqual(1f, _passive.GetDamageMultiplier(context));
        }

        [Test]
        public void OnAttackPerformed_AddsOneStack()
        {
            _passive.OnAttackPerformed();
            Assert.AreEqual(1, _passive.ActiveStacks);
        }

        [Test]
        public void DamageMultiplier_OneStack_Returns105()
        {
            _passive.OnAttackPerformed();
            var context = new HitContext { damageType = DamageType.Physical };
            // 1.05^1 = 1.05
            Assert.AreEqual(1.05f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void DamageMultiplier_TwoStacks_Multiplicative()
        {
            _passive.OnAttackPerformed();
            _passive.OnAttackPerformed();
            var context = new HitContext { damageType = DamageType.Physical };
            // 1.05^2 = 1.1025
            Assert.AreEqual(1.1025f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void DamageMultiplier_ThreeStacks_FullPower()
        {
            _passive.OnAttackPerformed();
            _passive.OnAttackPerformed();
            _passive.OnAttackPerformed();
            var context = new HitContext { damageType = DamageType.Physical };
            // 1.05^3 ≈ 1.157625
            Assert.AreEqual(1.157625f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void Stacks_CappedAtMax()
        {
            _passive.OnAttackPerformed();
            _passive.OnAttackPerformed();
            _passive.OnAttackPerformed();
            _passive.OnAttackPerformed(); // 4th should replace oldest

            Assert.AreEqual(3, _passive.ActiveStacks);
        }

        [Test]
        public void Stack_ExpiresAfterDuration()
        {
            _passive.OnAttackPerformed();
            Assert.AreEqual(1, _passive.ActiveStacks);

            _passive.Tick(3.0f);
            Assert.AreEqual(0, _passive.ActiveStacks);
        }

        [Test]
        public void Stack_DoesNotExpireBeforeDuration()
        {
            _passive.OnAttackPerformed();
            _passive.Tick(2.9f);
            Assert.AreEqual(1, _passive.ActiveStacks);
        }

        [Test]
        public void IndependentExpiry_OlderStackExpiresFirst()
        {
            _passive.OnAttackPerformed();
            _passive.Tick(1.0f); // 1s elapsed on stack 1
            _passive.OnAttackPerformed(); // stack 2 added at t=1s

            _passive.Tick(2.0f); // t=3s: stack 1 expires, stack 2 has 1s left
            Assert.AreEqual(1, _passive.ActiveStacks);

            _passive.Tick(1.0f); // t=4s: stack 2 expires
            Assert.AreEqual(0, _passive.ActiveStacks);
        }

        [Test]
        public void FourthCast_ReplacesOldestStack()
        {
            _passive.OnAttackPerformed(); // slot 0 = 3s
            _passive.Tick(1.0f);
            _passive.OnAttackPerformed(); // slot 1 = 3s
            _passive.Tick(1.0f);
            _passive.OnAttackPerformed(); // slot 2 = 3s

            // All 3 slots full. slot 0 has 1s left, slot 1 has 2s, slot 2 has 3s
            Assert.AreEqual(3, _passive.ActiveStacks);

            _passive.OnAttackPerformed(); // replaces slot 0 (oldest, 1s remaining)
            Assert.AreEqual(3, _passive.ActiveStacks);
        }

        [Test]
        public void DefenseAndKnockback_Neutral()
        {
            Assert.AreEqual(1f, _passive.GetDefenseMultiplier());
            Assert.AreEqual(1f, _passive.GetKnockbackMultiplier());
        }

        [Test]
        public void OnHitLanded_DoesNotAffectStacks()
        {
            _passive.OnHitLanded();
            Assert.AreEqual(0, _passive.ActiveStacks);
        }
    }
}
