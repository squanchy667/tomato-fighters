using System;
using UnityEngine;

namespace TomatoFighters.Shared.Events
{
    /// <summary>
    /// Reusable ScriptableObject event channel carrying a <see cref="float"/> payload.
    /// Decouples publisher from subscriber — both reference the same SO asset.
    /// Used for: health changed (normalized 0-1), mana changed (normalized 0-1).
    /// </summary>
    [CreateAssetMenu(fileName = "NewFloatEvent", menuName = "TomatoFighters/Events/Float Event Channel", order = 2)]
    public class FloatEventChannel : ScriptableObject
    {
        private Action<float> _onRaised;

        /// <summary>Subscribe a listener to this event channel.</summary>
        public void Register(Action<float> listener)
        {
            _onRaised += listener;
        }

        /// <summary>Unsubscribe a listener from this event channel.</summary>
        public void Unregister(Action<float> listener)
        {
            _onRaised -= listener;
        }

        /// <summary>Fire the event with a float payload, notifying all registered listeners.</summary>
        public void Raise(float value)
        {
            _onRaised?.Invoke(value);
        }
    }
}
