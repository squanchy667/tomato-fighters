using System.Collections.Generic;
using NUnit.Framework;
using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Roguelite
{
    [TestFixture]
    public class InspirationSystemTests
    {
        private InspirationSystem _system;
        private GameObject _go;
        private MockPathProvider _pathProvider;
        private List<InspirationData> _allInspirations;
        private List<Object> _createdAssets;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("InspirationSystemTest");
            _system = _go.AddComponent<InspirationSystem>();
            _pathProvider = new MockPathProvider(CharacterType.Brutor);
            _allInspirations = new List<InspirationData>();
            _createdAssets = new List<Object>();

            CreateBrutorInspirations();
            CreateSlasherInspirations();

            _system.InitializeForTest(_pathProvider, null, _allInspirations);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (var asset in _createdAssets)
                Object.DestroyImmediate(asset);
        }

        // ── Collection ───────────────────────────────────────────────────────

        [Test]
        public void CollectInspiration_ValidInspiration_ReturnsTrue()
        {
            var insp = FindInspiration("brutor_warden_stat");
            Assert.IsTrue(_system.CollectInspiration(insp));
            Assert.AreEqual(1, _system.CollectedInspirations.Count);
        }

        [Test]
        public void CollectInspiration_Duplicate_ReturnsFalse()
        {
            var insp = FindInspiration("brutor_warden_stat");
            _system.CollectInspiration(insp);
            Assert.IsFalse(_system.CollectInspiration(insp));
            Assert.AreEqual(1, _system.CollectedInspirations.Count);
        }

        [Test]
        public void CollectInspiration_WrongCharacter_ReturnsFalse()
        {
            // Brutor system, Slasher inspiration
            var insp = FindInspiration("slasher_executioner_stat");
            Assert.IsFalse(_system.CollectInspiration(insp));
            Assert.AreEqual(0, _system.CollectedInspirations.Count);
        }

        [Test]
        public void CollectInspiration_FiresEvent()
        {
            InspirationData received = null;
            _system.OnInspirationCollected += d => received = d;

            var insp = FindInspiration("brutor_warden_stat");
            _system.CollectInspiration(insp);

            Assert.AreEqual(insp, received);
        }

        // ── Drop Candidates ──────────────────────────────────────────────────

        [Test]
        public void GetDropCandidates_FiltersByActivePaths()
        {
            _pathProvider.SetPaths(PathType.Warden, PathType.Bulwark);

            var candidates = _system.GetDropCandidates(10);

            // Should only include Warden + Bulwark inspirations for Brutor (4 total)
            Assert.AreEqual(4, candidates.Count);
            foreach (var c in candidates)
            {
                Assert.AreEqual(CharacterType.Brutor, c.character);
                Assert.IsTrue(c.path == PathType.Warden || c.path == PathType.Bulwark);
            }
        }

        [Test]
        public void GetDropCandidates_ExcludesCollected()
        {
            _pathProvider.SetPaths(PathType.Warden, PathType.Bulwark);

            _system.CollectInspiration(FindInspiration("brutor_warden_stat"));

            var candidates = _system.GetDropCandidates(10);
            Assert.AreEqual(3, candidates.Count);

            foreach (var c in candidates)
                Assert.AreNotEqual("brutor_warden_stat", c.inspirationId);
        }

        [Test]
        public void GetDropCandidates_RespectsCount()
        {
            _pathProvider.SetPaths(PathType.Warden, PathType.Bulwark);

            var candidates = _system.GetDropCandidates(2);
            Assert.LessOrEqual(candidates.Count, 2);
        }

        // ── Stat Calculation ─────────────────────────────────────────────────

        [Test]
        public void GetInspirationStatBonus_FlatBonusesAdditive()
        {
            // Create two flat HP inspirations
            var flat1 = CreateInspiration("flat1", CharacterType.Brutor, PathType.Warden,
                InspirationEffectType.StatModifier, StatType.Health, ModifierType.Flat, 10f);
            var flat2 = CreateInspiration("flat2", CharacterType.Brutor, PathType.Warden,
                InspirationEffectType.StatModifier, StatType.Health, ModifierType.Flat, 5f);

            _system.CollectInspiration(flat1);
            _system.CollectInspiration(flat2);

            Assert.AreEqual(15f, _system.GetInspirationStatBonus(StatType.Health), 0.001f);
        }

        [Test]
        public void GetInspirationStatMultiplier_PercentBonusesMultiplicative()
        {
            var pct1 = CreateInspiration("pct1", CharacterType.Brutor, PathType.Warden,
                InspirationEffectType.StatModifier, StatType.Health, ModifierType.Percent, 0.10f);
            var pct2 = CreateInspiration("pct2", CharacterType.Brutor, PathType.Warden,
                InspirationEffectType.StatModifier, StatType.Health, ModifierType.Percent, 0.05f);

            _system.CollectInspiration(pct1);
            _system.CollectInspiration(pct2);

            // (1 + 0.10) * (1 + 0.05) = 1.155
            Assert.AreEqual(1.155f, _system.GetInspirationStatMultiplier(StatType.Health), 0.001f);
        }

        [Test]
        public void GetInspirationStatBonus_NoCollected_ReturnsZero()
        {
            Assert.AreEqual(0f, _system.GetInspirationStatBonus(StatType.Health));
        }

        [Test]
        public void GetInspirationStatMultiplier_NoCollected_ReturnsOne()
        {
            Assert.AreEqual(1f, _system.GetInspirationStatMultiplier(StatType.Health));
        }

        // ── Ability Modifiers ────────────────────────────────────────────────

        [Test]
        public void HasAbilityModifier_WhenCollected_ReturnsTrue()
        {
            var insp = FindInspiration("brutor_warden_ability");
            _system.CollectInspiration(insp);

            Assert.IsTrue(_system.HasAbilityModifier("warden_taunt_extended"));
        }

        [Test]
        public void HasAbilityModifier_WhenNotCollected_ReturnsFalse()
        {
            Assert.IsFalse(_system.HasAbilityModifier("warden_taunt_extended"));
        }

        [Test]
        public void HasAbilityModifier_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(_system.HasAbilityModifier(null));
            Assert.IsFalse(_system.HasAbilityModifier(""));
        }

        // ── Permanent Unlock ─────────────────────────────────────────────────

        [Test]
        public void TryPermanentUnlock_WithCurrency_Succeeds()
        {
            var currGo = new GameObject("Currency");
            var currencyManager = currGo.AddComponent<CurrencyManager>();
            currencyManager.TryAdd(CurrencyType.PrimordialSeeds, 10);

            _system.InitializeForTest(_pathProvider, currencyManager, _allInspirations);

            Assert.IsTrue(_system.TryPermanentUnlock("brutor_warden_stat"));
            // Seeds spent (cost = 3)
            Assert.AreEqual(7, currencyManager.GetBalance(CurrencyType.PrimordialSeeds));

            Object.DestroyImmediate(currGo);
        }

        [Test]
        public void TryPermanentUnlock_InsufficientCurrency_Fails()
        {
            var currGo = new GameObject("Currency");
            var currencyManager = currGo.AddComponent<CurrencyManager>();
            // No seeds added — balance is 0

            _system.InitializeForTest(_pathProvider, currencyManager, _allInspirations);

            Assert.IsFalse(_system.TryPermanentUnlock("brutor_warden_stat"));

            Object.DestroyImmediate(currGo);
        }

        [Test]
        public void TryPermanentUnlock_AlreadyUnlocked_ReturnsFalse()
        {
            var currGo = new GameObject("Currency");
            var currencyManager = currGo.AddComponent<CurrencyManager>();
            currencyManager.TryAdd(CurrencyType.PrimordialSeeds, 20);

            _system.InitializeForTest(_pathProvider, currencyManager, _allInspirations);

            _system.TryPermanentUnlock("brutor_warden_stat");
            Assert.IsFalse(_system.TryPermanentUnlock("brutor_warden_stat"));

            Object.DestroyImmediate(currGo);
        }

        // ── Run Reset ────────────────────────────────────────────────────────

        [Test]
        public void ResetForNewRun_ClearsCollected()
        {
            _system.CollectInspiration(FindInspiration("brutor_warden_stat"));
            Assert.AreEqual(1, _system.CollectedInspirations.Count);

            _system.ResetForNewRun();
            Assert.AreEqual(0, _system.CollectedInspirations.Count);
        }

        [Test]
        public void ResetForNewRun_ReGrantsPermanentUnlocks()
        {
            var currGo = new GameObject("Currency");
            var currencyManager = currGo.AddComponent<CurrencyManager>();
            currencyManager.TryAdd(CurrencyType.PrimordialSeeds, 10);

            _system.InitializeForTest(_pathProvider, currencyManager, _allInspirations);

            _system.TryPermanentUnlock("brutor_warden_stat");
            _system.CollectInspiration(FindInspiration("brutor_bulwark_stat"));

            _system.ResetForNewRun();

            // Only the permanently unlocked one should remain
            Assert.AreEqual(1, _system.CollectedInspirations.Count);
            Assert.AreEqual("brutor_warden_stat", _system.CollectedInspirations[0].inspirationId);

            Object.DestroyImmediate(currGo);
        }

        // ── Save/Load ────────────────────────────────────────────────────────

        [Test]
        public void SaveLoad_RoundTripPermanentUnlockIds()
        {
            var currGo = new GameObject("Currency");
            var currencyManager = currGo.AddComponent<CurrencyManager>();
            currencyManager.TryAdd(CurrencyType.PrimordialSeeds, 20);

            _system.InitializeForTest(_pathProvider, currencyManager, _allInspirations);

            _system.TryPermanentUnlock("brutor_warden_stat");
            _system.TryPermanentUnlock("brutor_bulwark_stat");

            var saveData = _system.CreateSaveData();
            Assert.AreEqual(2, saveData.Count);

            // Create a fresh system and load
            var go2 = new GameObject("System2");
            var system2 = go2.AddComponent<InspirationSystem>();
            system2.InitializeForTest(_pathProvider, currencyManager, _allInspirations);

            system2.LoadSaveData(saveData);
            system2.ResetForNewRun();

            Assert.AreEqual(2, system2.CollectedInspirations.Count);

            Object.DestroyImmediate(currGo);
            Object.DestroyImmediate(go2);
        }

        // ── TriggerInspirationDrop ───────────────────────────────────────────

        [Test]
        public void TriggerInspirationDrop_FiresEventWithCandidates()
        {
            _pathProvider.SetPaths(PathType.Warden, PathType.Bulwark);

            InspirationDropEventData received = default;
            _system.OnInspirationDropReady += d => received = d;

            _system.TriggerInspirationDrop(3);

            Assert.IsNotNull(received.candidates);
            Assert.IsTrue(received.candidates.Count > 0);
            Assert.LessOrEqual(received.candidates.Count, 3);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private InspirationData FindInspiration(string id)
        {
            foreach (var insp in _allInspirations)
            {
                if (insp.inspirationId == id)
                    return insp;
            }
            return null;
        }

        private InspirationData CreateInspiration(
            string id, CharacterType character, PathType path,
            InspirationEffectType effectType,
            StatType statType = StatType.Health,
            ModifierType modType = ModifierType.Flat,
            float value = 0f,
            string abilityId = "")
        {
            var data = ScriptableObject.CreateInstance<InspirationData>();
            data.inspirationId = id;
            data.character = character;
            data.path = path;
            data.effectType = effectType;
            data.statType = statType;
            data.modifierType = modType;
            data.value = value;
            data.abilityModifierId = abilityId;
            data.permanentUnlockCost = 3;

            _allInspirations.Add(data);
            _createdAssets.Add(data);
            return data;
        }

        private void CreateBrutorInspirations()
        {
            CreateInspiration("brutor_warden_stat", CharacterType.Brutor, PathType.Warden,
                InspirationEffectType.StatModifier, StatType.Health, ModifierType.Percent, 0.10f);

            CreateInspiration("brutor_warden_ability", CharacterType.Brutor, PathType.Warden,
                InspirationEffectType.AbilityModifier, abilityId: "warden_taunt_extended");

            CreateInspiration("brutor_bulwark_stat", CharacterType.Brutor, PathType.Bulwark,
                InspirationEffectType.StatModifier, StatType.Defense, ModifierType.Percent, 0.15f);

            CreateInspiration("brutor_bulwark_ability", CharacterType.Brutor, PathType.Bulwark,
                InspirationEffectType.AbilityModifier, abilityId: "bulwark_iron_reflect");

            CreateInspiration("brutor_guardian_stat", CharacterType.Brutor, PathType.Guardian,
                InspirationEffectType.StatModifier, StatType.Defense, ModifierType.Percent, 0.08f);

            CreateInspiration("brutor_guardian_ability", CharacterType.Brutor, PathType.Guardian,
                InspirationEffectType.AbilityModifier, abilityId: "guardian_shield_pulse");
        }

        private void CreateSlasherInspirations()
        {
            CreateInspiration("slasher_executioner_stat", CharacterType.Slasher, PathType.Executioner,
                InspirationEffectType.StatModifier, StatType.Attack, ModifierType.Percent, 0.12f);

            CreateInspiration("slasher_executioner_ability", CharacterType.Slasher, PathType.Executioner,
                InspirationEffectType.AbilityModifier, abilityId: "executioner_mark_spread");
        }

        // ── Mock ─────────────────────────────────────────────────────────────

        private class MockPathProvider : IPathProvider
        {
            private readonly CharacterType _character;
            private readonly HashSet<PathType> _activePaths = new HashSet<PathType>();

            public MockPathProvider(CharacterType character)
            {
                _character = character;
            }

            public void SetPaths(PathType main, PathType secondary)
            {
                _activePaths.Clear();
                _activePaths.Add(main);
                _activePaths.Add(secondary);
            }

            public CharacterType Character => _character;
            public PathData MainPath => null;
            public PathData SecondaryPath => null;
            public int MainPathTier => 1;
            public int SecondaryPathTier => 1;
            public bool HasPath(PathType type) => _activePaths.Contains(type);
            public float GetPathStatBonus(StatType stat) => 0f;
            public bool IsPathAbilityUnlocked(string abilityId) => false;
        }
    }
}
