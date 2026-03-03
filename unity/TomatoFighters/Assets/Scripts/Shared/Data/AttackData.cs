using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Universal data container for every attack in the game.
    /// Referenced by ComboNode, HitboxManager, EnemyAI, and the damage pipeline.
    /// Both Combat and World pillars read this — lives in Shared.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttack", menuName = "TomatoFighters/AttackData", order = 0)]
    public class AttackData : ScriptableObject
    {
        // ── Combo Identity ──────────────────────────────────────────────

        [Header("Combo Identity")]

        [Tooltip("Unique identifier (e.g. 'mystica_strike_1', 'brutor_finisher').")]
        public string attackId;

        [Tooltip("Display name for UI and debug ('Magic Burst 1', 'Overhead Slam').")]
        public string attackName;

        // ── Damage ──────────────────────────────────────────────────────

        [Header("Damage")]

        [Tooltip("Multiplied by character base ATK and all buff multipliers to get final damage.")]
        [Range(0.1f, 5.0f)]
        public float damageMultiplier = 1.0f;

        [Tooltip("Applied to target via Rigidbody2D on hit. X = horizontal push, Y = upward.")]
        public Vector2 knockbackForce;

        [Tooltip("Separate from knockback; used for launchers that send enemies airborne. Zero for non-launchers.")]
        public Vector2 launchForce;

        [Tooltip("If true and target hits a wall during knockback, triggers wall bounce.")]
        public bool causesWallBounce;

        [Tooltip("If true, target enters airborne state via juggle system.")]
        public bool causesLaunch;

        // ── Animation & Timing ──────────────────────────────────────────

        [Header("Animation & Timing")]

        [Tooltip("Animation clip this attack plays. Can be null for placeholder.")]
        public AnimationClip animationClip;

        [Tooltip("Frame number when the hitbox activates (0-indexed from animation start).")]
        [Range(0, 30)]
        public int hitboxStartFrame;

        [Tooltip("Number of frames the hitbox stays active.")]
        [Range(1, 30)]
        public int hitboxActiveFrames = 1;

        [Tooltip("Total animation length in frames (for combo window timing).")]
        [Range(1, 120)]
        public int totalFrames = 20;

        [Tooltip("Playback speed multiplier (default 1.0; Brutor slower, Slasher faster).")]
        [Range(0.1f, 3.0f)]
        public float animationSpeed = 1.0f;

        // ── Telegraph & State ───────────────────────────────────────────

        [Header("Telegraph & State")]

        [Tooltip("Normal attacks can be deflected/clashed. Unstoppable bypasses deflect — only dodge avoids.")]
        public TelegraphType telegraphType;

        [Tooltip("Can hit downed/grounded enemies (e.g. Brutor's overhead slam).")]
        public bool isOTGCapable;

        [Tooltip("Can be used while airborne.")]
        public bool isAirAttack;

        // ── Effects (Phase 1: nullable) ─────────────────────────────────

        [Header("Effects")]

        [Tooltip("Particle/VFX spawned on hit. Can be null in Phase 1.")]
        public GameObject hitEffectPrefab;

        [Tooltip("Sound on attack start. Can be null in Phase 1.")]
        public AudioClip swingSound;

        [Tooltip("Sound on hit confirm. Can be null in Phase 1.")]
        public AudioClip hitSound;
    }
}
