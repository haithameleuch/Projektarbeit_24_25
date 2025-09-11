using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// Static helper for handling player interactions with scene objects.
    /// Collects interactables from physics hits and triggers enter/exit calls.
    /// </summary>
    public static class InteractionHelper
    {
        /// <summary>
        /// Handles interactions for the current frame.
        /// Adds found interactables to <paramref name="newInteractables"/> and calls Interact
        /// either once or every frame, based on ShouldRepeat().
        /// </summary>
        /// <param name="hits">Detected physics hits (raycast, sphere cast, etc.).</param>
        /// <param name="newInteractables">List to fill with interactables found this frame.</param>
        /// <param name="currentInteractables">Interactables from the previous frame.</param>
        /// <param name="player">The player GameObject passed to interaction methods.</param>
        public static void HandleInteractions(RaycastHit[] hits, List<GameObject> newInteractables, List<GameObject> currentInteractables, GameObject player) 
        {
            // Create a set of IDs for all interactables that were already interacted with in the previous frame
            var currentIds = new HashSet<int>();
            foreach (var obj in currentInteractables)
            {
                if (obj != null)
                    currentIds.Add(obj.GetInstanceID());
            }

            // Track which interactables have already been processed this frame to avoid duplicates
            var processedIds = new HashSet<int>();

            // Loop through all detected hits to check if any objects are interactable
            foreach (var hit in hits)
            {
                // Always get the object that holds the IInteractable component (might not be the collider itself)
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                var interactableObject = ((MonoBehaviour)interactable).gameObject;
                var id = interactableObject.GetInstanceID();

                // Avoid processing the same interactable multiple times per frame
                if (processedIds.Contains(id)) continue;
                processedIds.Add(id);

                // Add the interactable object to the new list
                newInteractables.Add(interactableObject);

                if (interactable.ShouldRepeat())
                {
                    // Call Interact every frame if the interactable is repeatable
                    interactable.Interact(player);
                }
                else if (!currentIds.Contains(id))
                {
                    // Call Interact only once if the interactable was not interacted with in the previous frame
                    interactable.Interact(player);
                }
            }
        }

        /// <summary>
        /// Handles objects that are no longer interactable, triggering exit events and updating the list of current interactables.
        /// </summary>
        /// <param name="newInteractables">List of objects that are interactable in the current frame.</param>
        /// <param name="currentInteractables">List of objects that were interactable in the previous frame.</param>
        /// <param name="player">The GameObject, passed to interaction exit methods.</param>
        public static void HandleExits(List<GameObject> newInteractables, List<GameObject> currentInteractables, GameObject player)
        {
            // Loop through the current interactables in reverse to safely remove elements
            for (var i = currentInteractables.Count - 1; i >= 0; i--)
            {
                var obj = currentInteractables[i];

                // Check, if the object is null (destroyed)
                if (obj == null)
                {
                    currentInteractables.RemoveAt(i);
                    continue;
                }
            
                // If the object is not in the new interactables list, it is no longer interactable
                if (!newInteractables.Contains(obj))
                {
                    var interactable = obj.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.OnExit(player);
                    }
                    currentInteractables.RemoveAt(i);
                }
            }
        }
    }
}