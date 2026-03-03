using System.Reflection;
using NUnit.Framework;
using TomatoFighters.Paths;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Paths
{
    /// <summary>
    /// Edit-mode unit tests for PathSystem.
    /// Covers selection rules, tier progression, run lifecycle, IPathProvider,
    /// and event firing.
    ///
    /// MonoBehaviour is instantiated via AddComponent on a test GameObject.
    /// The private SerializeField 'character' is set via reflection.
    /// </summary>
    [TestFixture]
    public class PathSystemTests
    {
        private GameObject _go;
        private PathSystem _ps;

        private PathData _warden;      // Brutor path 1
        private PathData _bulwark;     // Brutor path 2
        private PathData _guardian;    // Brutor path 3
        private PathData _executioner; // Slasher — wrong character for Brutor tests

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PathSystemTest");
            _ps = _go.AddComponent<PathSystem>();

            // Awake fires during AddComponent — _runProgressionSource is null → null-safe no-op
            SetCharacter(CharacterType.Brutor);

            _warden = MakePath(CharacterType.Brutor, PathType.Warden,
                tier1: "Warden_Provoke",
                tier2: "Warden_AggroAura",
                tier3: "Warden_WrathOfTheWarden");

            _bulwark = MakePath(CharacterType.Brutor, PathType.Bulwark,
                tier1: "Bulwark_IronGuard",
                tier2: "Bulwark_Retaliation",
                tier3: "Bulwark_Fortress");

            _guardian = MakePath(CharacterType.Brutor, PathType.Guardian,
                tier1: "Guardian_ShieldLink",
                tier2: "Guardian_RallyingPresence",
                tier3: "Guardian_AegisDome");

            _executioner = MakePath(CharacterType.Slasher, PathType.Executioner,
                tier1: "Executioner_MarkForDeath",
                tier2: "Executioner_ExecutionThreshold",
                tier3: "Executioner_Deathblow");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_warden);
            Object.DestroyImmediate(_bulwark);
            Object.DestroyImmediate(_guardian);
            Object.DestroyImmediate(_executioner);
        }

        // ── SelectMainPath ─────────────────────────────────────────────────────

        [Test]
        public void SelectMainPath_ValidData_ReturnsTrueAndSetsTier1()
        {
            bool result = _ps.SelectMainPath(_warden);

            Assert.IsTrue(result);
            Assert.AreEqual(_warden, _ps.MainPath);
            Assert.AreEqual(1, _ps.MainPathTier);
        }

        [Test]
        public void SelectMainPath_NullData_ReturnsFalse()
        {
            bool result = _ps.SelectMainPath(null);

            Assert.IsFalse(result);
            Assert.IsNull(_ps.MainPath);
        }

        [Test]
        public void SelectMainPath_WrongCharacter_ReturnsFalse()
        {
            bool result = _ps.SelectMainPath(_executioner);  // Slasher path for Brutor system

            Assert.IsFalse(result);
            Assert.IsNull(_ps.MainPath);
        }

        [Test]
        public void SelectMainPath_AlreadySelected_ReturnsFalseAndKeepsOriginal()
        {
            _ps.SelectMainPath(_warden);
            bool result = _ps.SelectMainPath(_bulwark);

            Assert.IsFalse(result);
            Assert.AreEqual(_warden, _ps.MainPath);  // original unchanged
        }

        // ── SelectSecondaryPath ────────────────────────────────────────────────

        [Test]
        public void SelectSecondaryPath_WithoutMainSelected_ReturnsFalse()
        {
            bool result = _ps.SelectSecondaryPath(_bulwark);

            Assert.IsFalse(result);
            Assert.IsNull(_ps.SecondaryPath);
        }

        [Test]
        public void SelectSecondaryPath_SamePathAsMain_ReturnsFalse()
        {
            _ps.SelectMainPath(_warden);
            bool result = _ps.SelectSecondaryPath(_warden);  // same reference

            Assert.IsFalse(result);
            Assert.IsNull(_ps.SecondaryPath);
        }

        [Test]
        public void SelectSecondaryPath_WrongCharacter_ReturnsFalse()
        {
            _ps.SelectMainPath(_warden);
            bool result = _ps.SelectSecondaryPath(_executioner);

            Assert.IsFalse(result);
            Assert.IsNull(_ps.SecondaryPath);
        }

        [Test]
        public void SelectSecondaryPath_NullData_ReturnsFalse()
        {
            _ps.SelectMainPath(_warden);
            bool result = _ps.SelectSecondaryPath(null);

            Assert.IsFalse(result);
            Assert.IsNull(_ps.SecondaryPath);
        }

        [Test]
        public void SelectSecondaryPath_ValidData_ReturnsTrueAndSetsTier1()
        {
            _ps.SelectMainPath(_warden);
            bool result = _ps.SelectSecondaryPath(_bulwark);

            Assert.IsTrue(result);
            Assert.AreEqual(_bulwark, _ps.SecondaryPath);
            Assert.AreEqual(1, _ps.SecondaryPathTier);
        }

        [Test]
        public void SelectSecondaryPath_AlreadySelected_ReturnsFalseAndKeepsOriginal()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);

            bool result = _ps.SelectSecondaryPath(_guardian);  // third path attempt

            Assert.IsFalse(result);
            Assert.AreEqual(_bulwark, _ps.SecondaryPath);  // original unchanged
        }

        // ── Tier progression ───────────────────────────────────────────────────

        [Test]
        public void HandleBossDefeated_AdvancesBothActivePathsFromT1ToT2()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);

            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));

            Assert.AreEqual(2, _ps.MainPathTier);
            Assert.AreEqual(2, _ps.SecondaryPathTier);
        }

        [Test]
        public void HandleBossDefeated_IsIdempotent_DoesNotAdvancePastT2()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));

            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));  // second call

            Assert.AreEqual(2, _ps.MainPathTier);
            Assert.AreEqual(2, _ps.SecondaryPathTier);
        }

        [Test]
        public void HandleIslandCompleted_AdvancesMainFromT2ToT3()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));   // → T2

            _ps.HandleIslandCompleted(new IslandCompletedData(0));

            Assert.AreEqual(3, _ps.MainPathTier);
        }

        [Test]
        public void HandleIslandCompleted_SecondaryPathUnchanged()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));  // both → T2

            _ps.HandleIslandCompleted(new IslandCompletedData(0));

            Assert.AreEqual(2, _ps.SecondaryPathTier);  // secondary stays at T2
        }

        [Test]
        public void HandleIslandCompleted_CannotSkipFromT1ToT3()
        {
            _ps.SelectMainPath(_warden);
            // Main is at T1 — island completed should NOT grant T3

            _ps.HandleIslandCompleted(new IslandCompletedData(0));

            Assert.AreEqual(1, _ps.MainPathTier);
        }

        [Test]
        public void HandleBossDefeated_WithNoPathsSelected_IsNoOp()
        {
            // Should not throw or do anything unexpected
            Assert.DoesNotThrow(() =>
                _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0)));

            Assert.AreEqual(0, _ps.MainPathTier);
            Assert.AreEqual(0, _ps.SecondaryPathTier);
        }

        // ── Run lifecycle ──────────────────────────────────────────────────────

        [Test]
        public void ResetForNewRun_ClearsAllPathStateAndTiers()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));

            _ps.ResetForNewRun();

            Assert.IsNull(_ps.MainPath);
            Assert.IsNull(_ps.SecondaryPath);
            Assert.AreEqual(0, _ps.MainPathTier);
            Assert.AreEqual(0, _ps.SecondaryPathTier);
        }

        [Test]
        public void ResetForNewRun_AllowsRe_SelectionAfterReset()
        {
            _ps.SelectMainPath(_warden);
            _ps.ResetForNewRun();

            bool result = _ps.SelectMainPath(_bulwark);

            Assert.IsTrue(result);
            Assert.AreEqual(_bulwark, _ps.MainPath);
        }

        // ── IPathProvider ─────────────────────────────────────────────────────

        [Test]
        public void HasPath_ReturnsTrueForActiveMainPath()
        {
            _ps.SelectMainPath(_warden);

            Assert.IsTrue(_ps.HasPath(PathType.Warden));
        }

        [Test]
        public void HasPath_ReturnsTrueForActiveSecondaryPath()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);

            Assert.IsTrue(_ps.HasPath(PathType.Bulwark));
        }

        [Test]
        public void HasPath_ReturnsFalseForUnselectedPath()
        {
            _ps.SelectMainPath(_warden);

            Assert.IsFalse(_ps.HasPath(PathType.Guardian));
        }

        [Test]
        public void IsPathAbilityUnlocked_ReturnsTrueForT1AbilityAfterSelection()
        {
            _ps.SelectMainPath(_warden);

            Assert.IsTrue(_ps.IsPathAbilityUnlocked("Warden_Provoke"));
        }

        [Test]
        public void IsPathAbilityUnlocked_ReturnsFalseForT2AbilityWhileAtT1()
        {
            _ps.SelectMainPath(_warden);

            Assert.IsFalse(_ps.IsPathAbilityUnlocked("Warden_AggroAura"));
        }

        [Test]
        public void IsPathAbilityUnlocked_ReturnsTrueForT2AbilityAfterBossDefeated()
        {
            _ps.SelectMainPath(_warden);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));

            Assert.IsTrue(_ps.IsPathAbilityUnlocked("Warden_AggroAura"));
        }

        [Test]
        public void IsPathAbilityUnlocked_ReturnsFalseForT3AbilityWhileAtT2()
        {
            _ps.SelectMainPath(_warden);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));  // → T2

            Assert.IsFalse(_ps.IsPathAbilityUnlocked("Warden_WrathOfTheWarden"));
        }

        [Test]
        public void IsPathAbilityUnlocked_ReturnsTrueForSecondaryAbility()
        {
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);

            Assert.IsTrue(_ps.IsPathAbilityUnlocked("Bulwark_IronGuard"));
        }

        // ── Events ─────────────────────────────────────────────────────────────

        [Test]
        public void SelectMainPath_FiresOnMainPathSelectedEvent()
        {
            bool fired = false;
            _ps.OnMainPathSelected += _ => fired = true;

            _ps.SelectMainPath(_warden);

            Assert.IsTrue(fired);
        }

        [Test]
        public void SelectMainPath_DoesNotFireEvent_WhenRejected()
        {
            _ps.SelectMainPath(_warden);  // lock main path first
            bool fired = false;
            _ps.OnMainPathSelected += _ => fired = true;

            _ps.SelectMainPath(_bulwark);  // should be rejected

            Assert.IsFalse(fired);
        }

        [Test]
        public void SelectSecondaryPath_FiresOnSecondaryPathSelectedEvent()
        {
            bool fired = false;
            _ps.OnSecondaryPathSelected += _ => fired = true;
            _ps.SelectMainPath(_warden);

            _ps.SelectSecondaryPath(_bulwark);

            Assert.IsTrue(fired);
        }

        [Test]
        public void HandleBossDefeated_FiresTierUpEventForEachAdvancedPath()
        {
            int fireCount = 0;
            _ps.OnPathTierUp += _ => fireCount++;
            _ps.SelectMainPath(_warden);
            _ps.SelectSecondaryPath(_bulwark);

            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));

            Assert.AreEqual(2, fireCount);  // one for main, one for secondary
        }

        [Test]
        public void HandleBossDefeated_DoesNotFireEvent_WhenAlreadyAtT2()
        {
            _ps.SelectMainPath(_warden);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));  // advance to T2

            int fireCount = 0;
            _ps.OnPathTierUp += _ => fireCount++;

            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));  // already at T2

            Assert.AreEqual(0, fireCount);
        }

        [Test]
        public void HandleIslandCompleted_FiresTierUpEvent_ForMainPath()
        {
            _ps.SelectMainPath(_warden);
            _ps.HandleBossDefeated(new BossDefeatedData("TestBoss", 0));  // → T2

            int fireCount = 0;
            _ps.OnPathTierUp += _ => fireCount++;

            _ps.HandleIslandCompleted(new IslandCompletedData(0));

            Assert.AreEqual(1, fireCount);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SetCharacter(CharacterType type)
        {
            var field = typeof(PathSystem).GetField("character",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Field 'character' not found on PathSystem — check field name.");
            field.SetValue(_ps, type);
        }

        private static PathData MakePath(CharacterType character, PathType type,
            string tier1, string tier2, string tier3)
        {
            var data = ScriptableObject.CreateInstance<PathData>();
            data.character      = character;
            data.pathType       = type;
            data.tier1AbilityId = tier1;
            data.tier2AbilityId = tier2;
            data.tier3AbilityId = tier3;
            return data;
        }
    }
}
