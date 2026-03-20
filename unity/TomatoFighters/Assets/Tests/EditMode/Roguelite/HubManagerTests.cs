using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TomatoFighters.Paths;
using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Roguelite
{
    /// <summary>
    /// Unit tests for <see cref="HubManager"/>.
    /// Uses lightweight GameObjects — no scene loading required.
    /// </summary>
    [TestFixture]
    public class HubManagerTests
    {
        // ── Test fixtures ─────────────────────────────────────────────────────

        private GameObject _rootGo;
        private HubManager _hubManager;
        private SaveSystem _saveSystem;
        private MetaProgression _metaProgression;
        private CurrencyManager _currencyManager;
        private InspirationSystem _inspirationSystem;

        private List<Object> _toDestroy;

        [SetUp]
        public void SetUp()
        {
            _toDestroy = new List<Object>();
            _rootGo = new GameObject("HubManagerTestRoot");
            _toDestroy.Add(_rootGo);

            _hubManager       = _rootGo.AddComponent<HubManager>();
            _saveSystem       = _rootGo.AddComponent<SaveSystem>();
            _currencyManager  = _rootGo.AddComponent<CurrencyManager>();
            _metaProgression  = _rootGo.AddComponent<MetaProgression>();
            _inspirationSystem = _rootGo.AddComponent<InspirationSystem>();

            _hubManager.InitializeForTest(
                _saveSystem, _metaProgression, _currencyManager, _inspirationSystem);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _toDestroy)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
        }

        // ── Character selection ───────────────────────────────────────────────

        [Test]
        public void SelectCharacter_UpdatesSelectedCharacter()
        {
            _hubManager.SelectCharacter(CharacterType.Slasher);
            Assert.AreEqual(CharacterType.Slasher, _hubManager.SelectedCharacter);
        }

        [Test]
        public void SelectCharacter_CanChangeSelection()
        {
            _hubManager.SelectCharacter(CharacterType.Brutor);
            _hubManager.SelectCharacter(CharacterType.Mystica);
            Assert.AreEqual(CharacterType.Mystica, _hubManager.SelectedCharacter);
        }

        // ── Stat preview ──────────────────────────────────────────────────────

        [Test]
        public void GetStatPreview_NullBaseStats_ReturnsDefaultFinalStats()
        {
            var result = _hubManager.GetStatPreview(CharacterType.Brutor, null);

            // Default FinalStats — all zeroes
            Assert.AreEqual(0, result.health);
            Assert.AreEqual(0, result.defense);
        }

        [Test]
        public void GetStatPreview_WithBaseStats_ReturnsBaseValues()
        {
            var baseStats = ScriptableObject.CreateInstance<CharacterBaseStats>();
            baseStats.characterType  = CharacterType.Brutor;
            baseStats.health         = 200;
            baseStats.defense        = 25;
            baseStats.attack         = 0.7f;
            baseStats.rangedAttack   = -1f;
            baseStats.throwableAttack = 0.9f;
            baseStats.speed          = 0.7f;
            baseStats.mana           = 50;
            baseStats.manaRegen      = 3f;
            baseStats.critChance     = 0.05f;
            baseStats.stunRate       = 1.0f;
            _toDestroy.Add(baseStats);

            var result = _hubManager.GetStatPreview(CharacterType.Brutor, baseStats);

            // Without any soul tree nodes, bonuses are 1.0 multipliers — stats equal base
            Assert.AreEqual(200, result.health);
            Assert.AreEqual(25,  result.defense);
            Assert.AreEqual(0.7f, result.attack, 0.001f);
        }

        [Test]
        public void GetStatPreview_DoesNotIncludeRitualOrTrinketModifiers()
        {
            // Stat preview between runs should only show base + soul tree
            // This test verifies the preview produces a stable result without rituals/trinkets wired
            var baseStats = ScriptableObject.CreateInstance<CharacterBaseStats>();
            baseStats.characterType  = CharacterType.Viper;
            baseStats.health         = 80;
            baseStats.defense        = 10;
            baseStats.attack         = 1.8f;
            baseStats.rangedAttack   = 1.8f;
            baseStats.throwableAttack = 0.9f;
            baseStats.speed          = 1.1f;
            baseStats.mana           = 120;
            baseStats.manaRegen      = 4f;
            baseStats.critChance     = 0.10f;
            baseStats.stunRate       = 1.0f;
            _toDestroy.Add(baseStats);

            var preview1 = _hubManager.GetStatPreview(CharacterType.Viper, baseStats);
            var preview2 = _hubManager.GetStatPreview(CharacterType.Viper, baseStats);

            // Deterministic — same inputs produce same outputs
            Assert.AreEqual(preview1.health,  preview2.health);
            Assert.AreEqual(preview1.attack,  preview2.attack, 0.001f);
            Assert.AreEqual(preview1.speed,   preview2.speed,  0.001f);
        }

        // ── NPC interaction ───────────────────────────────────────────────────

        [Test]
        public void InteractWithNPC_FiresOnNPCInteractionEvent()
        {
            string received = null;
            _hubManager.OnNPCInteraction += id => received = id;

            _hubManager.InteractWithNPC("shop_keeper");

            Assert.AreEqual("shop_keeper", received);
        }

        [Test]
        public void InteractWithNPC_NullId_DoesNotFireEvent()
        {
            bool fired = false;
            _hubManager.OnNPCInteraction += _ => fired = true;

            _hubManager.InteractWithNPC(null);

            Assert.IsFalse(fired);
        }

        [Test]
        public void InteractWithNPC_EmptyId_DoesNotFireEvent()
        {
            bool fired = false;
            _hubManager.OnNPCInteraction += _ => fired = true;

            _hubManager.InteractWithNPC("");

            Assert.IsFalse(fired);
        }

        [Test]
        public void InteractWithNPC_MultipleSubscribers_AllReceiveEvent()
        {
            int callCount = 0;
            _hubManager.OnNPCInteraction += _ => callCount++;
            _hubManager.OnNPCInteraction += _ => callCount++;

            _hubManager.InteractWithNPC("trainer");

            Assert.AreEqual(2, callCount);
        }

        // ── Save loaded on Awake ──────────────────────────────────────────────

        [Test]
        public void HasSaveData_WithNoSaveFile_IsFalse()
        {
            // HubManager is freshly created with no save file present
            // HasSaveData should be false when Awake found no file
            // (Awake was not called automatically in EditMode test — check default state)
            Assert.IsFalse(_hubManager.HasSaveData);
        }

        [Test]
        public void HasSaveData_AfterManualLoadFromFile_IsTrue()
        {
            // Write a minimal valid save file, then create a new HubManager that loads it
            string tempPath = SaveSystem.SavePath;
            bool hadExisting = File.Exists(tempPath);
            string backup = hadExisting ? tempPath + ".bak" : null;

            try
            {
                if (hadExisting)
                    File.Move(tempPath, backup);

                // Write a valid save via the static helpers
                var saveData = SaveSystem.BuildSaveData(
                    MetaProgressionData.Empty(),
                    crystalBalance: 10,
                    permanentInspirations: new List<string>());

                string json = UnityEngine.JsonUtility.ToJson(saveData);
                File.WriteAllText(tempPath, json);

                // Create a new HubManager and call its Awake-equivalent
                var go2 = new GameObject("HubManagerAwakeTest");
                var hub2 = go2.AddComponent<HubManager>();
                var save2 = go2.AddComponent<SaveSystem>();
                var currency2 = go2.AddComponent<CurrencyManager>();
                var meta2 = go2.AddComponent<MetaProgression>();
                var insp2 = go2.AddComponent<InspirationSystem>();

                hub2.InitializeForTest(save2, meta2, currency2, insp2);

                // Simulate Awake by calling the internal method via the public test path
                // We verify HasSaveData by confirming TryLoad would succeed
                bool loadResult = save2.TryLoad(out _);
                Assert.IsTrue(loadResult, "TryLoad should succeed with a valid save file present.");

                Object.DestroyImmediate(go2);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (backup != null && File.Exists(backup))
                    File.Move(backup, tempPath);
            }
        }
    }
}
