using NUnit.Framework;
using TomatoFighters.Characters.Passives;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Characters
{
    [TestFixture]
    public class ThickSkinTests
    {
        private PassiveConfig _config;
        private ThickSkin _passive;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PassiveConfig>();
            _config.thickSkinDamageReduction = 0.15f;
            _config.thickSkinKnockbackReduction = 0.40f;
            _passive = new ThickSkin(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void DefenseMultiplier_Returns085()
        {
            Assert.AreEqual(0.85f, _passive.GetDefenseMultiplier(), 0.001f);
        }

        [Test]
        public void KnockbackMultiplier_Returns060()
        {
            Assert.AreEqual(0.60f, _passive.GetKnockbackMultiplier(), 0.001f);
        }

        [Test]
        public void DamageMultiplier_AlwaysReturns1()
        {
            var context = new HitContext { damageType = DamageType.Physical, distanceToTarget = 5f };
            Assert.AreEqual(1f, _passive.GetDamageMultiplier(context));
        }

        [Test]
        public void SpeedMultiplier_AlwaysReturns1()
        {
            Assert.AreEqual(1f, _passive.GetSpeedMultiplier());
        }

        [Test]
        public void ConstantValues_DoNotChangeOverTime()
        {
            _passive.Tick(10f);
            Assert.AreEqual(0.85f, _passive.GetDefenseMultiplier(), 0.001f);
            Assert.AreEqual(0.60f, _passive.GetKnockbackMultiplier(), 0.001f);
        }

        [Test]
        public void CustomConfig_RespectsValues()
        {
            _config.thickSkinDamageReduction = 0.25f;
            _config.thickSkinKnockbackReduction = 0.50f;
            var custom = new ThickSkin(_config);

            Assert.AreEqual(0.75f, custom.GetDefenseMultiplier(), 0.001f);
            Assert.AreEqual(0.50f, custom.GetKnockbackMultiplier(), 0.001f);
        }
    }
}
