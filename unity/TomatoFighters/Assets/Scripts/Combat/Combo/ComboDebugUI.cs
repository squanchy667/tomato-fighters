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

        // Precomputed combo routes for reference panel
        private readonly List<string> comboRoutes = new List<string>();

        // Last finisher info — persists after state resets to Idle
        private string lastFinisherText = "";
        private float lastFinisherTimer;

        private void Awake()
        {
            comboController = GetComponent<ComboController>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                baseColor = spriteRenderer.color;

            hasAnimator = GetComponent<Animator>() != null;
        }

        private void Start()
        {
            BuildComboRoutes();
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

            // Finisher banner — persists 3s after state resets to Idle
            if (lastFinisherTimer > 0f)
            {
                y += 10f;
                var finisherStyle = new GUIStyle(style) { fontSize = 22 };
                finisherStyle.normal.textColor = new Color(1f, 0.3f, 0.9f);
                GUI.Label(new Rect(10, y, 500, 30), lastFinisherText, finisherStyle);
                y += 30f;
            }

            if (eventTextTimer > 0f)
            {
                var eventStyle = new GUIStyle(style);
                eventStyle.fontSize = 24;
                eventStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(10, y + 10, 400, 40), lastEventText, eventStyle);
            }

            // Right panel — combo routes reference
            if (comboRoutes.Count > 0)
            {
                float rx = Screen.width - 520;
                float ry = 10f;

                var headerStyle = new GUIStyle(style) { fontSize = 16 };
                headerStyle.normal.textColor = Color.cyan;
                GUI.Label(new Rect(rx, ry, 500, 25), "COMBO ROUTES:", headerStyle);
                ry += 25f;

                var routeStyle = new GUIStyle(style) { fontSize = 13, fontStyle = FontStyle.Normal };
                routeStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

                foreach (string route in comboRoutes)
                {
                    GUI.Label(new Rect(rx, ry, 500, 20), route, routeStyle);
                    ry += 18f;
                }
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

            // Clear previous chain and finisher when a new combo starts
            if (comboController.ComboLength <= 1)
            {
                comboChain.Clear();
                lastFinisherText = "";
                lastFinisherTimer = 0f;
            }

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

            lastFinisherText = $"FINISHER: {finisherName} ({comboLength} hits)";
            lastFinisherTimer = 3f;

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

            Debug.Log("[Combo] Combo dropped");
        }

        private void OnComboEnded()
        {
            autoAdvanceActive = false;
            // Don't overwrite — let the finisher name stay visible
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
            if (lastFinisherTimer > 0f)
                lastFinisherTimer -= Time.deltaTime;
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

        /// <summary>
        /// Walks the combo tree and builds all possible routes from root to finisher/dead-end.
        /// Called once on Start. Handles cycles via visited set.
        /// </summary>
        private void BuildComboRoutes()
        {
            comboRoutes.Clear();
            var def = comboController?.Definition;
            if (def == null) return;

            if (def.IsValidStep(def.rootLightIndex))
            {
                TraceRoute(def, def.rootLightIndex,
                    new List<string>(), new HashSet<int>(), "[L]");
            }

            if (def.IsValidStep(def.rootHeavyIndex))
            {
                TraceRoute(def, def.rootHeavyIndex,
                    new List<string>(), new HashSet<int>(), "[H]");
            }
        }

        private void TraceRoute(ComboDefinition def, int stepIndex,
            List<string> path, HashSet<int> visited, string inputLabel)
        {
            if (!def.IsValidStep(stepIndex) || visited.Contains(stepIndex)) return;

            visited.Add(stepIndex);
            var step = def.steps[stepIndex];

            string name = GetAttackName(stepIndex);
            if (step.isFinisher)
                name = "★" + name;

            path.Add($"{inputLabel} {name}");

            bool isLeaf = step.isFinisher
                || (!def.IsValidStep(step.nextOnLight) && !def.IsValidStep(step.nextOnHeavy));

            if (isLeaf)
            {
                comboRoutes.Add(string.Join("  →  ", path));
            }
            else
            {
                if (def.IsValidStep(step.nextOnLight))
                    TraceRoute(def, step.nextOnLight,
                        new List<string>(path), new HashSet<int>(visited), "[L]");
                if (def.IsValidStep(step.nextOnHeavy))
                    TraceRoute(def, step.nextOnHeavy,
                        new List<string>(path), new HashSet<int>(visited), "[H]");
            }
        }
    }
}
