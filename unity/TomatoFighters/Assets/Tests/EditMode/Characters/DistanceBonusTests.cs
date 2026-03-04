using NUnit.Framework;
using TomatoFighters.Characters.Passives;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Characters
{
    [TestFixture]
    public class DistanceBonusTests
    {
        private PassiveConfig _config;
        private DistanceBonus _passive;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PassiveConfig>();
            _config.distanceBonusPerUnit = 0.02f;
            _config.distanceBonusMaxPercent = 0.30f;
            _passive = new DistanceBonus(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void ZeroDistance_Returns1()
        {
            var context = new HitContext { distanceToTarget = 0f };
            Assert.AreEqual(1f, _passive.GetDamageMultiplier(context));
        }

        [Test]
        public void OneUnit_Returns102()
        {
            var context = new HitContext { distanceToTarget = 1f };
            Assert.AreEqual(1.02f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void FiveUnits_Returns110()
        {
            var context = new HitContext { distanceToTarget = 5f };
            Assert.AreEqual(1.10f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void TenUnits_Returns120()
        {
            var context = new HitContext { distanceToTarget = 10f };
            Assert.AreEqual(1.20f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void FifteenUnits_Returns130_MaxCap()
        {
            var context = new HitContext { distanceToTarget = 15f };
            Assert.AreEqual(1.30f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void BeyondCap_StillReturns130()
        {
            var context = new HitContext { distanceToTarget = 100f };
            Assert.AreEqual(1.30f, _passive.GetDamageMultiplier(context), 0.001f);
        }

        [Test]
        public void DefenseAndKnockback_Neutral()
        {
            Assert.AreEqual(1f, _passive.GetDefenseMultiplier());
            Assert.AreEqual(1f, _passive.GetKnockbackMultiplier());
        }

        [Test]
        public void SpeedMultiplier_Neutral()
        {
            Assert.AreEqual(1f, _passive.GetSpeedMultiplier());
        }

        [Test]
        public void PerHit_DifferentDistances()
        {
            // Each hit is independent — distance from HitContext
            var close = new HitContext { distanceToTarget = 2f };
            var far = new HitContext { distanceToTarget = 12f };

            Assert.AreEqual(1.04f, _passive.GetDamageMultiplier(close), 0.001f);
            Assert.AreEqual(1.24f, _passive.GetDamageMultiplier(far), 0.001f);
        }
    }
}
