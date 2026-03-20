using System;
using TomatoFighters.Paths;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Orchestrates the between-run hub scene.
    ///
    /// <para><b>Responsibilities:</b>
    /// <list type="bullet">
    ///   <item>Load save data on scene enter via <see cref="Awake"/> and distribute it to systems.</item>
    ///   <item>Auto-save on application quit.</item>
    ///   <item>Character selection and stat preview (soul tree bonuses only — no ritual/trinket between runs).</item>
    ///   <item>NPC interaction event bus — subscribers handle UI and dialogue logic.</item>
    ///   <item>Soul Tree node unlock delegation to <see cref="MetaProgression"/>.</item>
    /// </list></para>
    ///
    /// <para><b>Injection:</b> not a singleton. All dependencies are wired via
    /// <c>[SerializeField]</c> in the Inspector or the HubSceneCreator Editor script.</para>
    /// </summary>
    public class HubManager : MonoBehaviour
    {
        // ── Dependencies (SerializeField) ─────────────────────────────────────

        [SerializeField] private SaveSystem _saveSystem;
        [SerializeField] private MetaProgression _metaProgression;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private InspirationSystem _inspirationSystem;
        [SerializeField] private CharacterBaseStats _defaultBaseStats;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the player interacts with an NPC.
        /// Subscribers (UI panels, dialogue managers) should handle presentation.
        /// Passes the NPC identifier string registered in <see cref="HubNPCInteraction.npcId"/>.
        /// </summary>
        public event Action<string> OnNPCInteraction;

        // ── Runtime state ─────────────────────────────────────────────────────

        private CharacterType _selectedCharacter;
        private bool _hasSaveData;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            LoadSaveIfExists();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        // ── Character selection ───────────────────────────────────────────────

        /// <summary>
        /// Sets the selected character for the next run.
        /// </summary>
        public void SelectCharacter(CharacterType character)
        {
            _selectedCharacter = character;
        }

        /// <summary>
        /// Returns a stat preview for the given character with only soul tree bonuses applied.
        /// Ritual and trinket modifiers are not included — the player is between runs.
        /// </summary>
        /// <param name="character">The character to preview.</param>
        /// <param name="baseStats">The character's base stat ScriptableObject.</param>
        /// <returns>
        /// <see cref="FinalStats"/> with only base + soul tree bonuses.
        /// Returns default <see cref="FinalStats"/> if <paramref name="baseStats"/> is null.
        /// </returns>
        public FinalStats GetStatPreview(CharacterType character, CharacterBaseStats baseStats)
        {
            if (baseStats == null)
            {
                Debug.LogWarning($"[HubManager] GetStatPreview: baseStats is null for {character}.");
                return default;
            }

            var input = StatModifierInput.Default(baseStats);

            // Apply soul tree bonuses only — rituals/trinkets are not active between runs
            if (_metaProgression != null)
            {
                int statCount = StatModifierInput.StatCount;
                var soulTreeBonuses = new float[statCount];
                for (int i = 0; i < statCount; i++)
                {
                    soulTreeBonuses[i] = _metaProgression.GetSoulTreeBonus((StatType)i);
                }
                input.soulTreeBonuses = soulTreeBonuses;
            }

            var calculator = new CharacterStatCalculator();
            return calculator.Calculate(input);
        }

        // ── NPC interaction ───────────────────────────────────────────────────

        /// <summary>
        /// Fires the <see cref="OnNPCInteraction"/> event for the given NPC identifier.
        /// Subscribers handle dialogue, shop UI, or any other NPC-specific logic.
        /// </summary>
        /// <param name="npcId">Identifier matching a <see cref="HubNPCInteraction.npcId"/>.</param>
        public void InteractWithNPC(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                Debug.LogWarning("[HubManager] InteractWithNPC: npcId is null or empty.");
                return;
            }

            OnNPCInteraction?.Invoke(npcId);
        }

        // ── Soul Tree ─────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to unlock a Soul Tree node, delegating currency checks to <see cref="MetaProgression"/>.
        /// </summary>
        /// <param name="nodeId">ID of the node to unlock.</param>
        /// <returns><c>true</c> if the node was successfully purchased and unlocked.</returns>
        public bool TryUnlockNode(string nodeId)
        {
            if (_metaProgression == null)
            {
                Debug.LogError("[HubManager] TryUnlockNode: MetaProgression is null.");
                return false;
            }

            return _metaProgression.TryPurchaseNode(nodeId);
        }

        // ── Save/Load ─────────────────────────────────────────────────────────

        /// <summary>
        /// Explicitly saves current state. Also called automatically on quit.
        /// </summary>
        public void Save()
        {
            if (_saveSystem == null || _metaProgression == null ||
                _currencyManager == null || _inspirationSystem == null)
            {
                Debug.LogWarning("[HubManager] Save skipped: one or more dependencies are null.");
                return;
            }

            _saveSystem.Save(_metaProgression, _currencyManager, _inspirationSystem);
        }

        // ── Public state ──────────────────────────────────────────────────────

        /// <summary>The character currently selected for the next run.</summary>
        public CharacterType SelectedCharacter => _selectedCharacter;

        /// <summary>Returns <c>true</c> if a save file was found and loaded on Awake.</summary>
        public bool HasSaveData => _hasSaveData;

        // ── Test support ──────────────────────────────────────────────────────

        /// <summary>
        /// Injects all dependencies for unit testing without a MonoBehaviour scene setup.
        /// </summary>
        internal void InitializeForTest(
            SaveSystem saveSystem,
            MetaProgression metaProgression,
            CurrencyManager currencyManager,
            InspirationSystem inspirationSystem)
        {
            _saveSystem        = saveSystem;
            _metaProgression   = metaProgression;
            _currencyManager   = currencyManager;
            _inspirationSystem = inspirationSystem;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void LoadSaveIfExists()
        {
            if (_saveSystem == null)
                return;

            if (_saveSystem.TryLoad(out var saveData))
            {
                _saveSystem.ApplyLoad(saveData, _metaProgression, _currencyManager, _inspirationSystem);
                _hasSaveData = true;
            }
        }
    }
}
