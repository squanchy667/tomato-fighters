using System.Collections.Generic;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Debug overlay that shows combo state, chain position, and input buffer.
    /// Attach to the same GameObject as <see cref="ComboController"/>.
    /// Flashes the sprite on attack steps for visual feedback without animations.
    /// Auto-advances combo windows when no Animator is present (simulates animation events).
    /// </summary>
    public class ComboDebugUI : MonoBehaviour
    {
        private ComboController comboController;
        private SpriteRenderer spriteRenderer;

        private Color baseColor;
        private Color flashColor;
        private float flashTimer;

        private const float FLASH_DURATION = 0.12f;
        private const float FINISHER_FLASH_DURATION = 0.25f;

        // Auto-advance: simulates animation events when no Animator is wired
        private const float AUTO_ADVANCE_DELAY = 0.2f;
        private const float AUTO_FINISHER_DELAY = 0.4f;
        private float autoAdvanceTimer;
        private bool autoAdvanceActive;
        private bool hasAnimator;

        private string lastEventText = "";
        private float eventTextTimer;

        // Combo chain history
        private const int MAX_CHAIN_DISPLAY = 8;
        private readonly List<string> comboChain = new List<string>();

        private void Awake()
        {
            comboController = GetComponent<ComboController>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                baseColor = spriteRenderer.color;

            hasAnimator = GetComponent<Animator>() != null;
        }

        private void OnEnable()
        {
            if (comboController == null) return;

            comboController.AttackStarted += OnAttackStarted;
            comboController.FinisherStarted += OnFinisherStarted;
            comboController.ComboDropped += OnComboDropped;
            comboController.ComboEnded += OnComboEnded;
        }

        private void OnDisable()
        {
            if (comboController == null) return;

            comboController.AttackStarted -= OnAttackStarted;
            comboController.FinisherStarted -= OnFinisherStarted;
            comboController.ComboDropped -= OnComboDropped;
            comboController.ComboEnded -= OnComboEnded;
        }

        private void Update()
        {
            UpdateAutoAdvance();
            UpdateFlash();
            UpdateEventText();
        }

        private void OnGUI()
        {
            if (comboController == null) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;

            float y = 10f;

            GUI.Label(new Rect(10, y, 400, 30), $"State: {comboController.CurrentState}", style);
            y += 25f;

            GUI.Label(new Rect(10, y, 400, 30), $"Step: {comboController.CurrentStepIndex}", style);
            y += 25f;

            GUI.Label(new Rect(10, y, 400, 30), $"Combo: {comboController.ComboLength} hits", style);
            y += 25f;

            if (comboController.Definition != null && comboController.CurrentStepIndex >= 0)
            {
                var step = comboController.Definition.steps[comboController.CurrentStepIndex];

                string attackName = step.attackData != null
                    ? step.attackData.attackName
                    : $"{step.attackType} #{comboController.CurrentStepIndex}";

                GUI.Label(new Rect(10, y, 400, 30), $"Attack: {attackName} (x{step.damageMultiplier:F1})", style);
                y += 25f;

                string branches = $"Next: L={step.nextOnLight} H={step.nextOnHeavy}";
                if (step.isFinisher) branches = "FINISHER";
                GUI.Label(new Rect(10, y, 400, 30), branches, style);
                y += 25f;
            }

            // Combo chain history
            if (comboChain.Count > 0)
            {
                y += 10f;
                var chainStyle = new GUIStyle(style) { fontSize = 14 };
                chainStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);

                GUI.Label(new Rect(10, y, 400, 25), "Chain:", chainStyle);
                y += 20f;
                GUI.Label(new Rect(10, y, 600, 25), string.Join(" → ", comboChain), chainStyle);
                y += 20f;
            }

            if (eventTextTimer > 0f)
            {
                var eventStyle = new GUIStyle(style);
                eventStyle.fontSize = 24;
                eventStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(10, y + 10, 400, 40), lastEventText, eventStyle);
            }
        }

        private void OnAttackStarted(AttackType type, int stepIndex)
        {
            flashColor = type == AttackType.Light
                ? new Color(1f, 1f, 0.3f)    // yellow flash for light
                : new Color(1f, 0.5f, 0.1f); // orange flash for heavy

            flashTimer = FLASH_DURATION;

            string attackName = GetAttackName(stepIndex);
            lastEventText = attackName;
            eventTextTimer = 1f;

            if (comboChain.Count >= MAX_CHAIN_DISPLAY)
                comboChain.RemoveAt(0);
            comboChain.Add(attackName);

            // Start auto-advance timer to simulate animation event
            autoAdvanceTimer = AUTO_ADVANCE_DELAY;
            autoAdvanceActive = true;

            Debug.Log($"[Combo] {attackName} — step {stepIndex}, chain {comboController.ComboLength}");
        }

        private void OnFinisherStarted(int comboLength)
        {
            flashColor = new Color(1f, 0.2f, 0.8f); // pink flash for finisher
            flashTimer = FINISHER_FLASH_DURATION;

            string finisherName = GetAttackName(comboController.CurrentStepIndex);
            lastEventText = $"FINISHER: {finisherName}! ({comboLength} hits)";
            eventTextTimer = 1.5f;

            if (comboChain.Count >= MAX_CHAIN_DISPLAY)
                comboChain.RemoveAt(0);
            comboChain.Add($"★{finisherName}");

            // Auto-end finisher after a short delay
            autoAdvanceTimer = AUTO_FINISHER_DELAY;
            autoAdvanceActive = true;

            Debug.Log($"[Combo] FINISHER: {finisherName} after {comboLength} hits!");
        }

        private void OnComboDropped()
        {
            autoAdvanceActive = false;
            lastEventText = "Combo dropped";
            eventTextTimer = 0.8f;
            comboChain.Clear();

            Debug.Log("[Combo] Combo dropped");
        }

        private void OnComboEnded()
        {
            autoAdvanceActive = false;
            lastEventText = "Combo complete!";
            eventTextTimer = 1f;
            comboChain.Clear();

            Debug.Log("[Combo] Combo ended cleanly");
        }

        /// <summary>
        /// Simulates animation events when no Animator is present.
        /// After a short delay in Attacking, calls OnComboWindowOpen.
        /// After a short delay in Finisher, calls OnFinisherEnd.
        /// </summary>
        private void UpdateAutoAdvance()
        {
            if (hasAnimator || !autoAdvanceActive || comboController == null) return;

            autoAdvanceTimer -= Time.deltaTime;
            if (autoAdvanceTimer > 0f) return;

            autoAdvanceActive = false;

            if (comboController.CurrentState == ComboState.Attacking)
            {
                comboController.OnComboWindowOpen();
            }
            else if (comboController.CurrentState == ComboState.Finisher)
            {
                comboController.OnFinisherEnd();
            }
        }

        private void UpdateFlash()
        {
            if (spriteRenderer == null || flashTimer <= 0f) return;

            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                spriteRenderer.color = baseColor;
            }
            else
            {
                float t = flashTimer / FLASH_DURATION;
                spriteRenderer.color = Color.Lerp(baseColor, flashColor, t);
            }
        }

        private void UpdateEventText()
        {
            if (eventTextTimer > 0f)
                eventTextTimer -= Time.deltaTime;
        }

        private string GetAttackName(int stepIndex)
        {
            if (comboController.Definition == null || !comboController.Definition.IsValidStep(stepIndex))
                return $"Step {stepIndex}";

            var step = comboController.Definition.steps[stepIndex];
            if (step.attackData != null && !string.IsNullOrEmpty(step.attackData.attackName))
                return step.attackData.attackName;

            return $"{step.attackType} #{stepIndex}";
        }
    }
}
