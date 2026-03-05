using System;
using TomatoFighters.Shared.Data;
using UnityEngine;

namespace TomatoFighters.Shared.Events
{
    /// <summary>
    /// ScriptableObject event channel carrying a <see cref="RewardSelectedData"/> payload.
    /// Decouples publisher from subscriber — both reference the same SO asset.
    /// Used for: reward selector fires after player picks a post-area reward.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRewardSelectedEvent", menuName = "TomatoFighters/Events/Reward Selected Event Channel", order = 3)]
    public class RewardSelectedEventChannel : ScriptableObject
    {
        private Action<RewardSelectedData> _onRaised;

        /// <summary>Subscribe a listener to this event channel.</summary>
        public void Register(Action<RewardSelectedData> listener)
        {
            _onRaised += listener;
        }

        /// <summary>Unsubscribe a listener from this event channel.</summary>
        public void Unregister(Action<RewardSelectedData> listener)
        {
            _onRaised -= listener;
        }

        /// <summary>Fire the event with a reward selection payload, notifying all registered listeners.</summary>
        public void Raise(RewardSelectedData data)
        {
            _onRaised?.Invoke(data);
        }
    }
}
