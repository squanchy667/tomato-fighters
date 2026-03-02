using TomatoFighters.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Reads Unity Input System actions and drives the CharacterMotor and ComboController.
    /// Wire InputActionReferences in the inspector from the project's InputActions asset.
    /// </summary>
    public class CharacterInputHandler : MonoBehaviour
    {
        [Header("Motor")]
        [SerializeField] private CharacterMotor motor;

        [Header("Combo")]
        [SerializeField] private ComboController comboController;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference dashAction;
        [SerializeField] private InputActionReference lightAttackAction;
        [SerializeField] private InputActionReference heavyAttackAction;

        private void OnEnable()
        {
            if (jumpAction != null)
                jumpAction.action.performed += OnJump;
            if (dashAction != null)
                dashAction.action.performed += OnDash;
            if (lightAttackAction != null)
                lightAttackAction.action.performed += OnLightAttack;
            if (heavyAttackAction != null)
                heavyAttackAction.action.performed += OnHeavyAttack;

            EnableActions();
        }

        private void OnDisable()
        {
            if (jumpAction != null)
                jumpAction.action.performed -= OnJump;
            if (dashAction != null)
                dashAction.action.performed -= OnDash;
            if (lightAttackAction != null)
                lightAttackAction.action.performed -= OnLightAttack;
            if (heavyAttackAction != null)
                heavyAttackAction.action.performed -= OnHeavyAttack;
        }

        private void Update()
        {
            if (moveAction == null || motor == null) return;

            Vector2 input = moveAction.action.ReadValue<Vector2>();
            motor.SetMoveInput(input);
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

        private void OnLightAttack(InputAction.CallbackContext ctx)
        {
            Debug.Log("[Input] Light attack (LMB) detected");
            if (comboController != null)
                comboController.RequestLightAttack();
        }

        private void OnHeavyAttack(InputAction.CallbackContext ctx)
        {
            Debug.Log("[Input] Heavy attack (E) detected");
            if (comboController != null)
                comboController.RequestHeavyAttack();
        }

        private void EnableActions()
        {
            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            dashAction?.action.Enable();
            lightAttackAction?.action.Enable();
            heavyAttackAction?.action.Enable();
        }
    }
}
