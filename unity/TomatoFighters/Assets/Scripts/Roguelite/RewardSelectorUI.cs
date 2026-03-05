using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Events;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Post-area reward selection screen. When triggered, pauses gameplay and offers
    /// 2-3 ritual options plus currency alternatives. Player picks one, the selection
    /// fires through <see cref="RewardSelectedEventChannel"/>, and gameplay resumes.
    ///
    /// <para><b>Trigger:</b> Subscribe the <c>onShowRewardSelector</c> VoidEventChannel
    /// to an area-cleared event. A mediator MonoBehaviour bridges
    /// <c>IRunProgressionEvents.OnAreaCleared</c> → the channel, keeping this UI
    /// decoupled from the World pillar (DD-2).</para>
    ///
    /// <para><b>Pause:</b> Uses <c>Time.timeScale = 0</c> (DD-4). All UI input
    /// uses <c>unscaledDeltaTime</c>.</para>
    /// </summary>
    public class RewardSelectorUI : MonoBehaviour
    {
        // ── Injection ────────────────────────────────────────────────────────

        [Header("Event Channels")]
        [Tooltip("Raised externally (e.g. by a mediator) when an area is cleared. Triggers the reward screen.")]
        [SerializeField] private VoidEventChannel onShowRewardSelector;

        [Tooltip("Fired when the player selects a reward. Consumed by RitualSystem / CurrencyManager.")]
        [SerializeField] private RewardSelectedEventChannel onRewardSelected;

        [Header("Configuration")]
        [Tooltip("Tunable reward parameters (currency amounts, pool sizes, weights).")]
        [SerializeField] private RewardConfig rewardConfig;

        [Tooltip("All ritual SOs available in the current pool.")]
        [SerializeField] private RitualData[] ritualPool;

        [Tooltip("Reference to RitualSystem to query maxed rituals.")]
        [SerializeField] private RitualSystem ritualSystem;

        [Header("Runtime")]
        [Tooltip("Set to true by Soul Tree (T038) to show a 3rd ritual choice.")]
        /// <summary>When true, one additional ritual option is shown. Set by Soul Tree (T038).</summary>
        public bool hasThirdChoice;

        [Header("Test")]
        [Tooltip("If true, automatically shows the reward screen on Start. For test scenes only.")]
        [SerializeField] private bool showOnStart;

        [Tooltip("Current area index for currency scaling. Set by run progression.")]
        [SerializeField] private int currentAreaIndex;

        // ── State ────────────────────────────────────────────────────────────

        private bool _isShowing;
        private List<RewardOption> _options;
        private int _selectedIndex = -1;
        private RitualPoolSelector _poolSelector;
        private Texture2D _whiteTexture;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _poolSelector = new RitualPoolSelector();
        }

        private void OnEnable()
        {
            if (onShowRewardSelector != null)
                onShowRewardSelector.Register(Show);
        }

        private void OnDisable()
        {
            if (onShowRewardSelector != null)
                onShowRewardSelector.Unregister(Show);
        }

        private void Start()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();

            if (showOnStart)
                Show();
        }

        private void Update()
        {
            if (!_isShowing) return;

            // Keyboard selection using unscaled time
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectOption(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectOption(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectOption(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectOption(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectOption(4);

            if (Input.GetKeyDown(KeyCode.Return) && _selectedIndex >= 0)
                ConfirmSelection();
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
                Destroy(_whiteTexture);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Opens the reward selection screen. Pauses gameplay with <c>Time.timeScale = 0</c>.
        /// Generates ritual options from the pool and adds currency alternatives.
        /// </summary>
        public void Show()
        {
            _options = GenerateOptions();

            if (_options.Count == 0)
            {
                Debug.LogWarning("[RewardSelectorUI] No reward options available. Skipping reward screen.");
                return;
            }

            _selectedIndex = -1;
            _isShowing = true;
            Time.timeScale = 0f;
        }

        // ── Option generation ────────────────────────────────────────────────

        private List<RewardOption> GenerateOptions()
        {
            var options = new List<RewardOption>();

            // Ritual options
            int ritualCount = rewardConfig != null ? rewardConfig.baseRitualChoices : 2;
            if (hasThirdChoice) ritualCount++;

            var maxedRituals = GetMaxedRituals();
            var pool = ritualPool != null ? new List<RitualData>(ritualPool) : new List<RitualData>();
            var selectedRituals = _poolSelector.Select(pool, rewardConfig, maxedRituals, ritualCount);

            foreach (var ritual in selectedRituals)
                options.Add(RewardOption.FromRitual(ritual));

            // Currency alternatives
            if (rewardConfig != null)
            {
                int crystals = rewardConfig.GetCrystalReward(currentAreaIndex);
                options.Add(RewardOption.FromCurrency(CurrencyType.Crystals, crystals));

                int fruits = rewardConfig.GetImbuedFruitReward(currentAreaIndex);
                options.Add(RewardOption.FromCurrency(CurrencyType.ImbuedFruits, fruits));
            }
            else
            {
                // Fallback defaults when no config is assigned
                options.Add(RewardOption.FromCurrency(CurrencyType.Crystals, 25));
                options.Add(RewardOption.FromCurrency(CurrencyType.ImbuedFruits, 10));
            }

            return options;
        }

        /// <summary>
        /// Queries RitualSystem for rituals at max level (level 3).
        /// Returns empty set if RitualSystem is not assigned.
        /// </summary>
        private HashSet<RitualData> GetMaxedRituals()
        {
            // RitualSystem doesn't expose a "get maxed" method yet.
            // When it does, populate this set. For now, return empty.
            return new HashSet<RitualData>();
        }

        // ── Selection logic ──────────────────────────────────────────────────

        private void SelectOption(int index)
        {
            if (index < 0 || _options == null || index >= _options.Count) return;
            _selectedIndex = index;
        }

        private void ConfirmSelection()
        {
            if (_selectedIndex < 0 || _options == null || _selectedIndex >= _options.Count) return;

            var selected = _options[_selectedIndex];
            var eventData = selected.ToEventData();

            Debug.Log($"[RewardSelectorUI] Selected: {selected.displayName} ({selected.type})");

            _isShowing = false;
            Time.timeScale = 1f;

            if (onRewardSelected != null)
                onRewardSelected.Raise(eventData);
        }

        // ── OnGUI rendering ─────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_isShowing) return;
            if (_options == null || _options.Count == 0) return;

            DrawOverlay();
            DrawTitle();
            DrawHintText();
            DrawRewardCards();
            DrawConfirmButton();
            DrawKeyboardHints();
        }

        private void DrawOverlay()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawTitle()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.06f, Screen.width, 60), "CHOOSE YOUR REWARD", style);
        }

        private void DrawHintText()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0, Screen.height * 0.06f + 55, Screen.width, 30),
                "Pick one reward", style);
        }

        private void DrawRewardCards()
        {
            int count = _options.Count;
            float cardWidth = Mathf.Min(220f, (Screen.width - 80f) / count);
            float cardHeight = 240f;
            float spacing = 16f;
            float totalWidth = count * cardWidth + (count - 1) * spacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float cardY = Screen.height * 0.22f;

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * (cardWidth + spacing);
                var rect = new Rect(x, cardY, cardWidth, cardHeight);
                bool isHighlighted = _selectedIndex == i;

                DrawSingleCard(rect, _options[i], i, isHighlighted);
            }
        }

        private void DrawSingleCard(Rect rect, RewardOption option, int index, bool isHighlighted)
        {
            // Card background
            GUI.color = new Color(option.borderColor.r, option.borderColor.g, option.borderColor.b, 0.25f);
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = Color.white;

            // Border
            if (isHighlighted)
                DrawBorder(rect, option.borderColor, 3);
            else
                DrawBorder(rect, new Color(option.borderColor.r, option.borderColor.g, option.borderColor.b, 0.5f), 2);

            float contentX = rect.x + 10;
            float contentW = rect.width - 20;
            float yOffset = rect.y + 8;

            // Key hint
            var keyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            keyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
            GUI.Label(new Rect(contentX, yOffset, contentW, 20), $"[{index + 1}]", keyStyle);
            yOffset += 22;

            // Type badge
            var badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            badgeStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            string badge = option.type == RewardType.Ritual ? "RITUAL" : "CURRENCY";
            GUI.Label(new Rect(contentX, yOffset, contentW, 16), badge, badgeStyle);
            yOffset += 18;

            if (option.type == RewardType.Ritual)
                DrawRitualCardContent(contentX, contentW, ref yOffset, option);
            else
                DrawCurrencyCardContent(contentX, contentW, ref yOffset, option);

            // Select button
            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(rect.x + 20, rect.y + rect.height - 38, rect.width - 40, 30),
                isHighlighted ? "SELECTED" : "SELECT", btnStyle))
            {
                SelectOption(index);
            }
        }

        private void DrawRitualCardContent(float x, float w, ref float y, RewardOption option)
        {
            // Family color indicator
            var familyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            familyStyle.normal.textColor = option.borderColor;
            if (option.ritualData != null)
            {
                string familyText = option.ritualData.family.ToString();
                if (option.ritualData.isTwin)
                    familyText += $" / {option.ritualData.secondFamily}";
                GUI.Label(new Rect(x, y, w, 16), familyText, familyStyle);
            }
            y += 18;

            // Ritual name
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            nameStyle.normal.textColor = option.borderColor;
            GUI.Label(new Rect(x, y, w, 26), option.displayName, nameStyle);
            y += 28;

            // Description
            var descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            descStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(x, y, w, 50), option.description, descStyle);
            y += 52;

            // Trigger info
            if (option.ritualData != null)
            {
                var triggerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter
                };
                triggerStyle.normal.textColor = new Color(1f, 0.85f, 0.2f, 0.7f);
                GUI.Label(new Rect(x, y, w, 16),
                    $"Trigger: {option.ritualData.trigger}", triggerStyle);
                y += 18;
            }
        }

        private void DrawCurrencyCardContent(float x, float w, ref float y, RewardOption option)
        {
            // Currency name
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            nameStyle.normal.textColor = option.borderColor;
            GUI.Label(new Rect(x, y, w, 26), option.displayName, nameStyle);
            y += 30;

            // Amount
            var amountStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            amountStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            GUI.Label(new Rect(x, y, w, 40), $"+{option.currencyAmount}", amountStyle);
            y += 50;
        }

        private void DrawConfirmButton()
        {
            float btnWidth = 200f;
            float btnHeight = 40f;
            float btnX = (Screen.width - btnWidth) / 2f;
            float btnY = Screen.height * 0.82f;

            bool canConfirm = _selectedIndex >= 0;
            GUI.enabled = canConfirm;

            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(btnX, btnY, btnWidth, btnHeight), "CONFIRM", style))
            {
                ConfirmSelection();
            }
            GUI.enabled = true;

            if (!canConfirm)
            {
                var hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };
                hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
                GUI.Label(new Rect(btnX - 50, btnY + btnHeight + 5, btnWidth + 100, 20),
                    "Select a reward first", hintStyle);
            }
        }

        private void DrawKeyboardHints()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
            int maxKey = _options != null ? _options.Count : 5;
            GUI.Label(new Rect(0, Screen.height * 0.92f, Screen.width, 20),
                $"Press 1-{maxKey} to select | Enter to confirm", style);
        }

        // ── Draw helpers ─────────────────────────────────────────────────────

        private void DrawBorder(Rect rect, Color color, int thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), _whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), _whiteTexture);
            GUI.color = Color.white;
        }
    }
}
