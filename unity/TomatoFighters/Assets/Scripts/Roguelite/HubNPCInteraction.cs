using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Base class for all Hub NPC interactable objects.
    ///
    /// <para>Attach a concrete subclass to an NPC GameObject in the Hub scene.
    /// When the player interacts, call <see cref="HubManager.InteractWithNPC"/>
    /// with <see cref="npcId"/> and then invoke <see cref="OnInteract"/> for
    /// NPC-specific local behaviour (animation, UI, dialogue trigger).</para>
    ///
    /// <para>New NPC types: subclass this, override <see cref="OnInteract"/>,
    /// and set a unique <see cref="npcId"/> in the Inspector.</para>
    /// </summary>
    public abstract class HubNPCInteraction : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier for this NPC. Must match the string used in
        /// <see cref="HubManager.InteractWithNPC"/> and any subscriber logic.
        /// Set this in the Inspector for each NPC instance.
        /// </summary>
        [Tooltip("Unique ID for this NPC — used by HubManager.InteractWithNPC and subscriber logic.")]
        public string npcId;

        /// <summary>
        /// Called when the player initiates interaction with this NPC.
        /// Override to implement NPC-specific behaviour: play animation,
        /// open shop panel, start dialogue sequence, etc.
        /// </summary>
        public abstract void OnInteract();
    }
}
