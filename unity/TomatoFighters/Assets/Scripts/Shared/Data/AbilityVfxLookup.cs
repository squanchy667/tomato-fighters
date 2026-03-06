using System;
using System.Collections.Generic;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Maps ability IDs to their VFX prefabs. Keeps VFX data separate from PathData
    /// so it remains a World-pillar concern without polluting combat/roguelite data.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityVfxLookup", menuName = "TomatoFighters/VFX/AbilityVfxLookup")]
    public class AbilityVfxLookup : ScriptableObject
    {
        [Serializable]
        public struct AbilityVfxEntry
        {
            public string abilityId;
            public GameObject vfxPrefab;
        }

        [SerializeField] private AbilityVfxEntry[] entries;

        private Dictionary<string, GameObject> _lookup;

        /// <summary>
        /// Returns the VFX prefab for the given ability ID, or null if not found.
        /// </summary>
        public GameObject GetVfxPrefab(string abilityId)
        {
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(abilityId, out var prefab);
            return prefab;
        }

        /// <summary>Sets the entries array (used by Creator Scripts).</summary>
        public void SetEntries(AbilityVfxEntry[] newEntries)
        {
            entries = newEntries;
            _lookup = null; // invalidate cache
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, GameObject>();
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.abilityId) && entry.vfxPrefab != null)
                    _lookup[entry.abilityId] = entry.vfxPrefab;
            }
        }

        private void OnEnable()
        {
            _lookup = null; // rebuild on load
        }
    }
}
