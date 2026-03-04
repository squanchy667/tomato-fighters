using NUnit.Framework;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Combat.Defense
{
    /// <summary>
    /// Validates the clash timing contract between attacker hitboxes and defender
    /// clash windows. Uses the spec from defense-timing-reference.md.
    ///
    /// Key contract: the defender's clash window must be OPEN when the attacker's
    /// hitbox collider fires OnTriggerEnter2D. There is no buffering or retroactive
    /// resolution — whatever DefenseState the defender is in at impact determines
    /// the outcome.
    /// </summary>
    [TestFixture]
    public class ClashTimingTests
    {
        private DefenseResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new DefenseResolver();
        }

        // ── Timing Math: Clash Window Before Hitbox ─────────────────────

        [Test]
        public void SlasherHeavyAttacks_ClashWindowStartBeforeHitbox()
        {
            // Critical design constraint from defense-timing-reference.md:
            // clashWindowStart < hitboxStartFrame / 60fps
            var attacks = new (string name, float clashStart, int hitboxFrame, float animSpeed)[]
            {
                ("SlasherHeavySlash",    0f, 3, 1f),
                ("SlasherLunge",         0f, 3, 1f),
                ("SlasherLungeFinisher", 0f, 4, 1f),
                ("SlasherQuickSlash",    0f, 2, 1f),
                ("SlasherSpinFinisher",  0f, 3, 1f),
            };

            foreach (var (name, clashStart, hitboxFrame, animSpeed) in attacks)
            {
                float hitboxStartTime = hitboxFrame / (60f * animSpeed);
                Assert.Less(clashStart, hitboxStartTime,
                    $"{name}: clashWindowStart ({clashStart:F3}s) must be < " +
                    $"hitboxStartTime ({hitboxStartTime:F3}s)");
            }
        }

        [Test]
        public void AllSlasherAttackDataSOs_PassClashConstraint()
        {
            // Loads real AttackData SOs and validates the constraint
            var guids = AssetDatabase.FindAssets("t:AttackData",
                new[] { "Assets/ScriptableObjects/Attacks/Slasher" });

            int checkedCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);
                if (attack == null || !attack.HasClashWindow) continue;

                float hitboxStartTime = attack.HitboxStartTime;

                Assert.Less(attack.clashWindowStart, hitboxStartTime,
                    $"{attack.attackName}: clash window start ({attack.clashWindowStart:F3}s) " +
                    $"must be before hitbox start ({hitboxStartTime:F3}s)");

                checkedCount++;
            }

            Assert.Greater(checkedCount, 0,
                "Should find at least one Slasher attack with a clash window");
        }

        // ── Resolver: Clash in HeavyStartup ─────────────────────────────

        [Test]
        public void Resolve_HeavyStartup_FacingAttacker_Clashed()
        {
            var result = _resolver.Resolve(
                DefenseState.HeavyStartup, Vector2.right, Vector2.right, false);

            Assert.AreEqual(DamageResponse.Clashed, result);
        }

        [Test]
        public void Resolve_HeavyStartup_FacingAway_Hit()
        {
            // Defender faces right, attacker is to the left — facing away
            var result = _resolver.Resolve(
                DefenseState.HeavyStartup, Vector2.right, Vector2.left, false);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        [Test]
        public void Resolve_HeavyStartup_Unstoppable_Hit()
        {
            var result = _resolver.Resolve(
                DefenseState.HeavyStartup, Vector2.right, Vector2.right,
                isAttackUnstoppable: true);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        [Test]
        public void Resolve_None_AlwaysHit()
        {
            // No defensive state — no clash possible
            var result = _resolver.Resolve(
                DefenseState.None, Vector2.right, Vector2.right, false);

            Assert.AreEqual(DamageResponse.Hit, result);
        }

        // ── DefenseSystem.OpenClashWindow Integration ───────────────────

        [Test]
        public void OpenClashWindow_PutsSystemInHeavyStartup()
        {
            var go = new GameObject("TestEntity");
            var ds = go.AddComponent<DefenseSystem>();

            var config = ScriptableObject.CreateInstance<DefenseConfig>();
            var so = new SerializedObject(ds);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();

            ds.OpenClashWindow(1.0f, Vector2.right);

            Assert.AreEqual(DefenseState.HeavyStartup, ds.CurrentState);
            Assert.IsTrue(ds.IsInClashWindow);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OpenClashWindow_Resolve_FacingAttacker_Clashed()
        {
            var go = new GameObject("TestEntity");
            go.transform.position = Vector3.zero;
            var ds = go.AddComponent<DefenseSystem>();

            var config = ScriptableObject.CreateInstance<DefenseConfig>();
            var so = new SerializedObject(ds);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();

            ds.OpenClashWindow(1.0f, Vector2.right);

            // Attacker is to the right
            Vector2 attackerPos = new Vector2(3f, 0f);
            var result = ds.Resolve(attackerPos, false);

            Assert.AreEqual(DamageResponse.Clashed, result);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OpenClashWindow_Resolve_AttackerBehind_Hit()
        {
            var go = new GameObject("TestEntity");
            go.transform.position = Vector3.zero;
            var ds = go.AddComponent<DefenseSystem>();

            var config = ScriptableObject.CreateInstance<DefenseConfig>();
            var so = new SerializedObject(ds);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Facing right
            ds.OpenClashWindow(1.0f, Vector2.right);

            // Attacker behind (to the left)
            Vector2 attackerPos = new Vector2(-3f, 0f);
            var result = ds.Resolve(attackerPos, false);

            Assert.AreEqual(DamageResponse.Hit, result,
                "Clash fails when attacker is behind the defender");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OpenClashWindow_Resolve_Unstoppable_Hit()
        {
            var go = new GameObject("TestEntity");
            go.transform.position = Vector3.zero;
            var ds = go.AddComponent<DefenseSystem>();

            var config = ScriptableObject.CreateInstance<DefenseConfig>();
            var so = new SerializedObject(ds);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();

            ds.OpenClashWindow(1.0f, Vector2.right);

            Vector2 attackerPos = new Vector2(3f, 0f);
            var result = ds.Resolve(attackerPos, isUnstoppable: true);

            Assert.AreEqual(DamageResponse.Hit, result,
                "Unstoppable attacks bypass clash");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(go);
        }

        // ── Timing Window Scenario: Slasher vs TestDummy ────────────────
        //
        // TestDummy: attackInterval=3s, telegraphDuration=1.0s, hitbox active=1.5s
        // Enemy opens clash window for 1.0s during telegraph
        //
        // Slasher heavy: clashWindowEnd=0.35s, hitbox fires at 0.35s (timer fallback)
        //
        // Two clash paths:
        //   Path 1 (EARLY): Player's hitbox hits enemy during enemy's 1.0s clash window
        //   Path 2 (LATE):  Enemy's hitbox hits player during player's 0.35s clash window
        //
        // Both paths set reciprocal immunity via ClashTracker → mutual cancel

        [Test]
        public void Scenario_EarlyClash_PlayerHitboxDuringEnemyTelegraph()
        {
            // Player presses heavy at 0.3s into the 1.0s telegraph
            // Player's hitbox fires at 0.3 + 0.35 = 0.65s → enemy still in clash window
            float telegraphDuration = 1.0f;
            float playerPressTime = 0.3f;
            float playerHitboxDelay = 0.35f; // SlasherHeavySlash clashWindowEnd
            float playerHitboxFires = playerPressTime + playerHitboxDelay;

            Assert.Less(playerHitboxFires, telegraphDuration,
                "Player's hitbox arrives while enemy is still in telegraph → Clashed");

            // At that moment, enemy is in HeavyStartup facing the player
            var enemyResolve = _resolver.Resolve(
                DefenseState.HeavyStartup, Vector2.left, Vector2.left, false);
            Assert.AreEqual(DamageResponse.Clashed, enemyResolve);
        }

        [Test]
        public void Scenario_LateClash_EnemyHitboxDuringPlayerClashWindow()
        {
            // Player presses heavy at 0.8s into the 1.0s telegraph
            // Enemy's hitbox fires at 1.0s → player's clash window: 0.8-1.15s → ACTIVE
            float playerPressTime = 0.8f;
            float playerClashDuration = 0.35f;
            float enemyHitboxFires = 1.0f;

            float playerClashStart = playerPressTime;
            float playerClashEnd = playerPressTime + playerClashDuration;

            Assert.GreaterOrEqual(enemyHitboxFires, playerClashStart);
            Assert.LessOrEqual(enemyHitboxFires, playerClashEnd,
                "Enemy's hitbox arrives during player's clash window → Clashed");

            // At that moment, player is in HeavyStartup facing the enemy
            var playerResolve = _resolver.Resolve(
                DefenseState.HeavyStartup, Vector2.right, Vector2.right, false);
            Assert.AreEqual(DamageResponse.Clashed, playerResolve);
        }

        [Test]
        public void Scenario_TooLate_MissedClashWindow()
        {
            // Player presses heavy at 1.2s (after telegraph ended at 1.0s)
            // Enemy's hitbox fired at 1.0s — player was in None state → Hit
            float playerPressTime = 1.2f;
            float enemyHitboxFires = 1.0f;

            Assert.Greater(playerPressTime, enemyHitboxFires,
                "Player presses after enemy hitbox already fired → took hit at None state");

            var result = _resolver.Resolve(
                DefenseState.None, Vector2.zero, Vector2.right, false);
            Assert.AreEqual(DamageResponse.Hit, result);
        }

        [Test]
        public void FullTimingWindow_EntireTelegraphIsValidClashWindow()
        {
            // The full 1.0s telegraph is the valid timing window.
            // Press at 0-0.65s → early clash (player hitbox during enemy window)
            // Press at 0.65-1.0s → late clash (enemy hitbox during player window)
            // Either path → mutual cancel via reciprocal immunity

            float telegraphDuration = 1.0f;
            float slasherClashDuration = 0.35f;
            float slasherHitboxDelay = 0.35f;

            float earlyClashEnd = telegraphDuration - slasherHitboxDelay; // 0.65s
            float lateClashStart = telegraphDuration - slasherClashDuration; // 0.65s

            // Early and late windows are contiguous (no gap)
            Assert.AreEqual(earlyClashEnd, lateClashStart, 0.001f,
                "Early clash and late clash windows meet at " +
                $"{earlyClashEnd:F2}s — no timing gap");

            // Total valid window = full telegraph
            Assert.AreEqual(telegraphDuration, 1.0f, 0.001f,
                "The full 1.0s telegraph is valid for clashing");
        }
    }
}
