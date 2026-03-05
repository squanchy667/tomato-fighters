using TomatoFighters.Shared.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TomatoFighters.World.UI
{
    /// <summary>
    /// Player mana bar. Subscribes to a <see cref="FloatEventChannel"/> fired by
    /// PlayerManaTracker (Shared) with normalized mana (0-1).
    /// </summary>
    public class ManaBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;

        [Header("Events")]
        [SerializeField] private FloatEventChannel onManaChanged;

        [Header("Settings")]
        [SerializeField] private Color fullColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color lowColor = new Color(0.1f, 0.2f, 0.5f);
        [SerializeField]
        [Range(0f, 0.5f)]
        private float lowThreshold = 0.2f;

        private void OnEnable()
        {
            if (onManaChanged != null)
                onManaChanged.Register(HandleManaChanged);
        }

        private void OnDisable()
        {
            if (onManaChanged != null)
                onManaChanged.Unregister(HandleManaChanged);
        }

        private void HandleManaChanged(float normalizedMana)
        {
            if (fillImage == null) return;

            fillImage.fillAmount = normalizedMana;
            fillImage.color = normalizedMana <= lowThreshold ? lowColor : fullColor;
        }
    }
}
