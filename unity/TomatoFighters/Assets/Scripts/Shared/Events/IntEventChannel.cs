using System;
using UnityEngine;

namespace TomatoFighters.Shared.Events
{
    /// <summary>
    /// Reusable ScriptableObject event channel carrying an <see cref="int"/> payload.
    /// Decouples publisher from subscriber — both reference the same SO asset.
    /// Used for: wave start (wave index).
    /// </summary>
    [CreateAssetMenu(fileName = "NewIntEvent", menuName = "TomatoFighters/Events/Int Event Channel", order = 1)]
    public class IntEventChannel : ScriptableObject
    {
        private Action<int> _onRaised;

        /// <summary>Subscribe a listener to this event channel.</summary>
        public void Register(Action<int> listener)
        {
            _onRaised += listener;
        }

        /// <summary>Unsubscribe a listener from this event channel.</summary>
        public void Unregister(Action<int> listener)
        {
            _onRaised -= listener;
        }

        /// <summary>Fire the event with an integer payload, notifying all registered listeners.</summary>
        public void Raise(int value)
        {
            _onRaised?.Invoke(value);
        }
    }
}
