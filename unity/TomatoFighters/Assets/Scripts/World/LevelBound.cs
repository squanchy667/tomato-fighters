using TomatoFighters.Shared.Events;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Invisible trigger boundary placed at the edge of a combat area.
    /// When the player crosses it, fires a <see cref="VoidEventChannel"/> that
    /// WaveManager subscribes to for starting waves and locking the camera.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelBound : MonoBehaviour
    {
        [Header("Event")]
        [Tooltip("SO event channel raised when the player enters this bound. WaveManager listens.")]
        [SerializeField] private VoidEventChannel onBoundReached;

        private bool _hasFired;

        // ── Trigger Detection ───────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasFired) return;
            if (!other.CompareTag("Player")) return;

            _hasFired = true;
            onBoundReached.Raise();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Resets the bound so it can fire again. Call when reusing an area
        /// (e.g. backtracking or branching paths that reuse geometry).
        /// </summary>
        public void ResetBound()
        {
            _hasFired = false;
        }

        // ── Validation ──────────────────────────────────────────────────

        private void OnValidate()
        {
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[LevelBound] Collider on '{name}' set to isTrigger automatically.", this);
            }
        }

        // ── Gizmos ──────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = _hasFired ? new Color(0.5f, 0f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f,
                _hasFired ? "BOUND (fired)" : "BOUND (ready)");
#endif
        }
    }
}
