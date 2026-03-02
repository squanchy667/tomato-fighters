using TomatoFighters.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Reads Unity Input System actions and drives the CharacterMotor.
    /// Wire InputActionReferences in the inspector from the project's InputActions asset.
    /// </summary>
    public class CharacterInputHandler : MonoBehaviour
    {
        [Header("Motor")]
        [SerializeField] private CharacterMotor motor;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference dashAction;

        private void OnEnable()
        {
            if (jumpAction != null)
                jumpAction.action.performed += OnJump;
            if (dashAction != null)
                dashAction.action.performed += OnDash;

            EnableActions();
        }

        private void OnDisable()
        {
            if (jumpAction != null)
                jumpAction.action.performed -= OnJump;
            if (dashAction != null)
                dashAction.action.performed -= OnDash;
        }

        private void Update()
        {
            if (moveAction == null || motor == null) return;

            Vector2 input = moveAction.action.ReadValue<Vector2>();
            motor.SetMoveInput(input.x);
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (motor != null)
                motor.RequestJump();
        }

        private void OnDash(InputAction.CallbackContext ctx)
        {
            if (motor == null) return;

            // Use current move input as dash direction
            Vector2 dashDir = Vector2.zero;
            if (moveAction != null)
                dashDir = moveAction.action.ReadValue<Vector2>();

            motor.RequestDash(dashDir);
        }

        private void EnableActions()
        {
            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            dashAction?.action.Enable();
        }
    }
}
