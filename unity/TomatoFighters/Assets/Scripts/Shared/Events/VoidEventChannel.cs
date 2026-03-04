using System;
using UnityEngine;

namespace TomatoFighters.Shared.Events
{
    /// <summary>
    /// Reusable ScriptableObject event channel with no payload.
    /// Decouples publisher from subscriber — both reference the same SO asset.
    /// Used for: wave cleared, area complete, camera lock/unlock, bound reached.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVoidEvent", menuName = "TomatoFighters/Events/Void Event Channel", order = 0)]
    public class VoidEventChannel : ScriptableObject
    {
        private Action _onRaised;

        /// <summary>Subscribe a listener to this event channel.</summary>
        public void Register(Action listener)
        {
            _onRaised += listener;
        }

        /// <summary>Unsubscribe a listener from this event channel.</summary>
        public void Unregister(Action listener)
        {
            _onRaised -= listener;
        }

        /// <summary>Fire the event, notifying all registered listeners.</summary>
        public void Raise()
        {
            _onRaised?.Invoke();
        }
    }
}
