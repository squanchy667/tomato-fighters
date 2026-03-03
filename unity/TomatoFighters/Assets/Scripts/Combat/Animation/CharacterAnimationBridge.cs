using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Bridges <see cref="CharacterMotor"/> state to Animator parameters each frame.
    /// Lives on the root Player GameObject alongside the motor; the Animator it drives
    /// lives on the Sprite child (where the SpriteRenderer is).
    ///
    /// <para><b>Animation state mapping (intent-driven, not speed-based):</b></para>
    /// <list type="bullet">
    ///   <item>Speed = 0   → idle (no movement input)</item>
    ///   <item>Speed = 0.5 → walk (moving, not running)</item>
    ///   <item>Speed = 1   → run  (moving + run activated via Left Ctrl)</item>
    /// </list>
    ///
    /// <para><b>Run behaviour:</b> Run is sticky — pressing Ctrl while moving activates it,
    /// releasing Ctrl keeps running. Run resets on: stop, attack, dash, jump.
    /// See <see cref="CharacterMotor.RequestRun"/> for reset logic.</para>
    ///
    /// <para><b>Why a separate component?</b> Keeps CharacterMotor free of Animator
    /// dependencies so the motor remains unit-testable with plain C# tests.
    /// ComboController has its own Animator reference for attack triggers.</para>
    /// </summary>
    public class CharacterAnimationBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterMotor motor;

        // Cached hashes avoid string lookups every frame
        private static readonly int SpeedHash = Animator.StringToHash(TomatoFighterAnimatorParams.SPEED);
        private static readonly int IsGroundedHash = Animator.StringToHash(TomatoFighterAnimatorParams.ISGROUNDED);

        /// <summary>
        /// Runs after Update so motor state is settled before pushing to the Animator.
        /// </summary>
        private void LateUpdate()
        {
            if (animator == null || motor == null) return;

            // Intent-driven: animation state follows player input, not velocity magnitude
            float speed;
            if (motor.MoveInput.sqrMagnitude < 0.01f)
                speed = 0f;      // idle
            else if (motor.IsRunning)
                speed = 1f;      // run
            else
                speed = 0.5f;    // walk

            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsGroundedHash, motor.IsGrounded);
        }
    }
}
