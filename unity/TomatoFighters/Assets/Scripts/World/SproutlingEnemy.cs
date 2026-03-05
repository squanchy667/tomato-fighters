using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Sproutling companion summoned by Conjurer path. Derives from <see cref="EnemyBase"/>
    /// but targets the enemy layer instead of the player layer (DD-7).
    /// HP: 40, lifetime: 20s. Max 2 active, tracked by SummonSproutling ability.
    /// </summary>
    public class SproutlingEnemy : EnemyBase
    {
        private float _lifetime = 20f;
        private float _timer;

        protected override void Awake()
        {
            base.Awake();
            _timer = 0f;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _lifetime)
            {
                Debug.Log("[SproutlingEnemy] Lifetime expired — despawning");
                Destroy(gameObject);
            }
        }

        /// <summary>Set the max lifetime for this sproutling.</summary>
        public void SetLifetime(float seconds)
        {
            _lifetime = seconds;
        }
    }
}
