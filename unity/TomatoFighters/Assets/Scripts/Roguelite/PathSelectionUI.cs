using TomatoFighters.Paths;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Selection mode for the path upgrade shrine.
    /// </summary>
    public enum PathSelectionMode { Main, Secondary }

    /// <summary>
    /// Full-screen upgrade shrine UI. Displays path cards with stat bonuses and T1 ability
    /// previews. Two-step selection: click a card (or press 1-3) to highlight, then CONFIRM
    /// to lock the path. Pauses the game while open.
    ///
    /// <para>Call <see cref="Show"/> from a world event handler or test bootstrap.
    /// Does not subscribe to <c>IRunProgressionEvents</c> directly — that would cross
    /// pillar boundaries.</para>
    /// </summary>
    public class PathSelectionUI : MonoBehaviour
    {
        // -- Injected --------------------------------------------------------
        [SerializeField] private PathSystem pathSystem;
        [SerializeField] private PathData[] availablePaths; // All 3 paths for character

        [Header("Test")]
        [Tooltip("If true, automatically calls Show(Main) on Start. For test scenes only.")]
        [SerializeField] private bool showOnStart;

        // -- State -----------------------------------------------------------
        private bool _isSelecting;
        private PathSelectionMode _currentMode;
        private int _selectedIndex = -1;
        private Texture2D _whiteTexture;

        // -- Unity lifecycle -------------------------------------------------
        private void Start()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();

            if (showOnStart)
                Show(PathSelectionMode.Main);
        }

        private void Update()
        {
            if (!_isSelecting) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectPath(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectPath(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectPath(2);

            if (Input.GetKeyDown(KeyCode.Return) && _selectedIndex >= 0)
                ConfirmSelection();
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
                Destroy(_whiteTexture);
        }

        // -- Public API ------------------------------------------------------

        /// <summary>
        /// Opens the path selection overlay. Pauses the game via <c>Time.timeScale = 0</c>.
        /// </summary>
        /// <param name="mode">
        /// <see cref="PathSelectionMode.Main"/> shows all 3 paths.
        /// <see cref="PathSelectionMode.Secondary"/> greys out the already-selected main path.
        /// </param>
        public void Show(PathSelectionMode mode)
        {
            _currentMode = mode;
            _isSelecting = true;
            _selectedIndex = -1;
            Time.timeScale = 0f;
        }

        // -- OnGUI -----------------------------------------------------------
        private void OnGUI()
        {
            if (!_isSelecting) return;
            if (availablePaths == null || availablePaths.Length == 0) return;

            DrawOverlay();
            DrawTitle();
            DrawSubtitle();
            DrawPathCards();
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

            string title = _currentMode == PathSelectionMode.Main
                ? "CHOOSE YOUR PATH"
                : "CHOOSE SECONDARY PATH";

            GUI.Label(new Rect(0, Screen.height * 0.06f, Screen.width, 60), title, style);
        }

        private void DrawSubtitle()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.5f);

            string subtitle = _currentMode == PathSelectionMode.Main
                ? "Select your main upgrade path. This choice is permanent."
                : "Select a secondary path. Your main path is locked.";

            GUI.Label(new Rect(0, Screen.height * 0.06f + 55, Screen.width, 30), subtitle, style);
        }

        private void DrawPathCards()
        {
            int count = availablePaths.Length;
            float cardWidth = Mathf.Min(260f, (Screen.width - 80f) / count);
            float cardHeight = 280f;
            float spacing = 20f;
            float totalWidth = count * cardWidth + (count - 1) * spacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float cardY = Screen.height * 0.22f;

            for (int i = 0; i < count; i++)
            {
                var pathData = availablePaths[i];
                if (pathData == null) continue;

                float x = startX + i * (cardWidth + spacing);
                var cardRect = new Rect(x, cardY, cardWidth, cardHeight);
                bool isMainPathInSecondary = _currentMode == PathSelectionMode.Secondary
                    && pathSystem != null
                    && pathSystem.MainPath != null
                    && pathData == pathSystem.MainPath;
                bool isHighlighted = _selectedIndex == i;

                DrawSingleCard(cardRect, pathData, i, isMainPathInSecondary, isHighlighted);
            }
        }

        private void DrawSingleCard(Rect rect, PathData pathData, int index,
            bool isGreyedOut, bool isHighlighted)
        {
            Color pathColor = GetPathColor(pathData.pathType);

            // Card background
            if (isGreyedOut)
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
            else
                GUI.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.3f);
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = Color.white;

            // Border — brighter when highlighted
            if (isHighlighted)
                DrawBorder(rect, pathColor, 3);
            else if (!isGreyedOut)
                DrawBorder(rect, new Color(pathColor.r, pathColor.g, pathColor.b, 0.5f), 2);
            else
                DrawBorder(rect, new Color(0.4f, 0.4f, 0.4f, 0.3f), 1);

            float contentX = rect.x + 10;
            float contentW = rect.width - 20;
            float yOffset = rect.y + 8;

            // Key hint
            var keyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            keyStyle.normal.textColor = isGreyedOut
                ? new Color(1f, 1f, 1f, 0.2f)
                : new Color(1f, 1f, 1f, 0.4f);
            GUI.Label(new Rect(contentX, yOffset, contentW, 20), $"[{index + 1}]", keyStyle);
            yOffset += 22;

            // Path name
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            nameStyle.normal.textColor = isGreyedOut
                ? new Color(0.5f, 0.5f, 0.5f, 0.5f)
                : pathColor;
            GUI.Label(new Rect(contentX, yOffset, contentW, 30), pathData.pathType.ToString(), nameStyle);
            yOffset += 32;

            // "LOCKED" label for greyed-out main path
            if (isGreyedOut)
            {
                var lockedStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic
                };
                lockedStyle.normal.textColor = new Color(1f, 0.4f, 0.4f, 0.6f);
                GUI.Label(new Rect(contentX, yOffset, contentW, 20), "(Main Path - Locked)", lockedStyle);
                return; // Don't render the rest for greyed-out cards
            }

            // Description
            var descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            descStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(contentX, yOffset, contentW, 55), pathData.description, descStyle);
            yOffset += 58;

            // T1 Ability
            var abilityLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            abilityLabelStyle.normal.textColor = new Color(1f, 0.85f, 0.2f, 0.7f);
            GUI.Label(new Rect(contentX, yOffset, contentW, 18), "T1 ABILITY", abilityLabelStyle);
            yOffset += 18;

            var abilityStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            abilityStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(contentX, yOffset, contentW, 22), FormatAbilityName(pathData.tier1AbilityId), abilityStyle);
            yOffset += 26;

            // Stat bonuses
            var statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            statStyle.normal.textColor = new Color(0.4f, 1f, 0.5f, 0.9f);
            GUI.Label(new Rect(contentX, yOffset, contentW, 60), FormatStatBonuses(pathData.tier1Bonuses), statStyle);
            yOffset += 62;

            // Select button (only if not greyed out)
            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(rect.x + 20, rect.y + rect.height - 38, rect.width - 40, 30),
                isHighlighted ? "SELECTED" : "SELECT", btnStyle))
            {
                SelectPath(index);
            }
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

            // Hint below button
            if (!canConfirm)
            {
                var hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };
                hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
                GUI.Label(new Rect(btnX - 50, btnY + btnHeight + 5, btnWidth + 100, 20),
                    "Select a path first", hintStyle);
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
            float y = Screen.height * 0.92f;
            GUI.Label(new Rect(0, y, Screen.width, 20),
                "Press 1-3 to select | Enter to confirm", style);
        }

        // -- Selection logic -------------------------------------------------
        private void SelectPath(int index)
        {
            if (index < 0 || index >= availablePaths.Length) return;
            if (availablePaths[index] == null) return;

            // Prevent selecting the greyed-out main path in secondary mode
            if (_currentMode == PathSelectionMode.Secondary
                && pathSystem != null
                && pathSystem.MainPath != null
                && availablePaths[index] == pathSystem.MainPath)
            {
                return;
            }

            _selectedIndex = index;
        }

        private void ConfirmSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= availablePaths.Length) return;

            var selectedPath = availablePaths[_selectedIndex];
            bool success;

            if (_currentMode == PathSelectionMode.Main)
                success = pathSystem.SelectMainPath(selectedPath);
            else
                success = pathSystem.SelectSecondaryPath(selectedPath);

            if (success)
            {
                Debug.Log($"[PathSelectionUI] Confirmed {_currentMode} path: {selectedPath.pathType}");
                _isSelecting = false;
                Time.timeScale = 1f;
            }
            else
            {
                Debug.LogWarning($"[PathSelectionUI] PathSystem rejected {_currentMode} selection: {selectedPath.pathType}");
            }
        }

        // -- Helpers ---------------------------------------------------------
        private string FormatStatBonuses(PathTierBonuses bonuses)
        {
            var sb = new System.Text.StringBuilder();
            if (bonuses.healthBonus != 0)        sb.AppendLine($"+{bonuses.healthBonus} HP");
            if (bonuses.defenseBonus != 0)       sb.AppendLine($"+{bonuses.defenseBonus} DEF");
            if (bonuses.attackBonus != 0)        sb.AppendLine($"+{bonuses.attackBonus:F1} ATK");
            if (bonuses.rangedAttackBonus != 0)  sb.AppendLine($"+{bonuses.rangedAttackBonus:F1} RATK");
            if (bonuses.speedBonus != 0)         sb.AppendLine($"+{bonuses.speedBonus:F1} SPD");
            if (bonuses.manaBonus != 0)          sb.AppendLine($"+{bonuses.manaBonus} MNA");
            if (bonuses.manaRegenBonus != 0)     sb.AppendLine($"+{bonuses.manaRegenBonus:F1} MRG");
            if (bonuses.critChanceBonus != 0)    sb.AppendLine($"+{bonuses.critChanceBonus * 100:F0}% CRIT");
            if (bonuses.stunRateBonus != 0)      sb.AppendLine($"+{bonuses.stunRateBonus:F1} PRS");
            return sb.ToString().TrimEnd();
        }

        private string FormatAbilityName(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId)) return "\u2014";
            int underscore = abilityId.IndexOf('_');
            return underscore >= 0 ? abilityId.Substring(underscore + 1) : abilityId;
        }

        private static Color GetPathColor(PathType type) => type switch
        {
            PathType.Warden      => new Color(0.9f, 0.3f, 0.2f),
            PathType.Bulwark     => new Color(0.6f, 0.6f, 0.8f),
            PathType.Guardian    => new Color(0.3f, 0.8f, 0.9f),
            PathType.Executioner => new Color(0.8f, 0.15f, 0.15f),
            PathType.Reaper      => new Color(0.7f, 0.2f, 0.5f),
            PathType.Shadow      => new Color(0.5f, 0.3f, 0.7f),
            PathType.Sage        => new Color(0.3f, 0.9f, 0.5f),
            PathType.Enchanter   => new Color(0.6f, 0.4f, 0.9f),
            PathType.Conjurer    => new Color(0.9f, 0.5f, 0.2f),
            PathType.Marksman    => new Color(0.2f, 0.7f, 0.3f),
            PathType.Trapper     => new Color(0.65f, 0.5f, 0.2f),
            PathType.Arcanist    => new Color(0.4f, 0.6f, 0.9f),
            _                    => Color.white,
        };

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
