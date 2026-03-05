using System.Collections;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Handles telegraph visual effects on enemies. Placed on the enemy root,
    /// references the SpriteRenderer on the sprite child. AttackState calls
    /// these through the EnemyAI context.
    /// </summary>
    public class TelegraphVisualController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;

        private Coroutine _activeTelegraph;
        private Color _originalColor;

        private void Awake()
        {
            if (_sprite == null)
                _sprite = GetComponentInChildren<SpriteRenderer>();

            if (_sprite != null)
                _originalColor = _sprite.color;
        }

        /// <summary>
        /// Play Normal telegraph: smooth white → yellow ramp over duration.
        /// </summary>
        public Coroutine PlayNormalTelegraph(float duration)
        {
            CancelTelegraph();
            _activeTelegraph = StartCoroutine(NormalTelegraphRoutine(duration));
            return _activeTelegraph;
        }

        /// <summary>
        /// Play Unstoppable telegraph: rapid red flashes over duration.
        /// </summary>
        public Coroutine PlayUnstoppableTelegraph(float duration)
        {
            CancelTelegraph();
            _activeTelegraph = StartCoroutine(UnstoppableTelegraphRoutine(duration));
            return _activeTelegraph;
        }

        /// <summary>
        /// Stop any active telegraph and reset sprite color.
        /// </summary>
        public void CancelTelegraph()
        {
            if (_activeTelegraph != null)
            {
                StopCoroutine(_activeTelegraph);
                _activeTelegraph = null;
            }

            if (_sprite != null)
                _sprite.color = _originalColor;
        }

        /// <summary>
        /// Flash the sprite red-orange during active hitbox frames, then restore.
        /// Called by AttackState during the swing phase.
        /// </summary>
        public void SetActiveSwingColor()
        {
            if (_sprite != null)
                _sprite.color = new Color(1f, 0.2f, 0f);
        }

        /// <summary>
        /// Restore the sprite to its original color.
        /// </summary>
        public void RestoreColor()
        {
            if (_sprite != null)
                _sprite.color = _originalColor;
        }

        private IEnumerator NormalTelegraphRoutine(float duration)
        {
            if (_sprite == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // White → yellow ramp (wind-up feel)
                _sprite.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0f), t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Brief white flash before strike
            _sprite.color = Color.white;
            yield return null;

            _activeTelegraph = null;
        }

        private IEnumerator UnstoppableTelegraphRoutine(float duration)
        {
            if (_sprite == null) yield break;

            // Rapid red/original blink (2–3 flashes), then stay red
            float elapsed = 0f;
            float flashInterval = duration / 6f; // ~3 full on/off cycles
            bool isRed = false;

            while (elapsed < duration)
            {
                isRed = !isRed;
                _sprite.color = isRed
                    ? new Color(1f, 0.15f, 0.15f)
                    : _originalColor;

                float wait = Mathf.Min(flashInterval, duration - elapsed);
                yield return new WaitForSeconds(wait);
                elapsed += wait;
            }

            // Stay red during active frames
            _sprite.color = new Color(1f, 0f, 0f);
            _activeTelegraph = null;
        }
    }
}
