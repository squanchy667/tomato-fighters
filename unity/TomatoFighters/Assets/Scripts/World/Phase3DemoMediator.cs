using TomatoFighters.Shared.Events;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Phase 3 demo glue: bridges <c>onWaveCleared</c> events to the appropriate UI
    /// based on which wave just finished.
    ///
    /// <para><b>Wave 1 cleared →</b> raise <c>onShowRewardSelector</c> (ritual pick).</para>
    /// <para><b>Wave 2 cleared →</b> raise <c>onShowPathSelection</c> (path shrine).</para>
    /// <para><b>Wave 3 cleared →</b> no action (boss death triggers area complete via WaveManager).</para>
    ///
    /// <para>Because <see cref="RewardSelectorUI"/> and <see cref="PathSelectionUI"/> both
    /// pause the game with <c>Time.timeScale = 0</c>, the WaveManager coroutine freezes
    /// until the player makes their choice, naturally sequencing the demo loop.</para>
    /// </summary>
    public class Phase3DemoMediator : MonoBehaviour
    {
        [Header("Incoming")]
        [Tooltip("Fires each time a WaveManager wave is cleared.")]
        [SerializeField] private VoidEventChannel onWaveCleared;

        [Header("Outgoing")]
        [Tooltip("Raised after wave 1 to open the ritual reward screen.")]
        [SerializeField] private VoidEventChannel onShowRewardSelector;

        [Tooltip("Raised after wave 2 to open the path selection shrine.")]
        [SerializeField] private VoidEventChannel onShowPathSelection;

        private int _waveClearCount;

        private void OnEnable()
        {
            if (onWaveCleared != null)
                onWaveCleared.Register(HandleWaveCleared);
        }

        private void OnDisable()
        {
            if (onWaveCleared != null)
                onWaveCleared.Unregister(HandleWaveCleared);
        }

        private void HandleWaveCleared()
        {
            _waveClearCount++;

            switch (_waveClearCount)
            {
                case 1:
                    Debug.Log("[Phase3Demo] Wave 1 cleared → showing ritual reward selector.");
                    if (onShowRewardSelector != null)
                        onShowRewardSelector.Raise();
                    break;

                case 2:
                    Debug.Log("[Phase3Demo] Wave 2 cleared → showing path selection shrine.");
                    if (onShowPathSelection != null)
                        onShowPathSelection.Raise();
                    break;

                default:
                    Debug.Log($"[Phase3Demo] Wave {_waveClearCount} cleared → no UI trigger.");
                    break;
            }
        }
    }
}
