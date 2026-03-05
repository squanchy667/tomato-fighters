using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Events;
using UnityEngine;

namespace TomatoFighters.Shared.Components
{
    /// <summary>
    /// Tracks current mana at runtime. Handles passive regen and fires
    /// <see cref="FloatEventChannel"/> on every change so the HUD (World pillar)
    /// and ability systems (Combat pillar) stay decoupled.
    /// </summary>
    public class PlayerManaTracker : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CharacterBaseStats baseStats;

        [Header("Events")]
        [SerializeField]
        [Tooltip("Fires with normalized mana (0-1) whenever current mana changes.")]
        private FloatEventChannel onManaChanged;

        /// <summary>Current mana value.</summary>
        public float CurrentMana { get; private set; }

        /// <summary>Maximum mana from base stats.</summary>
        public float MaxMana => baseStats != null ? baseStats.mana : 100f;

        /// <summary>Mana regen per second from base stats.</summary>
        public float ManaRegen => baseStats != null ? baseStats.manaRegen : 3f;

        private void Awake()
        {
            if (baseStats == null)
            {
                Debug.LogError("[PlayerManaTracker] No CharacterBaseStats assigned.", this);
                return;
            }

            CurrentMana = MaxMana;
        }

        private void Start()
        {
            // Fire initial state so HUD can set up
            FireChanged();
        }

        private void Update()
        {
            if (CurrentMana >= MaxMana) return;

            float previous = CurrentMana;
            CurrentMana = Mathf.Min(CurrentMana + ManaRegen * Time.deltaTime, MaxMana);

            // Only fire event when the rounded display value would change
            if (Mathf.Abs(CurrentMana - previous) > 0.01f)
            {
                FireChanged();
            }
        }

        /// <summary>
        /// Try to consume mana. Returns true if sufficient mana was available.
        /// </summary>
        public bool TryConsume(float amount)
        {
            if (amount <= 0f) return true;
            if (CurrentMana < amount) return false;

            CurrentMana -= amount;
            FireChanged();
            return true;
        }

        /// <summary>
        /// Restore mana by a flat amount. Clamped to max.
        /// </summary>
        public void Restore(float amount)
        {
            if (amount <= 0f) return;

            CurrentMana = Mathf.Min(CurrentMana + amount, MaxMana);
            FireChanged();
        }

        private void FireChanged()
        {
            if (onManaChanged != null)
            {
                float normalized = MaxMana > 0f ? CurrentMana / MaxMana : 0f;
                onManaChanged.Raise(normalized);
            }
        }
    }
}
