using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Shared.Components
{
    /// <summary>
    /// Reusable projectile base class. Handles travel via Rigidbody2D, collision detection
    /// against a target layer, and lifetime expiry. Subclasses override
    /// <see cref="OnTargetHit"/> to apply ability-specific effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class ProjectileBase : MonoBehaviour
    {
        [SerializeField] protected float speed = 15f;
        [SerializeField] protected float lifetime = 3f;
        [SerializeField] protected LayerMask targetLayer;

        protected Rigidbody2D rb;
        protected float timer;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        protected virtual void Start()
        {
            rb.linearVelocity = transform.right * speed;
        }

        protected virtual void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                OnLifetimeExpired();
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & targetLayer) == 0) return;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                OnTargetHit(damageable, other);
            }
        }

        /// <summary>Called when the projectile hits a valid target.</summary>
        protected virtual void OnTargetHit(IDamageable target, Collider2D collider) { }

        /// <summary>Called when lifetime expires. Destroys by default.</summary>
        protected virtual void OnLifetimeExpired()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Initialize projectile direction and speed. Call after instantiation.
        /// </summary>
        public void Initialize(Vector2 direction, float overrideSpeed = -1f)
        {
            transform.right = direction;
            if (overrideSpeed > 0f) speed = overrideSpeed;
        }
    }
}
