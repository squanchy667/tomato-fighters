using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Full-screen character selection overlay. Pauses the game until a character is chosen.
    /// Works with <see cref="CharacterSpawner"/> to spawn the selected character.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private CharacterSpawner spawner;
        [SerializeField] private CharacterRegistry registry;

        private bool _isSelecting = true;
        private Texture2D _whiteTexture;

        private static readonly (CharacterType type, string name, string role, string stats, Color color)[] CHARACTERS =
        {
            (CharacterType.Brutor,  "BRUTOR",  "Tank",  "HP:200 DEF:25 ATK:0.7\nPassive: Thick Skin (15% DR)", new Color(0.8f, 0.2f, 0.2f)),
            (CharacterType.Slasher, "SLASHER", "Melee", "HP:100 ATK:2.0 SPD:1.3\nPassive: Bloodlust (+3% ATK/hit)", new Color(0.9f, 0.6f, 0.1f)),
            (CharacterType.Mystica, "MYSTICA", "Mage",  "HP:50 MNA:150 ATK:0.5\nPassive: Arcane Resonance (+5%)", new Color(0.4f, 0.3f, 0.9f)),
            (CharacterType.Viper,   "VIPER",   "Range", "HP:80 SPD:1.1 ATK:1.8\nPassive: Distance Bonus (+2%/u)", new Color(0.2f, 0.8f, 0.3f)),
        };

        private void Start()
        {
            Time.timeScale = 0f;
            _isSelecting = true;
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void Update()
        {
            if (!_isSelecting) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectCharacter(CharacterType.Brutor);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectCharacter(CharacterType.Slasher);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectCharacter(CharacterType.Mystica);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectCharacter(CharacterType.Viper);
        }

        private void OnGUI()
        {
            if (!_isSelecting) return;

            // Dark overlay
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTexture);
            GUI.color = Color.white;

            // Title
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, 60), "CHOOSE YOUR FIGHTER", titleStyle);

            // Subtitle
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0, Screen.height * 0.08f + 55, Screen.width, 30), "Click a character or press 1-4", subStyle);

            // Character cards
            float cardWidth = Mathf.Min(220f, (Screen.width - 100f) / 4f);
            float cardHeight = 200f;
            float spacing = 20f;
            float totalWidth = 4 * cardWidth + 3 * spacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float cardY = Screen.height * 0.3f;

            for (int i = 0; i < CHARACTERS.Length; i++)
            {
                var (type, charName, role, stats, color) = CHARACTERS[i];
                float x = startX + i * (cardWidth + spacing);

                // Card background
                GUI.color = new Color(color.r, color.g, color.b, 0.3f);
                GUI.DrawTexture(new Rect(x, cardY, cardWidth, cardHeight), _whiteTexture);
                GUI.color = Color.white;

                // Card border
                DrawBorder(new Rect(x, cardY, cardWidth, cardHeight), color, 2);

                // Number key hint
                var keyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                keyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(x, cardY + 5, cardWidth, 20), $"[{i + 1}]", keyStyle);

                // Character name
                var nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                nameStyle.normal.textColor = color;
                GUI.Label(new Rect(x, cardY + 25, cardWidth, 35), charName, nameStyle);

                // Role
                var roleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic
                };
                roleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                GUI.Label(new Rect(x, cardY + 60, cardWidth, 25), role, roleStyle);

                // Stats
                var statsStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                statsStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
                GUI.Label(new Rect(x + 10, cardY + 90, cardWidth - 20, 80), stats, statsStyle);

                // Select button
                var btnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                if (GUI.Button(new Rect(x + 20, cardY + cardHeight - 40, cardWidth - 40, 30), "SELECT", btnStyle))
                {
                    SelectCharacter(type);
                }
            }

            // Controls preview
            var controlsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            controlsStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
            float controlsY = cardY + cardHeight + 40;
            GUI.Label(new Rect(0, controlsY, Screen.width, 20), "WASD: Move | Space: Jump | Shift: Dash | LMB: Light | C: Heavy | Ctrl: Run", controlsStyle);
            GUI.Label(new Rect(0, controlsY + 22, Screen.width, 20), "Press 1-4 during gameplay to switch characters", controlsStyle);
        }

        private void DrawBorder(Rect rect, Color color, int thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _whiteTexture); // top
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), _whiteTexture); // bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _whiteTexture); // left
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), _whiteTexture); // right
            GUI.color = Color.white;
        }

        private void SelectCharacter(CharacterType type)
        {
            _isSelecting = false;
            Time.timeScale = 1f;

            if (spawner != null)
            {
                spawner.SwitchCharacter(type);
                Debug.Log($"[CharacterSelectUI] Selected {type}. Game started!");
            }
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
                Destroy(_whiteTexture);
        }
    }
}
