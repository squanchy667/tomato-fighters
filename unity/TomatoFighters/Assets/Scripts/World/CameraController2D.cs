using System.Collections.Generic;
using TomatoFighters.Shared.Events;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Smooth side-scrolling camera that follows player(s) with configurable leading,
    /// respects dynamic level bounds, locks at combat boundaries via SO event channels,
    /// and zooms in on stun events. Designed for co-op framing (multiple targets).
    /// Communicates exclusively through ScriptableObject event channels — no cross-pillar references.
    /// </summary>
    public class CameraController2D : MonoBehaviour
    {
        // ── Follow Configuration ───────────────────────────────────

        [Header("Follow")]
        [Tooltip("Smooth damping time for camera follow (lower = snappier).")]
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothTime = 0.2f;

        [Tooltip("How far ahead the camera leads in the player's facing direction.")]
        [SerializeField] private float leadDistance = 2f;

        [Tooltip("How fast the lead offset interpolates toward the desired direction.")]
        [SerializeField] private float leadSpeed = 5f;

        [Tooltip("Vertical offset applied to the camera target position.")]
        [SerializeField] private float verticalOffset = 1f;

        // ── Co-op Framing ──────────────────────────────────────────

        [Header("Co-op Framing")]
        [Tooltip("Follow targets. Phase 1: single player. Co-op: multiple players.")]
        [SerializeField] private List<Transform> targets = new();

        [Tooltip("Minimum orthographic size when framing multiple targets.")]
        [SerializeField] private float minFramingSize = 4f;

        [Tooltip("Padding added around targets when calculating co-op framing size.")]
        [SerializeField] private float framingPadding = 2f;

        // ── Level Bounds ───────────────────────────────────────────

        [Header("Level Bounds")]
        [Tooltip("Minimum X position the camera can reach.")]
        [SerializeField] private float boundsMinX = -50f;

        [Tooltip("Maximum X position the camera can reach.")]
        [SerializeField] private float boundsMaxX = 50f;

        [Tooltip("Minimum Y position the camera can reach.")]
        [SerializeField] private float boundsMinY = -10f;

        [Tooltip("Maximum Y position the camera can reach.")]
        [SerializeField] private float boundsMaxY = 10f;

        // ── Zoom Configuration ─────────────────────────────────────

        [Header("Zoom")]
        [Tooltip("Default orthographic camera size.")]
        [SerializeField] private float defaultOrthoSize = 5f;

        [Tooltip("Orthographic size when zoomed in on a stunned enemy.")]
        [SerializeField] private float stunZoomSize = 3.5f;

        [Tooltip("Duration of the zoom transition in seconds.")]
        [SerializeField] private float zoomDuration = 0.3f;

        // ── SO Event Channels ──────────────────────────────────────

        [Header("Event Channels — Incoming")]
        [Tooltip("Lock camera at current position (combat boundary). Raised by WaveManager.")]
        [SerializeField] private VoidEventChannel onCameraLock;

        [Tooltip("Unlock camera to resume following. Raised by WaveManager.")]
        [SerializeField] private VoidEventChannel onCameraUnlock;

        [Tooltip("Enemy stunned — triggers zoom in. Raised by PressureSystem (Phase 3).")]
        [SerializeField] private VoidEventChannel onStunTriggered;

        [Tooltip("Enemy recovered from stun — triggers zoom out.")]
        [SerializeField] private VoidEventChannel onStunRecovered;

        // ── Runtime State ──────────────────────────────────────────

        private Camera _camera;
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _currentLead = Vector3.zero;
        private float _targetOrthoSize;
        private bool _isLocked;
        private Vector3 _lockPosition;

        // ── Public Properties ──────────────────────────────────────

        /// <summary>True when the camera is locked at a combat boundary.</summary>
        public bool IsLocked => _isLocked;

        /// <summary>Current follow targets.</summary>
        public IReadOnlyList<Transform> Targets => targets;

        // ── Unity Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = Camera.main;

            _targetOrthoSize = defaultOrthoSize;

            if (_camera != null)
                _camera.orthographicSize = defaultOrthoSize;
        }

        private void OnEnable()
        {
            if (onCameraLock != null)
                onCameraLock.Register(HandleCameraLock);
            if (onCameraUnlock != null)
                onCameraUnlock.Register(HandleCameraUnlock);
            if (onStunTriggered != null)
                onStunTriggered.Register(HandleStunTriggered);
            if (onStunRecovered != null)
                onStunRecovered.Register(HandleStunRecovered);
        }

        private void OnDisable()
        {
            if (onCameraLock != null)
                onCameraLock.Unregister(HandleCameraLock);
            if (onCameraUnlock != null)
                onCameraUnlock.Unregister(HandleCameraUnlock);
            if (onStunTriggered != null)
                onStunTriggered.Unregister(HandleStunTriggered);
            if (onStunRecovered != null)
                onStunRecovered.Unregister(HandleStunRecovered);
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            UpdateZoom();
            AutoDiscoverPlayer();

            if (targets.Count == 0) return;

            Vector3 desiredPosition = CalculateDesiredPosition();
            desiredPosition = ClampToBounds(desiredPosition);

            // Preserve Z so the camera stays at its rendering depth
            desiredPosition.z = transform.position.z;

            // Follow the player whether locked or not — bounds clamping
            // already constrains the camera to the combat zone.
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                smoothTime);
        }

        // ── Follow Calculation ─────────────────────────────────────

        private Vector3 CalculateDesiredPosition()
        {
            if (targets.Count == 1)
                return CalculateSingleTargetPosition(targets[0]);

            return CalculateMultiTargetPosition();
        }

        private Vector3 CalculateSingleTargetPosition(Transform target)
        {
            if (target == null) return transform.position;

            // Determine facing direction from target's local scale (flipped = facing left)
            float facingDir = target.localScale.x >= 0f ? 1f : -1f;
            Vector3 desiredLead = new Vector3(facingDir * leadDistance, 0f, 0f);

            // Smoothly interpolate the lead offset
            _currentLead = Vector3.Lerp(_currentLead, desiredLead, Time.deltaTime * leadSpeed);

            return new Vector3(
                target.position.x + _currentLead.x,
                target.position.y + verticalOffset,
                transform.position.z);
        }

        private Vector3 CalculateMultiTargetPosition()
        {
            // Remove null targets
            Vector3 center = Vector3.zero;
            int validCount = 0;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;

                Vector3 pos = targets[i].position;
                center += pos;
                validCount++;

                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            if (validCount == 0) return transform.position;

            center /= validCount;

            // Adjust orthographic size to frame all targets
            if (validCount > 1)
            {
                float spanX = maxX - minX;
                float spanY = maxY - minY;
                float requiredSize = Mathf.Max(spanX / _camera.aspect, spanY) * 0.5f + framingPadding;
                _targetOrthoSize = Mathf.Max(requiredSize, minFramingSize);
            }

            return new Vector3(center.x, center.y + verticalOffset, transform.position.z);
        }

        // ── Auto-Discovery ──────────────────────────────────────────

        /// <summary>
        /// Replaces null/destroyed targets with the current Player-tagged object.
        /// Runs each frame so the camera picks up the player after CharacterSpawner instantiates it.
        /// </summary>
        private void AutoDiscoverPlayer()
        {
            // Check if we already have a valid target
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && targets[i].CompareTag("Player"))
                    return;
            }

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO == null) return;

            // Replace first null slot, or add
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                {
                    targets[i] = playerGO.transform;
                    return;
                }
            }

            targets.Clear();
            targets.Add(playerGO.transform);
        }

        // ── Bounds ─────────────────────────────────────────────────

        private Vector3 ClampToBounds(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, boundsMinX, boundsMaxX);
            position.y = Mathf.Clamp(position.y, boundsMinY, boundsMaxY);
            return position;
        }

        // ── Zoom ───────────────────────────────────────────────────

        private void UpdateZoom()
        {
            if (Mathf.Approximately(_camera.orthographicSize, _targetOrthoSize)) return;

            // Smooth lerp toward target size — use unscaled so zoom works during hitstop
            float t = (zoomDuration > 0f)
                ? Time.unscaledDeltaTime / zoomDuration
                : 1f;

            _camera.orthographicSize = Mathf.Lerp(
                _camera.orthographicSize,
                _targetOrthoSize,
                t);
        }

        // ── Event Handlers ─────────────────────────────────────────

        private void HandleCameraLock()
        {
            _isLocked = true;
            _lockPosition = transform.position;
        }

        private void HandleCameraUnlock()
        {
            _isLocked = false;
        }

        private void HandleStunTriggered()
        {
            _targetOrthoSize = stunZoomSize;
        }

        private void HandleStunRecovered()
        {
            _targetOrthoSize = defaultOrthoSize;
        }

        // ── Public API ─────────────────────────────────────────────

        /// <summary>Add a follow target (e.g., when a player joins co-op).</summary>
        public void AddTarget(Transform target)
        {
            if (target != null && !targets.Contains(target))
                targets.Add(target);
        }

        /// <summary>Remove a follow target (e.g., when a player disconnects).</summary>
        public void RemoveTarget(Transform target)
        {
            targets.Remove(target);
        }

        /// <summary>
        /// Update the camera's level bounds dynamically.
        /// Called by area transitions or WaveManager via SO event bridge.
        /// </summary>
        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            boundsMinX = minX;
            boundsMaxX = maxX;
            boundsMinY = minY;
            boundsMaxY = maxY;
        }

        /// <summary>Snap camera to target position immediately (no smoothing). Use for scene transitions.</summary>
        public void SnapToTarget()
        {
            if (targets.Count == 0) return;

            Vector3 pos = CalculateDesiredPosition();
            pos = ClampToBounds(pos);
            pos.z = transform.position.z;
            transform.position = pos;
            _velocity = Vector3.zero;
        }

        // ── Gizmos ─────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            // Draw level bounds as a yellow rectangle
            Gizmos.color = Color.yellow;

            Vector3 bottomLeft = new Vector3(boundsMinX, boundsMinY, 0f);
            Vector3 bottomRight = new Vector3(boundsMaxX, boundsMinY, 0f);
            Vector3 topLeft = new Vector3(boundsMinX, boundsMaxY, 0f);
            Vector3 topRight = new Vector3(boundsMaxX, boundsMaxY, 0f);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);

            // Draw lock position if locked
            if (Application.isPlaying && _isLocked)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_lockPosition, 0.5f);
            }
        }
    }
}
