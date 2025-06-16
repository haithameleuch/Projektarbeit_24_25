using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Provides static helper methods for handling player interactions with objects in the game world.
/// </summary>
public static class InteractionHelper
{
    /// <summary>
    /// Handles interactions with objects detected by a raycast.
    /// Updates the list of new interactables and performs interactions as needed.
    /// </summary>
    /// <param name="hits">Array of RaycastHit results from a raycast.</param>
    /// <param name="newInteractables">List to store objects that are interactable in the current frame.</param>
    /// <param name="currentInteractables">List of objects that were interactable in the previous frame.</param>
    /// <param name="player">The GameObject, passed to interaction methods.</param>
    public static void HandleInteractions(RaycastHit[] hits, List<GameObject> newInteractables, List<GameObject> currentInteractables, GameObject player) 
    {
        // Create a set of IDs for all interactables that were already interacted with in the previous frame
        HashSet<int> currentIds = new HashSet<int>();
        foreach (var obj in currentInteractables)
        {
            if (obj != null)
                currentIds.Add(obj.GetInstanceID());
        }

        // Track which interactables have already been processed this frame to avoid duplicates
        HashSet<int> processedIds = new HashSet<int>();

        // Loop through all detected hits to check if any objects are interactable
        foreach (RaycastHit hit in hits)
        {
            // Always get the object that holds the IInteractable component (might not be the collider itself)
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            GameObject interactableObject = ((MonoBehaviour)interactable).gameObject;
            int id = interactableObject.GetInstanceID();

            // Avoid processing the same interactable multiple times per frame
            if (processedIds.Contains(id)) continue;
            processedIds.Add(id);

            // Add the interactable object to the new list
            newInteractables.Add(interactableObject);

            if (interactable.ShouldRepeat())
            {
                // Call Interact every frame if the interactable is repeatable
                interactable.Interact(player);
                //Debug.Log("EVERY FRAME");
            }
            else if (!currentIds.Contains(id))
            {
                // Call Interact only once if the interactable was not interacted with in the previous frame
                interactable.Interact(player);
                //Debug.Log("SINGLE FRAME");
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
        for (int i = currentInteractables.Count - 1; i >= 0; i--)
        {
            GameObject obj = currentInteractables[i];

            // Check, if object is null (destroyed)
            if (obj == null)
            {
                currentInteractables.RemoveAt(i);
                continue;
            }
            
            // If the object is not in the new interactables list, it is no longer interactable
            if (!newInteractables.Contains(obj))
            {
                IInteractable interactable = obj.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    // Trigger the OnExit method for the object
                    interactable.OnExit(player);
                }
                currentInteractables.RemoveAt(i);
            }
        }
    }
}