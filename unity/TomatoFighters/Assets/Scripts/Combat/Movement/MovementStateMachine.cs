namespace TomatoFighters.Combat
{
    /// <summary>
    /// Plain C# state machine for character movement.
    /// Tracks current state and determines which actions are allowed.
    /// </summary>
    public class MovementStateMachine
    {
        public MovementState CurrentState { get; private set; } = MovementState.Grounded;
        public MovementState PreviousState { get; private set; } = MovementState.Grounded;

        /// <summary>Transition to a new movement state.</summary>
        public void TransitionTo(MovementState newState)
        {
            if (CurrentState == newState) return;

            PreviousState = CurrentState;
            CurrentState = newState;
        }

        /// <summary>Whether jumping is allowed in the current state.</summary>
        public bool CanJump()
        {
            // Can jump from ground; coyote time is handled by the motor
            return CurrentState != MovementState.Dashing;
        }

        /// <summary>Whether dashing is allowed in the current state.</summary>
        public bool CanDash()
        {
            return CurrentState != MovementState.Dashing;
        }

        /// <summary>Whether horizontal movement input is applied.</summary>
        public bool CanMove()
        {
            return CurrentState != MovementState.Dashing;
        }

        /// <summary>Reset to grounded state.</summary>
        public void Reset()
        {
            PreviousState = MovementState.Grounded;
            CurrentState = MovementState.Grounded;
        }
    }
}
