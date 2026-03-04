using System;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Registry of all playable characters. Holds prefab + stats references per character.
    /// Used by <see cref="CharacterSpawner"/> to instantiate the selected character.
    /// Single SO asset — wire all 4 entries in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Data/Character Registry",
                     fileName = "CharacterRegistry")]
    public class CharacterRegistry : ScriptableObject
    {
        [Tooltip("One entry per playable character.")]
        public CharacterEntry[] characters;

        /// <summary>
        /// Finds the entry matching the given character type. Returns null if not registered.
        /// </summary>
        public CharacterEntry GetEntry(CharacterType type)
        {
            if (characters == null) return null;

            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i].characterType == type)
                    return characters[i];
            }

            return null;
        }
    }

    [Serializable]
    public class CharacterEntry
    {
        public CharacterType characterType;

        [Tooltip("The prefab to instantiate for this character.")]
        public GameObject prefab;

        [Tooltip("Base stats SO for this character.")]
        public CharacterBaseStats baseStats;
    }
}
