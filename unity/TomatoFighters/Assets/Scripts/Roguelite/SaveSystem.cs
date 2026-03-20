using System;
using System.Collections.Generic;
using System.IO;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// JSON persistence wrapper for meta-progression data.
    /// Serialises Crystal balance, Soul Tree node IDs, and permanent inspiration IDs
    /// using only <see cref="JsonUtility"/> — no external dependencies.
    ///
    /// <para><b>Schema versioning:</b> <see cref="SaveData.schemaVersion"/> is stored on
    /// every write. A version mismatch on load produces a warning but never crashes;
    /// the data is still applied to allow forward-compatible saves.</para>
    ///
    /// <para><b>Usage:</b> Inject via <c>[SerializeField]</c> — not a singleton.</para>
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────

        private const int CURRENT_SCHEMA_VERSION = 1;

        // ── Serialisable save envelope ────────────────────────────────────────

        /// <summary>
        /// Flat serialisable snapshot of all persistent game state.
        /// Exactly one instance is written to disk per save.
        /// </summary>
        [Serializable]
        public struct SaveData
        {
            /// <summary>Bumped when the save format changes in a breaking way.</summary>
            public int schemaVersion;

            /// <summary>Soul Tree and permanent inspiration state.</summary>
            public MetaProgressionData metaProgression;

            /// <summary>Persisted Crystal balance (Crystals are the only cross-run currency).</summary>
            public int crystalBalance;

            /// <summary>IDs of permanently unlocked inspirations (via Primordial Seeds).</summary>
            public List<string> permanentInspirations;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Absolute path to the save file on this device.</summary>
        public static string SavePath =>
            Path.Combine(Application.persistentDataPath, "tomatofighters_save.json");

        /// <summary>Returns <c>true</c> if a save file exists on disk.</summary>
        public bool SaveExists => File.Exists(SavePath);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises current game state to JSON and writes it to <see cref="SavePath"/>.
        /// </summary>
        /// <param name="metaProgression">Source of Soul Tree state.</param>
        /// <param name="currencyManager">Source of Crystal balance.</param>
        /// <param name="inspirationSystem">Source of permanent inspiration IDs.</param>
        public void Save(
            MetaProgression metaProgression,
            CurrencyManager currencyManager,
            InspirationSystem inspirationSystem)
        {
            if (metaProgression == null)
            {
                Debug.LogError("[SaveSystem] Save failed: MetaProgression is null.");
                return;
            }
            if (currencyManager == null)
            {
                Debug.LogError("[SaveSystem] Save failed: CurrencyManager is null.");
                return;
            }
            if (inspirationSystem == null)
            {
                Debug.LogError("[SaveSystem] Save failed: InspirationSystem is null.");
                return;
            }

            var metaData = metaProgression.CreateSaveData();

            var data = new SaveData
            {
                schemaVersion        = CURRENT_SCHEMA_VERSION,
                metaProgression      = metaData,
                crystalBalance       = currencyManager.GetBalance(CurrencyType.Crystals),
                permanentInspirations = inspirationSystem.CreateSaveData()
            };

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);

            Debug.Log($"[SaveSystem] Game saved to: {SavePath}");
        }

        /// <summary>
        /// Attempts to read and deserialise the save file.
        /// </summary>
        /// <param name="data">The deserialised save data on success.</param>
        /// <returns><c>true</c> if the file exists and parsed successfully; <c>false</c> otherwise.</returns>
        public bool TryLoad(out SaveData data)
        {
            data = default;

            if (!File.Exists(SavePath))
                return false;

            try
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SaveData>(json);

                if (data.schemaVersion != CURRENT_SCHEMA_VERSION)
                {
                    Debug.LogWarning(
                        $"[SaveSystem] Schema version mismatch: expected {CURRENT_SCHEMA_VERSION}, " +
                        $"got {data.schemaVersion}. Applying data anyway.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load save file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies loaded save data to all runtime systems.
        /// Call after a successful <see cref="TryLoad"/>.
        /// </summary>
        /// <param name="data">Previously loaded save data.</param>
        /// <param name="metaProgression">Receives Soul Tree state.</param>
        /// <param name="currencyManager">Receives Crystal balance.</param>
        /// <param name="inspirationSystem">Receives permanent inspiration IDs.</param>
        public void ApplyLoad(
            SaveData data,
            MetaProgression metaProgression,
            CurrencyManager currencyManager,
            InspirationSystem inspirationSystem)
        {
            if (metaProgression == null || currencyManager == null || inspirationSystem == null)
            {
                Debug.LogError("[SaveSystem] ApplyLoad failed: one or more systems are null.");
                return;
            }

            metaProgression.LoadSaveData(data.metaProgression);
            currencyManager.SetBalance(CurrencyType.Crystals, data.crystalBalance);
            inspirationSystem.LoadSaveData(data.permanentInspirations);

            Debug.Log("[SaveSystem] Save data applied to all systems.");
        }

        /// <summary>Deletes the save file if it exists.</summary>
        public void DeleteSave()
        {
            if (!File.Exists(SavePath))
                return;

            File.Delete(SavePath);
            Debug.Log("[SaveSystem] Save file deleted.");
        }

        // ── Test support ──────────────────────────────────────────────────────

        /// <summary>
        /// Builds a <see cref="SaveData"/> struct from provided values without file I/O.
        /// Used by unit tests to verify round-trip serialisation without disk access.
        /// </summary>
        internal static SaveData BuildSaveData(
            MetaProgressionData metaProgression,
            int crystalBalance,
            List<string> permanentInspirations)
        {
            return new SaveData
            {
                schemaVersion         = CURRENT_SCHEMA_VERSION,
                metaProgression       = metaProgression,
                crystalBalance        = crystalBalance,
                permanentInspirations = permanentInspirations
            };
        }

        /// <summary>
        /// Serialises then deserialises a <see cref="SaveData"/> in-memory.
        /// Returns the round-tripped struct for test assertion.
        /// </summary>
        internal static SaveData RoundTrip(SaveData input)
        {
            string json = JsonUtility.ToJson(input);
            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}
