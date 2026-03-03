using TomatoFighters.Shared.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace TomatoFighters.Shared.Components
{
    /// <summary>
    /// Temp debug HP bar. Add to any GameObject with <see cref="IDamageable"/>.
    /// Auto-creates a world-space canvas above the sprite. Replaced by T025 HUD.
    /// </summary>
    public class DebugHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 1.2f, 0f);
        [SerializeField] private Vector2 barSize = new(1f, 0.12f);
        [SerializeField] private Color fillColor = Color.green;
        [SerializeField] private Color bgColor = new(0.2f, 0.2f, 0.2f, 0.8f);

        private IDamageable _damageable;
        private Image _fill;

        private void Awake()
        {
            _damageable = GetComponent<IDamageable>()
                          ?? GetComponentInParent<IDamageable>();

            if (_damageable == null)
            {
                Debug.LogWarning($"[DebugHealthBar] No IDamageable on {name}. Disabling.");
                enabled = false;
                return;
            }

            Debug.Log($"[DebugHealthBar] Found IDamageable on '{name}' — HP: {_damageable.CurrentHealth}/{_damageable.MaxHealth}");
            BuildBar();
        }

        private void LateUpdate()
        {
            if (_damageable == null || _damageable.MaxHealth <= 0f) return;
            _fill.fillAmount = _damageable.CurrentHealth / _damageable.MaxHealth;
        }

        private void BuildBar()
        {
            var canvasGO = new GameObject("DebugHP_Canvas");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = offset;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = barSize;
            rt.localScale = Vector3.one;

            // Background
            var bgGO = new GameObject("BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = bgColor;
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgRt.anchoredPosition = Vector2.zero;

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(canvasGO.transform, false);
            _fill = fillGO.AddComponent<Image>();
            _fill.color = fillColor;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillAmount = 1f;
            var fillRt = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
        }
    }
}
