using TomatoFighters.Shared.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace TomatoFighters.World.UI
{
    /// <summary>
    /// Displays the player's current Main and Secondary path selections.
    /// Queries <see cref="IPathProvider"/> (Shared interface) on initialization.
    /// Shows empty slots when paths are not yet selected.
    /// </summary>
    public class PathIndicatorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text mainPathText;
        [SerializeField] private Text secondaryPathText;
        [SerializeField] private Text mainTierText;
        [SerializeField] private Text secondaryTierText;

        [Header("Colors")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private IPathProvider _pathProvider;

        /// <summary>
        /// Initialize with a path provider reference. Called by HUDManager.
        /// </summary>
        public void Initialize(IPathProvider pathProvider)
        {
            _pathProvider = pathProvider;
            Refresh();
        }

        /// <summary>
        /// Refresh the display. Call after path selection or tier-up events.
        /// </summary>
        public void Refresh()
        {
            if (_pathProvider == null)
            {
                SetEmpty();
                return;
            }

            // Main path
            if (_pathProvider.MainPath != null)
            {
                if (mainPathText != null)
                {
                    mainPathText.text = _pathProvider.MainPath.pathType.ToString();
                    mainPathText.color = activeColor;
                }
                if (mainTierText != null)
                    mainTierText.text = $"T{_pathProvider.MainPathTier}";
            }
            else
            {
                if (mainPathText != null)
                {
                    mainPathText.text = "---";
                    mainPathText.color = emptyColor;
                }
                if (mainTierText != null)
                    mainTierText.text = "";
            }

            // Secondary path
            if (_pathProvider.SecondaryPath != null)
            {
                if (secondaryPathText != null)
                {
                    secondaryPathText.text = _pathProvider.SecondaryPath.pathType.ToString();
                    secondaryPathText.color = activeColor;
                }
                if (secondaryTierText != null)
                    secondaryTierText.text = $"T{_pathProvider.SecondaryPathTier}";
            }
            else
            {
                if (secondaryPathText != null)
                {
                    secondaryPathText.text = "---";
                    secondaryPathText.color = emptyColor;
                }
                if (secondaryTierText != null)
                    secondaryTierText.text = "";
            }
        }

        private void SetEmpty()
        {
            if (mainPathText != null)
            {
                mainPathText.text = "---";
                mainPathText.color = emptyColor;
            }
            if (secondaryPathText != null)
            {
                secondaryPathText.text = "---";
                secondaryPathText.color = emptyColor;
            }
            if (mainTierText != null) mainTierText.text = "";
            if (secondaryTierText != null) secondaryTierText.text = "";
        }
    }
}
