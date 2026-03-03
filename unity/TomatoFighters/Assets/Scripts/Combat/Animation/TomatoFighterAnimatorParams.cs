namespace TomatoFighters.Combat
{
    /// <summary>
    /// Single source of truth for Animator parameter names and state names.
    /// Used by <see cref="CharacterAnimationBridge"/> (runtime) and
    /// <c>AnimationBuilder</c> (editor) to avoid string-mismatch bugs.
    ///
    /// <para><b>Animator parameters:</b></para>
    /// <list type="bullet">
    ///   <item><b>Speed</b> (Float) — 0 = idle, 0.5 = walk, 1 = run</item>
    ///   <item><b>IsGrounded</b> (Bool) — true when on the ground plane</item>
    ///   <item><b>AttackTrigger</b> (Trigger) — fired by ComboController per combo step</item>
    ///   <item><b>HurtTrigger</b> (Trigger) — reserved for hit-reaction (future)</item>
    ///   <item><b>DeathTrigger</b> (Trigger) — reserved for death animation (future)</item>
    /// </list>
    ///
    /// <para>Generated originally by Animation Forge, then moved into the
    /// <c>TomatoFighters.Combat</c> namespace for proper assembly referencing.</para>
    /// </summary>
    public static class TomatoFighterAnimatorParams
    {
        // Float: 0 = idle, 0.5 = walk, 1 = run (set by CharacterAnimationBridge)
        public const string SPEED = "Speed";

        // Bool: true when grounded (set by CharacterAnimationBridge)
        public const string ISGROUNDED = "IsGrounded";

        // Trigger: fired per combo step (set by ComboController.TriggerStepAnimation)
        public const string ATTACKTRIGGER = "AttackTrigger";

        // Trigger: reserved for hit-reaction system (T016+)
        public const string HURTTRIGGER = "HurtTrigger";

        // Trigger: reserved for death animation (T016+)
        public const string DEATHTRIGGER = "DeathTrigger";

        // Animator state names (must match states created by AnimationBuilder)
        public const string STATE_IDLE = "idle";
        public const string STATE_WALK = "walk";
        public const string STATE_RUN = "run";
    }
}
