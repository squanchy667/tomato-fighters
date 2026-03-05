using TomatoFighters.Shared.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TomatoFighters.World.UI
{
    /// <summary>
    /// Player health bar. Subscribes to a <see cref="FloatEventChannel"/> fired by
    /// PlayerDamageable (Combat pillar) with normalized health (0-1).
    /// No direct reference to Combat code — fully decoupled via SO event channel.
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image damageFlashImage;

        [Header("Events")]
        [SerializeField] private FloatEventChannel onHealthChanged;

        [Header("Settings")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("Health ratio below which the bar turns critical color.")]
        private float criticalThreshold = 0.3f;
        [SerializeField] private float flashFadeDuration = 0.4f;

        private float _flashAlpha;

        private void OnEnable()
        {
            if (onHealthChanged != null)
                onHealthChanged.Register(HandleHealthChanged);
        }

        private void OnDisable()
        {
            if (onHealthChanged != null)
                onHealthChanged.Unregister(HandleHealthChanged);
        }

        private void Update()
        {
            // Fade out damage flash
            if (damageFlashImage != null && _flashAlpha > 0f)
            {
                _flashAlpha -= Time.deltaTime / flashFadeDuration;
                var c = damageFlashImage.color;
                c.a = Mathf.Max(_flashAlpha, 0f);
                damageFlashImage.color = c;
            }
        }

        private void HandleHealthChanged(float normalizedHealth)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = normalizedHealth;
                fillImage.color = normalizedHealth <= criticalThreshold ? criticalColor : healthyColor;
            }

            // Trigger damage flash
            if (damageFlashImage != null)
            {
                _flashAlpha = 1f;
                var c = damageFlashImage.color;
                c.a = 1f;
                damageFlashImage.color = c;
            }
        }
    }
}
