using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Spawns the selected character prefab from the <see cref="CharacterRegistry"/>.
    /// Place in any scene that needs a player character. Spawns on Awake.
    /// The <see cref="selectedCharacter"/> field can be set from a menu, save data,
    /// or the Inspector for quick testing.
    /// </summary>
    public class CharacterSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CharacterRegistry registry;
        [SerializeField] private CharacterType selectedCharacter = CharacterType.Slasher;

        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("When true, skip auto-spawn in Awake. Use with CharacterSelectUI.")]
        [SerializeField] private bool deferSpawn;

        [Header("Runtime")]
        [Tooltip("The currently spawned player instance. Read-only at runtime.")]
        [SerializeField] private GameObject currentPlayer;

        /// <summary>Currently active player GameObject.</summary>
        public GameObject CurrentPlayer => currentPlayer;

        /// <summary>The character type that is currently selected.</summary>
        public CharacterType SelectedCharacter => selectedCharacter;

        private void Awake()
        {
            if (registry == null)
                registry = Resources.Load<CharacterRegistry>("CharacterRegistry");

            if (!deferSpawn)
                SpawnSelected();
        }

        /// <summary>
        /// Spawns the currently selected character. Destroys any existing player first.
        /// </summary>
        public void SpawnSelected()
        {
            SpawnCharacter(selectedCharacter);
        }

        /// <summary>
        /// Switches to a different character. Destroys current player and spawns the new one.
        /// </summary>
        public void SwitchCharacter(CharacterType newType)
        {
            selectedCharacter = newType;
            SpawnCharacter(newType);
        }

        private void SpawnCharacter(CharacterType type)
        {
            if (registry == null)
            {
                Debug.LogError("[CharacterSpawner] No CharacterRegistry assigned.");
                return;
            }

            var entry = registry.GetEntry(type);
            if (entry == null || entry.prefab == null)
            {
                Debug.LogError($"[CharacterSpawner] No prefab registered for {type}.");
                return;
            }

            if (currentPlayer != null)
                Destroy(currentPlayer);

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            currentPlayer = Instantiate(entry.prefab, pos, Quaternion.identity);
            currentPlayer.name = $"Player_{type}";
            currentPlayer.tag = "Player";

            Debug.Log($"[CharacterSpawner] Spawned {type} at {pos}.");
        }
    }
}
