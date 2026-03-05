using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Placed on the Sprite child (same GameObject as the Animator) to relay
    /// Animation Events to components on the parent root GameObject.
    /// Unity only dispatches animation events to the Animator's own GameObject,
    /// so this relay bridges to HitboxManager and ComboController on the parent.
    /// </summary>
    public class AnimationEventRelay : MonoBehaviour
    {
        private HitboxManager _hitboxManager;
        private ComboController _comboController;

        private void Awake()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                Debug.LogError("[AnimationEventRelay] No parent found. This component must be on a child GameObject.", this);
                return;
            }

            _hitboxManager = parent.GetComponent<HitboxManager>();
            _comboController = parent.GetComponent<ComboController>();
        }

        // ── Animation Event callbacks (called by .anim clips) ──

        public void ActivateHitbox()
        {
            if (_hitboxManager != null)
                _hitboxManager.ActivateHitbox();
        }

        public void DeactivateHitbox()
        {
            if (_hitboxManager != null)
                _hitboxManager.DeactivateHitbox();
        }

        public void OnComboWindowOpen()
        {
            if (_comboController != null)
                _comboController.OnComboWindowOpen();
        }

        public void OnFinisherEnd()
        {
            if (_comboController != null)
                _comboController.OnFinisherEnd();
        }
    }
}
