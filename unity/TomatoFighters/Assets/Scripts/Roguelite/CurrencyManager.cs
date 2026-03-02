using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Central authority for all currency operations in Tomato Fighters.
    /// No other system should directly modify currency values — all changes go through here.
    ///
    /// <para><b>3 currency types:</b>
    /// <list type="bullet">
    ///   <item><description>Crystals — persist between runs, spent on Soul Tree and hub shop.</description></item>
    ///   <item><description>ImbuedFruits — per-run only, reset at run start.</description></item>
    ///   <item><description>PrimordialSeeds — per-run only, reset at run start. Used for permanent unlocks.</description></item>
    /// </list></para>
    ///
    /// <para><b>Injection:</b> not a singleton. Other systems receive this via <c>[SerializeField]</c>.</para>
    ///
    /// <para><b>Thread safety:</b> all balance modifications are protected by a lock, safe for
    /// async save/load operations (T039).</para>
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired after every successful currency modification (Add, Remove, Set, Reset).
        /// The event carries the previous amount, new amount, and signed delta.
        /// </summary>
        public event Action<CurrencyChangeEventData> OnCurrencyChanged;

        // ── Persistence metadata ──────────────────────────────────────────────

        // Crystals persist; Imbued Fruits and Primordial Seeds reset each run.
        // This flag is read by SaveSystem (T039) to decide what to serialize.
        private static readonly Dictionary<CurrencyType, bool> PersistsBetweenRuns
            = new Dictionary<CurrencyType, bool>
            {
                { CurrencyType.Crystals,        true  },
                { CurrencyType.ImbuedFruits,    false },
                { CurrencyType.PrimordialSeeds, false }
            };

        // ── Internal state ────────────────────────────────────────────────────

        private readonly Dictionary<CurrencyType, int> _balances
            = new Dictionary<CurrencyType, int>
            {
                { CurrencyType.Crystals,        0 },
                { CurrencyType.ImbuedFruits,    0 },
                { CurrencyType.PrimordialSeeds, 0 }
            };

        // All balance writes go through this lock so async save/load can't race with gameplay.
        private readonly object _lockObj = new object();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="amount"/> to the specified currency balance.
        /// </summary>
        /// <param name="type">The currency to add to.</param>
        /// <param name="amount">Must be greater than zero.</param>
        /// <returns><c>true</c> on success; <c>false</c> if <paramref name="amount"/> is invalid.</returns>
        public bool TryAdd(CurrencyType type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] TryAdd rejected: amount must be > 0, got {amount}.");
                return false;
            }

            int previous;
            int next;

            lock (_lockObj)
            {
                previous = _balances[type];
                next = previous + amount;
                _balances[type] = next;
            }

            FireEvent(type, previous, next);
            return true;
        }

        /// <summary>
        /// Removes <paramref name="amount"/> from the specified currency balance if funds are sufficient.
        /// Never results in a negative balance.
        /// </summary>
        /// <param name="type">The currency to remove from.</param>
        /// <param name="amount">Must be greater than zero.</param>
        /// <returns><c>true</c> on success; <c>false</c> if insufficient balance or invalid amount.</returns>
        public bool TryRemove(CurrencyType type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] TryRemove rejected: amount must be > 0, got {amount}.");
                return false;
            }

            int previous;
            int next;

            lock (_lockObj)
            {
                previous = _balances[type];

                if (previous < amount)
                    return false;

                next = previous - amount;
                _balances[type] = next;
            }

            FireEvent(type, previous, next);
            return true;
        }

        /// <summary>
        /// Returns the current balance for the specified currency.
        /// </summary>
        public int GetBalance(CurrencyType type)
        {
            lock (_lockObj)
            {
                return _balances[type];
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the current balance is at least <paramref name="cost"/>.
        /// Does not modify the balance.
        /// </summary>
        public bool CanAfford(CurrencyType type, int cost)
        {
            lock (_lockObj)
            {
                return _balances[type] >= cost;
            }
        }

        /// <summary>
        /// Directly sets the balance for a currency. Intended for save/load only (T039).
        /// Fires <see cref="OnCurrencyChanged"/> so the UI updates immediately after loading.
        /// </summary>
        /// <param name="type">The currency to set.</param>
        /// <param name="amount">Must be >= 0.</param>
        public void SetBalance(CurrencyType type, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[CurrencyManager] SetBalance rejected: amount must be >= 0, got {amount}.");
                return;
            }

            int previous;

            lock (_lockObj)
            {
                previous = _balances[type];
                _balances[type] = amount;
            }

            FireEvent(type, previous, amount);
        }

        /// <summary>
        /// Resets all per-run currencies (ImbuedFruits, PrimordialSeeds) to zero.
        /// Called at the start of each run. Fires one <see cref="OnCurrencyChanged"/> event
        /// per currency that was reset so the UI can update each independently.
        /// Crystals are never touched by this method.
        /// </summary>
        public void ResetPerRunCurrencies()
        {
            foreach (var pair in PersistsBetweenRuns)
            {
                if (pair.Value)
                    continue; // skip currencies that persist (Crystals)

                CurrencyType type = pair.Key;
                int previous;

                lock (_lockObj)
                {
                    previous = _balances[type];
                    _balances[type] = 0;
                }

                if (previous != 0)
                    FireEvent(type, previous, 0);
            }
        }

        /// <summary>
        /// Returns whether the specified currency persists between runs.
        /// Read by SaveSystem (T039) to determine what to serialize.
        /// </summary>
        public bool DoesPersistBetweenRuns(CurrencyType type)
        {
            return PersistsBetweenRuns[type];
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void FireEvent(CurrencyType type, int previous, int next)
        {
            OnCurrencyChanged?.Invoke(new CurrencyChangeEventData
            {
                currencyType   = type,
                previousAmount = previous,
                newAmount      = next,
                delta          = next - previous
            });
        }
    }
}
