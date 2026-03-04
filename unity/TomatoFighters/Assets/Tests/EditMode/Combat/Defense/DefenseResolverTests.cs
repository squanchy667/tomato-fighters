using NUnit.Framework;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Combat.Defense
{
    [TestFixture]
    public class DefenseResolverTests
    {
        private DefenseResolver resolver;

        // Common test directions
        private static readonly Vector2 Right = Vector2.right;
        private static readonly Vector2 Left = Vector2.left;
        private static readonly Vector2 Up = Vector2.up;
        private static readonly Vector2 Down = Vector2.down;
        private static readonly Vector2 UpRight = new Vector2(1f, 1f).normalized;

        [SetUp]
        public void SetUp()
        {
            resolver = new DefenseResolver();
        }

        // ── No Defense State ───────────────────────────────────────────

        [Test]
        public void Resolve_NoDefenseState_ReturnsHit()
        {
            var result = resolver.Resolve(
                DefenseState.None, Right, Right, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        [Test]
        public void Resolve_NoDefenseState_UnstoppableAttack_ReturnsHit()
        {
            var result = resolver.Resolve(
                DefenseState.None, Right, Right, isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        // ── Dash Toward (Deflect) ──────────────────────────────────────

        [Test]
        public void Resolve_DashTowardAttacker_NormalAttack_ReturnsDeflected()
        {
            // Dashing right, attacker is to the right
            var result = resolver.Resolve(
                DefenseState.Dashing, Right, Right, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Deflected, result);
        }

        [Test]
        public void Resolve_DashTowardAttacker_UnstoppableAttack_ReturnsHit()
        {
            // Dashing right toward attacker, but attack is unstoppable
            var result = resolver.Resolve(
                DefenseState.Dashing, Right, Right, isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        [Test]
        public void Resolve_DashTowardAttacker_SlightAngle_ReturnsDeflected()
        {
            // Dashing right, attacker is up-right (within 90 degree threshold)
            var result = resolver.Resolve(
                DefenseState.Dashing, Right, UpRight, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Deflected, result);
        }

        // ── Dash Away ──────────────────────────────────────────────────

        [Test]
        public void Resolve_DashAway_NormalAttack_ReturnsHit()
        {
            // Dashing left, attacker is to the right — dashing away
            var result = resolver.Resolve(
                DefenseState.Dashing, Left, Right, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        [Test]
        public void Resolve_DashAway_UnstoppableAttack_ReturnsHit()
        {
            var result = resolver.Resolve(
                DefenseState.Dashing, Left, Right, isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        // ── Dash Vertical (Dodge) ──────────────────────────────────────

        [Test]
        public void Resolve_DashVerticalUp_NormalAttack_ReturnsDodged()
        {
            var result = resolver.Resolve(
                DefenseState.Dashing, Up, Right, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Dodged, result);
        }

        [Test]
        public void Resolve_DashVerticalDown_NormalAttack_ReturnsDodged()
        {
            var result = resolver.Resolve(
                DefenseState.Dashing, Down, Right, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Dodged, result);
        }

        [Test]
        public void Resolve_DashVerticalUp_UnstoppableAttack_ReturnsDodged()
        {
            // Dodge beats unstoppable — i-frames are universal
            var result = resolver.Resolve(
                DefenseState.Dashing, Up, Right, isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Dodged, result);
        }

        [Test]
        public void Resolve_DashVerticalDown_UnstoppableAttack_ReturnsDodged()
        {
            var result = resolver.Resolve(
                DefenseState.Dashing, Down, Left, isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Dodged, result);
        }

        // ── Heavy Startup Facing Toward (Clash) ────────────────────────

        [Test]
        public void Resolve_HeavyFacingToward_NormalAttack_ReturnsClashed()
        {
            // Facing right, attacker is to the right
            var result = resolver.Resolve(
                DefenseState.HeavyStartup, Right, Right, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Clashed, result);
        }

        [Test]
        public void Resolve_HeavyFacingToward_UnstoppableAttack_ReturnsHit()
        {
            // Facing toward attacker but attack is unstoppable — clash fails
            var result = resolver.Resolve(
                DefenseState.HeavyStartup, Right, Right, isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        // ── Heavy Startup Facing Away ──────────────────────────────────

        [Test]
        public void Resolve_HeavyFacingAway_NormalAttack_ReturnsHit()
        {
            // Facing left, attacker is to the right — facing away
            var result = resolver.Resolve(
                DefenseState.HeavyStartup, Left, Right, isAttackUnstoppable: false);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        [Test]
        public void Resolve_HeavyFacingAway_UnstoppableAttack_ReturnsHit()
        {
            var result = resolver.Resolve(
                DefenseState.HeavyStartup, Left, Right, isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        // ── IsVertical Tests ───────────────────────────────────────────

        [Test]
        public void IsVertical_Up_ReturnsTrue()
        {
            Assert.IsTrue(DefenseResolver.IsVertical(Vector2.up));
        }

        [Test]
        public void IsVertical_Down_ReturnsTrue()
        {
            Assert.IsTrue(DefenseResolver.IsVertical(Vector2.down));
        }

        [Test]
        public void IsVertical_Right_ReturnsFalse()
        {
            Assert.IsFalse(DefenseResolver.IsVertical(Vector2.right));
        }

        [Test]
        public void IsVertical_Left_ReturnsFalse()
        {
            Assert.IsFalse(DefenseResolver.IsVertical(Vector2.left));
        }

        [Test]
        public void IsVertical_DiagonalUpRight_ReturnsFalse()
        {
            // Exactly 45 degrees — at the threshold boundary, considered vertical
            Assert.IsTrue(DefenseResolver.IsVertical(new Vector2(1f, 1f).normalized));
        }

        [Test]
        public void IsVertical_MostlyHorizontal_ReturnsFalse()
        {
            // 70 degrees from Y-axis — not vertical
            Assert.IsFalse(DefenseResolver.IsVertical(new Vector2(2f, 1f).normalized));
        }

        [Test]
        public void IsVertical_MostlyVertical_ReturnsTrue()
        {
            // 20 degrees from Y-axis — vertical
            Assert.IsTrue(DefenseResolver.IsVertical(new Vector2(0.3f, 1f).normalized));
        }

        [Test]
        public void IsVertical_ZeroVector_ReturnsFalse()
        {
            Assert.IsFalse(DefenseResolver.IsVertical(Vector2.zero));
        }

        // ── IsFacing Tests ─────────────────────────────────────────────

        [Test]
        public void IsFacing_SameDirection_ReturnsTrue()
        {
            Assert.IsTrue(DefenseResolver.IsFacing(Vector2.right, Vector2.right));
        }

        [Test]
        public void IsFacing_OppositeDirection_ReturnsFalse()
        {
            Assert.IsFalse(DefenseResolver.IsFacing(Vector2.right, Vector2.left));
        }

        [Test]
        public void IsFacing_Perpendicular_ReturnsTrue()
        {
            // 90 degrees — at the threshold, considered facing
            Assert.IsTrue(DefenseResolver.IsFacing(Vector2.right, Vector2.up));
        }

        [Test]
        public void IsFacing_ZeroActionDir_ReturnsFalse()
        {
            Assert.IsFalse(DefenseResolver.IsFacing(Vector2.zero, Vector2.right));
        }

        [Test]
        public void IsFacing_ZeroAttackerDir_ReturnsFalse()
        {
            Assert.IsFalse(DefenseResolver.IsFacing(Vector2.right, Vector2.zero));
        }
    }
}
