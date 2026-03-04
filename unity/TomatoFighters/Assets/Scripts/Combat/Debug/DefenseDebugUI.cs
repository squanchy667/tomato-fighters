using TomatoFighters.Shared.Data;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Floating world-space debug UI for defense events. Spawns color-coded
    /// TextMesh labels that float up and fade on deflect/clash/dodge.
    /// Also shows a persistent label with the current defense state.
    /// </summary>
    public class DefenseDebugUI : MonoBehaviour
    {
        [SerializeField] private DefenseSystem defenseSystem;

        private static readonly Color DEFLECT_COLOR = new Color(0.2f, 1f, 0.2f); // green
        private static readonly Color CLASH_COLOR = new Color(1f, 1f, 0.2f);     // yellow
        private static readonly Color DODGE_COLOR = new Color(0.2f, 1f, 1f);     // cyan

        private const float FLOAT_DISTANCE = 0.5f;
        private const float FADE_DURATION = 1f;
        private const int FONT_SIZE = 50;
        private const float CHAR_SIZE = 0.12f;

        private TextMesh _stateLabel;

        private void Start()
        {
            CreateStateLabel();
        }

        private void OnEnable()
        {
            if (defenseSystem == null) return;
            defenseSystem.OnDeflect += HandleDeflect;
            defenseSystem.OnClash += HandleClash;
            defenseSystem.OnDodge += HandleDodge;
        }

        private void OnDisable()
        {
            if (defenseSystem == null) return;
            defenseSystem.OnDeflect -= HandleDeflect;
            defenseSystem.OnClash -= HandleClash;
            defenseSystem.OnDodge -= HandleDodge;
        }

        private void Update()
        {
            if (_stateLabel != null && defenseSystem != null)
            {
                _stateLabel.text = defenseSystem.CurrentState != DefenseState.None
                    ? $"[{defenseSystem.CurrentState}]"
                    : "";
                _stateLabel.transform.position = transform.position + new Vector3(0f, 1.2f, 0f);
            }
        }

        private void HandleDeflect(DeflectEventData data)
        {
            SpawnFloatingText("DEFLECTED!", DEFLECT_COLOR);
        }

        private void HandleClash(ClashEventData data)
        {
            SpawnFloatingText("CLASHED!", CLASH_COLOR);
        }

        private void HandleDodge(DodgeEventData data)
        {
            SpawnFloatingText("DODGED!", DODGE_COLOR);
        }

        private void SpawnFloatingText(string text, Color color)
        {
            var go = new GameObject("DefensePopup");
            go.transform.position = transform.position + new Vector3(0f, 0.8f, 0f);

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = FONT_SIZE;
            tm.characterSize = CHAR_SIZE;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            go.AddComponent<FloatingFadeText>();
        }

        private void CreateStateLabel()
        {
            var go = new GameObject("DefenseStateLabel");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            _stateLabel = go.AddComponent<TextMesh>();
            _stateLabel.fontSize = 20;
            _stateLabel.characterSize = 0.06f;
            _stateLabel.anchor = TextAnchor.MiddleCenter;
            _stateLabel.alignment = TextAlignment.Center;
            _stateLabel.color = new Color(1f, 1f, 1f, 0.6f);
        }
    }

    /// <summary>
    /// Animates a TextMesh: floats upward and fades alpha to zero, then self-destructs.
    /// </summary>
    internal class FloatingFadeText : MonoBehaviour
    {
        private float _elapsed;
        private TextMesh _textMesh;
        private Color _startColor;
        private Vector3 _startPosition;

        private void Awake()
        {
            _textMesh = GetComponent<TextMesh>();
            _startColor = _textMesh.color;
            _startPosition = transform.position;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / 1f; // 1 second duration

            // Float up
            transform.position = _startPosition + new Vector3(0f, t * 0.5f, 0f);

            // Fade out
            _textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, 1f - t);

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
