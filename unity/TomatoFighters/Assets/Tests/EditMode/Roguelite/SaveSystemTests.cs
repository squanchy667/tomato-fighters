using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Tests.EditMode.Roguelite
{
    /// <summary>
    /// Unit tests for <see cref="SaveSystem"/>.
    /// Uses in-memory round-trip helpers so no disk I/O occurs during the test run.
    /// </summary>
    [TestFixture]
    public class SaveSystemTests
    {
        // ── Schema version ────────────────────────────────────────────────────

        [Test]
        public void BuildSaveData_SetsSchemaVersion1()
        {
            var data = SaveSystem.BuildSaveData(
                MetaProgressionData.Empty(),
                crystalBalance: 0,
                permanentInspirations: new List<string>());

            Assert.AreEqual(1, data.schemaVersion);
        }

        // ── MetaProgressionData round-trip ────────────────────────────────────

        [Test]
        public void RoundTrip_MetaProgressionData_UnlockedNodeIds_Preserved()
        {
            var meta = new MetaProgressionData
            {
                unlockedNodeIds       = new List<string> { "node_atk_1", "node_hp_2" },
                permanentInspirationIds = new List<string>()
            };

            var input = SaveSystem.BuildSaveData(meta, crystalBalance: 0, permanentInspirations: new List<string>());
            var output = SaveSystem.RoundTrip(input);

            Assert.AreEqual(2, output.metaProgression.unlockedNodeIds.Count);
            Assert.AreEqual("node_atk_1", output.metaProgression.unlockedNodeIds[0]);
            Assert.AreEqual("node_hp_2",  output.metaProgression.unlockedNodeIds[1]);
        }

        [Test]
        public void RoundTrip_MetaProgressionData_EmptyNodeIds_Preserved()
        {
            var meta = MetaProgressionData.Empty();

            var input  = SaveSystem.BuildSaveData(meta, 0, new List<string>());
            var output = SaveSystem.RoundTrip(input);

            Assert.IsNotNull(output.metaProgression.unlockedNodeIds);
            Assert.AreEqual(0, output.metaProgression.unlockedNodeIds.Count);
        }

        // ── Crystal balance ───────────────────────────────────────────────────

        [Test]
        public void RoundTrip_CrystalBalance_Preserved()
        {
            var input  = SaveSystem.BuildSaveData(MetaProgressionData.Empty(), 250, new List<string>());
            var output = SaveSystem.RoundTrip(input);

            Assert.AreEqual(250, output.crystalBalance);
        }

        [Test]
        public void RoundTrip_ZeroCrystalBalance_Preserved()
        {
            var input  = SaveSystem.BuildSaveData(MetaProgressionData.Empty(), 0, new List<string>());
            var output = SaveSystem.RoundTrip(input);

            Assert.AreEqual(0, output.crystalBalance);
        }

        // ── Permanent inspiration IDs ─────────────────────────────────────────

        [Test]
        public void RoundTrip_PermanentInspirationIds_Preserved()
        {
            var ids = new List<string> { "brutor_warden_stat", "slasher_shadow_ability" };

            var input  = SaveSystem.BuildSaveData(MetaProgressionData.Empty(), 0, ids);
            var output = SaveSystem.RoundTrip(input);

            Assert.AreEqual(2, output.permanentInspirations.Count);
            Assert.AreEqual("brutor_warden_stat",      output.permanentInspirations[0]);
            Assert.AreEqual("slasher_shadow_ability",  output.permanentInspirations[1]);
        }

        [Test]
        public void RoundTrip_EmptyPermanentInspirations_Preserved()
        {
            var input  = SaveSystem.BuildSaveData(MetaProgressionData.Empty(), 0, new List<string>());
            var output = SaveSystem.RoundTrip(input);

            Assert.IsNotNull(output.permanentInspirations);
            Assert.AreEqual(0, output.permanentInspirations.Count);
        }

        // ── TryLoad with missing file ─────────────────────────────────────────

        [Test]
        public void TryLoad_MissingFile_ReturnsFalse()
        {
            var go = new GameObject("SaveSystemTest");
            var saveSystem = go.AddComponent<SaveSystem>();

            // Use a path that should not exist on any test machine
            string nonExistentPath = Path.Combine(
                Application.persistentDataPath, "nonexistent_test_save_xyz.json");

            // Verify the file truly doesn't exist before the test
            if (File.Exists(nonExistentPath))
                File.Delete(nonExistentPath);

            // TryLoad on the default path — should return false when no save file exists
            // (We cannot override SavePath, but we can delete any existing save first)
            string actualPath = SaveSystem.SavePath;
            bool hadExistingFile = File.Exists(actualPath);
            string backup = null;

            try
            {
                if (hadExistingFile)
                {
                    backup = actualPath + ".bak";
                    File.Move(actualPath, backup);
                }

                bool result = saveSystem.TryLoad(out _);
                Assert.IsFalse(result);
            }
            finally
            {
                if (backup != null && File.Exists(backup))
                    File.Move(backup, actualPath);
                Object.DestroyImmediate(go);
            }
        }

        // ── Full round-trip combining all fields ──────────────────────────────

        [Test]
        public void RoundTrip_AllFields_Preserved()
        {
            var meta = new MetaProgressionData
            {
                unlockedNodeIds         = new List<string> { "soul_atk_1" },
                permanentInspirationIds = new List<string> { "brutor_warden_stat" }
            };

            var inspirations = new List<string> { "brutor_warden_stat" };

            var input = SaveSystem.BuildSaveData(meta, crystalBalance: 100, permanentInspirations: inspirations);
            var output = SaveSystem.RoundTrip(input);

            Assert.AreEqual(1, output.schemaVersion);
            Assert.AreEqual(100, output.crystalBalance);
            Assert.AreEqual(1, output.metaProgression.unlockedNodeIds.Count);
            Assert.AreEqual(1, output.permanentInspirations.Count);
        }
    }
}
