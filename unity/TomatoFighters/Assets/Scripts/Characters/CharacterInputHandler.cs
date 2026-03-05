using TomatoFighters.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Reads Unity Input System actions and drives the CharacterMotor, ComboController,
    /// and PathAbilityExecutor. Routes dash/jump inputs through the combo cancel system
    /// when a combo is active. Wire InputActionReferences in the inspector from the
    /// project's InputActions asset. If references are null at runtime (e.g. after prefab
    /// instantiation), self-wires from a Resources-loaded InputActionAsset.
    /// </summary>
    public class CharacterInputHandler : MonoBehaviour
    {
        [Header("Motor")]
        [SerializeField] private CharacterMotor motor;

        [Header("Combo")]
        [SerializeField] private ComboController comboController;

        [Header("Abilities")]
        [SerializeField] private PathAbilityExecutor abilityExecutor;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference dashAction;
        [SerializeField] private InputActionReference lightAttackAction;
        [SerializeField] private InputActionReference heavyAttackAction;
        [SerializeField] private InputActionReference runAction;
        [SerializeField] private InputActionReference ability1Action;
        [SerializeField] private InputActionReference ability2Action;

        // Runtime-created asset kept alive so actions aren't GC'd
        private InputActionAsset _runtimeAsset;

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<CharacterMotor>();
            if (comboController == null)
                comboController = GetComponent<ComboController>();
            if (abilityExecutor == null)
                abilityExecutor = GetComponent<PathAbilityExecutor>();

            if (moveAction == null)
                SelfWireInputActions();
        }

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
            if (runAction != null)
                runAction.action.performed += OnRun;
            if (ability1Action != null)
            {
                ability1Action.action.performed += OnAbility1;
                ability1Action.action.canceled += OnAbility1Released;
            }
            if (ability2Action != null)
            {
                ability2Action.action.performed += OnAbility2;
                ability2Action.action.canceled += OnAbility2Released;
            }

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
            if (runAction != null)
                runAction.action.performed -= OnRun;
            if (ability1Action != null)
            {
                ability1Action.action.performed -= OnAbility1;
                ability1Action.action.canceled -= OnAbility1Released;
            }
            if (ability2Action != null)
            {
                ability2Action.action.performed -= OnAbility2;
                ability2Action.action.canceled -= OnAbility2Released;
            }
        }

        private void Update()
        {
            if (moveAction == null || motor == null) return;

            Vector2 input = moveAction.action.ReadValue<Vector2>();
            motor.SetMoveInput(input);
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            // During active combo: attempt cancel or buffer the input
            if (comboController != null && comboController.IsComboActive)
            {
                comboController.TryJumpCancel();
                return; // never fall through to raw jump during combo
            }

            if (motor != null)
                motor.RequestJump();
        }

        private void OnDash(InputAction.CallbackContext ctx)
        {
            if (motor == null) return;

            Vector2 dashDir = Vector2.zero;
            if (moveAction != null)
                dashDir = moveAction.action.ReadValue<Vector2>();

            // During active combo: attempt cancel or buffer the input
            if (comboController != null && comboController.IsComboActive)
            {
                comboController.TryDashCancel(dashDir);
                return; // never fall through to raw dash during combo
            }

            motor.RequestDash(dashDir);
        }

        private void OnLightAttack(InputAction.CallbackContext ctx)
        {
            if (comboController != null)
                comboController.RequestLightAttack();
        }

        private void OnHeavyAttack(InputAction.CallbackContext ctx)
        {
            if (comboController != null)
                comboController.RequestHeavyAttack();
        }

        private void OnRun(InputAction.CallbackContext ctx)
        {
            if (motor != null)
                motor.RequestRun();
        }

        private void OnAbility1(InputAction.CallbackContext ctx)
        {
            if (abilityExecutor != null)
                abilityExecutor.ActivateMainAbility();
        }

        private void OnAbility1Released(InputAction.CallbackContext ctx)
        {
            if (abilityExecutor != null)
                abilityExecutor.ReleaseMainAbility();
        }

        private void OnAbility2(InputAction.CallbackContext ctx)
        {
            if (abilityExecutor != null)
                abilityExecutor.ActivateSecondaryAbility();
        }

        private void OnAbility2Released(InputAction.CallbackContext ctx)
        {
            if (abilityExecutor != null)
                abilityExecutor.ReleaseSecondaryAbility();
        }

        private void EnableActions()
        {
            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            dashAction?.action.Enable();
            lightAttackAction?.action.Enable();
            heavyAttackAction?.action.Enable();
            runAction?.action.Enable();
            ability1Action?.action.Enable();
            ability2Action?.action.Enable();
        }

        /// <summary>
        /// InputActionReferences don't survive prefab serialization.
        /// When instantiated at runtime (e.g. via CharacterSpawner), self-wire from Resources.
        /// </summary>
        private void SelfWireInputActions()
        {
            var asset = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (asset == null)
            {
                Debug.LogError("[CharacterInputHandler] InputSystem_Actions not found in Resources. Copy it there.");
                return;
            }

            // Clone so each player instance has independent action state
            _runtimeAsset = Instantiate(asset);

            moveAction        = InputActionReference.Create(_runtimeAsset.FindAction("Player/Move"));
            jumpAction        = InputActionReference.Create(_runtimeAsset.FindAction("Player/Jump"));
            dashAction        = InputActionReference.Create(_runtimeAsset.FindAction("Player/Sprint"));
            lightAttackAction = InputActionReference.Create(_runtimeAsset.FindAction("Player/Attack"));
            heavyAttackAction = InputActionReference.Create(_runtimeAsset.FindAction("Player/Crouch"));

            var runActionFound = _runtimeAsset.FindAction("Player/Run");
            if (runActionFound != null)
                runAction = InputActionReference.Create(runActionFound);

            var ability1Found = _runtimeAsset.FindAction("Player/Ability1");
            if (ability1Found != null)
                ability1Action = InputActionReference.Create(ability1Found);

            var ability2Found = _runtimeAsset.FindAction("Player/Ability2");
            if (ability2Found != null)
                ability2Action = InputActionReference.Create(ability2Found);

            Debug.Log("[CharacterInputHandler] Self-wired input actions from Resources.");
        }

        private void OnDestroy()
        {
            if (_runtimeAsset != null)
                Destroy(_runtimeAsset);
        }
    }
}
