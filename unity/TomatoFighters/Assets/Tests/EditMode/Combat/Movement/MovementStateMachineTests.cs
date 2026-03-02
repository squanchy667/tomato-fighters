using NUnit.Framework;
using TomatoFighters.Combat;

namespace TomatoFighters.Tests.EditMode.Combat.Movement
{
    [TestFixture]
    public class MovementStateMachineTests
    {
        private MovementStateMachine sm;

        [SetUp]
        public void SetUp()
        {
            sm = new MovementStateMachine();
        }

        // --- State Transitions ---

        [Test]
        public void StartsInGroundedState()
        {
            Assert.AreEqual(MovementState.Grounded, sm.CurrentState);
        }

        [Test]
        public void TransitionTo_Airborne_ChangesState()
        {
            sm.TransitionTo(MovementState.Airborne);

            Assert.AreEqual(MovementState.Airborne, sm.CurrentState);
        }

        [Test]
        public void TransitionTo_Dashing_ChangesState()
        {
            sm.TransitionTo(MovementState.Dashing);

            Assert.AreEqual(MovementState.Dashing, sm.CurrentState);
        }

        [Test]
        public void TransitionTo_SameState_DoesNotUpdatePrevious()
        {
            sm.TransitionTo(MovementState.Airborne);
            var previousBefore = sm.PreviousState;

            sm.TransitionTo(MovementState.Airborne);

            Assert.AreEqual(previousBefore, sm.PreviousState);
        }

        [Test]
        public void TransitionTo_UpdatesPreviousState()
        {
            sm.TransitionTo(MovementState.Airborne);
            sm.TransitionTo(MovementState.Dashing);

            Assert.AreEqual(MovementState.Airborne, sm.PreviousState);
        }

        [Test]
        public void TransitionTo_FullCycle_TracksCorrectly()
        {
            sm.TransitionTo(MovementState.Airborne);
            sm.TransitionTo(MovementState.Dashing);
            sm.TransitionTo(MovementState.Grounded);

            Assert.AreEqual(MovementState.Grounded, sm.CurrentState);
            Assert.AreEqual(MovementState.Dashing, sm.PreviousState);
        }

        // --- CanJump ---

        [Test]
        public void CanJump_WhenGrounded_ReturnsTrue()
        {
            Assert.IsTrue(sm.CanJump());
        }

        [Test]
        public void CanJump_WhenAirborne_ReturnsTrue()
        {
            sm.TransitionTo(MovementState.Airborne);

            Assert.IsTrue(sm.CanJump());
        }

        [Test]
        public void CanJump_WhenDashing_ReturnsFalse()
        {
            sm.TransitionTo(MovementState.Dashing);

            Assert.IsFalse(sm.CanJump());
        }

        // --- CanDash ---

        [Test]
        public void CanDash_WhenGrounded_ReturnsTrue()
        {
            Assert.IsTrue(sm.CanDash());
        }

        [Test]
        public void CanDash_WhenAirborne_ReturnsTrue()
        {
            sm.TransitionTo(MovementState.Airborne);

            Assert.IsTrue(sm.CanDash());
        }

        [Test]
        public void CanDash_WhenDashing_ReturnsFalse()
        {
            sm.TransitionTo(MovementState.Dashing);

            Assert.IsFalse(sm.CanDash());
        }

        // --- CanMove ---

        [Test]
        public void CanMove_WhenGrounded_ReturnsTrue()
        {
            Assert.IsTrue(sm.CanMove());
        }

        [Test]
        public void CanMove_WhenAirborne_ReturnsTrue()
        {
            sm.TransitionTo(MovementState.Airborne);

            Assert.IsTrue(sm.CanMove());
        }

        [Test]
        public void CanMove_WhenDashing_ReturnsFalse()
        {
            sm.TransitionTo(MovementState.Dashing);

            Assert.IsFalse(sm.CanMove());
        }

        // --- Reset ---

        [Test]
        public void Reset_SetsCurrentToGrounded()
        {
            sm.TransitionTo(MovementState.Dashing);

            sm.Reset();

            Assert.AreEqual(MovementState.Grounded, sm.CurrentState);
        }

        [Test]
        public void Reset_SetsPreviousToGrounded()
        {
            sm.TransitionTo(MovementState.Airborne);
            sm.TransitionTo(MovementState.Dashing);

            sm.Reset();

            Assert.AreEqual(MovementState.Grounded, sm.PreviousState);
        }
    }
}
