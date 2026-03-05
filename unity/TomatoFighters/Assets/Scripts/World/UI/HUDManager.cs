using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.World.UI
{
    /// <summary>
    /// Root component for the player HUD (screen-space overlay Canvas).
    /// Owns references to all sub-components and handles show/hide.
    /// Wired via <see cref="UnityEngine.SerializeField"/> — no singletons.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Sub-Components")]
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private ManaBarUI manaBar;
        [SerializeField] private ComboCounterUI comboCounter;
        [SerializeField] private PathIndicatorUI pathIndicator;

        [Header("Path Provider")]
        [SerializeField]
        [Tooltip("Assign the GameObject that implements IPathProvider (e.g. PathSystem). " +
                 "Null is safe — path indicator shows empty slots.")]
        private Component pathProviderComponent;

        private void Start()
        {
            // Wire path indicator to the IPathProvider if available
            if (pathIndicator != null)
            {
                var provider = pathProviderComponent as IPathProvider;
                pathIndicator.Initialize(provider);
            }
        }

        /// <summary>Show all HUD elements.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Hide all HUD elements (e.g. during cutscenes or menus).</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Refresh the path indicator display. Call after path selection or tier-up.
        /// </summary>
        public void RefreshPaths()
        {
            if (pathIndicator != null)
                pathIndicator.Refresh();
        }
    }
}
