using UnityEngine;
using UnityEngine.UI;

namespace TomatoFighters.World.UI
{
    /// <summary>
    /// World-space health + pressure bar displayed above an enemy.
    /// Reads directly from the parent <see cref="EnemyBase"/> — same pillar, no boundary violation.
    /// Spawned by EnemyBase in Awake() from a prefab reference.
    /// </summary>
    public class EnemyHealthBarUI : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField]
        [Range(0f, 0.5f)]
        private float criticalThreshold = 0.3f;

        [Header("Pressure")]
        [SerializeField] private Image pressureFill;
        [SerializeField] private Color pressureColor = new Color(0.3f, 0.5f, 1f);
        [SerializeField] private Color stunnedColor = Color.yellow;

        private EnemyBase _enemy;

        /// <summary>
        /// Initialize with the enemy this bar tracks.
        /// </summary>
        public void Initialize(EnemyBase enemy)
        {
            _enemy = enemy;
        }

        private void LateUpdate()
        {
            if (_enemy == null)
            {
                Destroy(gameObject);
                return;
            }

            UpdateHealthBar();
            UpdatePressureBar();
        }

        private void UpdateHealthBar()
        {
            if (healthFill == null || _enemy.MaxHealth <= 0f) return;

            float ratio = Mathf.Clamp01(_enemy.CurrentHealth / _enemy.MaxHealth);
            healthFill.fillAmount = ratio;
            healthFill.color = ratio <= criticalThreshold ? criticalColor : healthyColor;
        }

        private void UpdatePressureBar()
        {
            if (pressureFill == null) return;

            float ratio = _enemy.PressureRatio;
            pressureFill.fillAmount = ratio;
            pressureFill.color = _enemy.IsStunned ? stunnedColor : pressureColor;
        }
    }
}
