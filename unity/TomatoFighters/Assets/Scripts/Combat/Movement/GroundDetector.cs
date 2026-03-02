using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Detects ground contact using Physics2D.OverlapBox at the character's feet.
    /// </summary>
    public class GroundDetector : MonoBehaviour
    {
        [SerializeField] private Vector2 boxSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private Vector2 boxOffset = new Vector2(0f, -0.5f);
        [SerializeField] private LayerMask groundLayer;

        /// <summary>Whether the character is currently touching the ground.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>True on the frame the character first touches the ground.</summary>
        public bool JustLanded { get; private set; }

        /// <summary>True on the frame the character leaves the ground.</summary>
        public bool JustLeftGround { get; private set; }

        private bool wasGrounded;

        private void FixedUpdate()
        {
            wasGrounded = IsGrounded;

            Vector2 origin = (Vector2)transform.position + boxOffset;
            IsGrounded = Physics2D.OverlapBox(origin, boxSize, 0f, groundLayer) != null;

            JustLanded = IsGrounded && !wasGrounded;
            JustLeftGround = !IsGrounded && wasGrounded;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 origin = (Vector2)transform.position + boxOffset;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(origin, boxSize);
        }
    }
}
