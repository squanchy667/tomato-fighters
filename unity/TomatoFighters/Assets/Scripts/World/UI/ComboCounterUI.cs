using TomatoFighters.Shared.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TomatoFighters.World.UI
{
    /// <summary>
    /// Combo counter display. Subscribes to an <see cref="IntEventChannel"/> fired by
    /// ComboController on hit-confirm. Shows hit count with a punch scale animation
    /// on each increment and fades out after a decay period.
    /// </summary>
    public class ComboCounterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text comboText;
        [SerializeField] private Text labelText;

        [Header("Events")]
        [SerializeField] private IntEventChannel onComboHitConfirmed;
        [SerializeField]
        [Tooltip("Subscribe to know when combo drops (fires with 0). Optional.")]
        private IntEventChannel onComboDropped;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Seconds after last hit before the counter fades out.")]
        private float decayTime = 2f;
        [SerializeField] private float punchScaleAmount = 1.3f;
        [SerializeField] private float punchScaleDuration = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private int _currentCount;
        private float _timeSinceLastHit;
        private float _alpha = 0f;
        private Vector3 _baseScale;
        private float _punchTimer;
        private bool _isVisible;

        private void Awake()
        {
            _baseScale = transform.localScale;
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            if (onComboHitConfirmed != null)
                onComboHitConfirmed.Register(HandleHitConfirmed);
            if (onComboDropped != null)
                onComboDropped.Register(HandleComboDropped);
        }

        private void OnDisable()
        {
            if (onComboHitConfirmed != null)
                onComboHitConfirmed.Unregister(HandleHitConfirmed);
            if (onComboDropped != null)
                onComboDropped.Unregister(HandleComboDropped);
        }

        private void Update()
        {
            if (!_isVisible) return;

            _timeSinceLastHit += Time.deltaTime;

            // Punch scale animation
            if (_punchTimer > 0f)
            {
                _punchTimer -= Time.deltaTime;
                float t = 1f - (_punchTimer / punchScaleDuration);
                float scale = Mathf.Lerp(punchScaleAmount, 1f, t);
                transform.localScale = _baseScale * scale;
            }
            else
            {
                transform.localScale = _baseScale;
            }

            // Decay and fade
            if (_timeSinceLastHit >= decayTime)
            {
                float fadeT = (_timeSinceLastHit - decayTime) / fadeOutDuration;
                _alpha = Mathf.Lerp(1f, 0f, fadeT);
                SetAlpha(_alpha);

                if (_alpha <= 0f)
                {
                    _isVisible = false;
                    _currentCount = 0;
                }
            }
        }

        private void HandleHitConfirmed(int comboLength)
        {
            _currentCount = comboLength;
            _timeSinceLastHit = 0f;
            _punchTimer = punchScaleDuration;
            _isVisible = true;
            _alpha = 1f;

            SetAlpha(1f);

            if (comboText != null)
                comboText.text = _currentCount.ToString();
        }

        private void HandleComboDropped(int _)
        {
            // Start immediate fade
            _timeSinceLastHit = decayTime;
        }

        private void SetAlpha(float a)
        {
            if (comboText != null)
            {
                var c = comboText.color;
                c.a = a;
                comboText.color = c;
            }

            if (labelText != null)
            {
                var c = labelText.color;
                c.a = a;
                labelText.color = c;
            }
        }
    }
}
