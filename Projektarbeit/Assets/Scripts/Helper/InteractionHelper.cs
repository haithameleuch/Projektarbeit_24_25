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
        /*// Loop through all detected hits to check if any objects are interactable
        foreach (RaycastHit hit in hits)
        {
            GameObject interactableObject = hit.collider.gameObject;
            // Attempt to get the IInteractable component from the object that was hit
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            

            // If the object implements the IInteractable interface, interact with it
            if (interactable != null)
            {
                Debug.Log("interactable object name: " + interactableObject);
                Debug.Log("interactable: " + interactable);
                newInteractables.Add(interactableObject);

                if (interactable.ShouldRepeat())
                {
                    // Call Interact for every frame
                    interactable.Interact(player);
                    // Debug.Log("EVERY FRAME");
                }
                else if (!currentInteractables.Contains(interactableObject))
                {
                    // Call Interact only once
                    interactable.Interact(player);
                    // Debug.Log("SINGLE FRAME");
                }
            }
        }*/
    }

    /// <summary>
    /// Handles objects that are no longer interactable, triggering exit events and updating the list of current interactables.
    /// </summary>
    /// <param name="newInteractables">List of objects that are interactable in the current frame.</param>
    /// <param name="currentInteractables">List of objects that were interactable in the previous frame.</param>
    /// <param name="player">The GameObject, passed to interaction exit methods.</param>
    public static void HandleExits(List<GameObject> newInteractables, List<GameObject> currentInteractables, GameObject player)
    {
        /*// Loop through the current interactables in reverse to safely remove elements
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
        }*/
    }
}