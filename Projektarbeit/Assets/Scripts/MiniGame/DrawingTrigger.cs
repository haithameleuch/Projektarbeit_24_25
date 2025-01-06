using UnityEngine;

// Script to handle interaction and triggering of drawing functionality
public class DrawingTrigger : MonoBehaviour, IInteractable
{
    private Renderer playerRenderer; // Player's renderer (unused, could be removed if unnecessary)
    private bool isCameraPositionSet = false; // Flag to ensure the camera position is set only once

    // Method invoked when the player interacts with the object
    public void Interact(GameObject interactor)
    {
        // Find the "Center" GameObject, which determines the camera's new position
        GameObject myObject = GameObject.Find("Center");

        // Find the Cinemachine camera using the "CanvCamera" tag
        GameObject cinemachineCamera = GameObject.FindWithTag("CanvCamera");

        // Display a UI panel instructing the player to press [G] to draw
        UIManager.Instance.ShowPanel("Press [G] to Draw!");

        // Check if the Cinemachine camera exists
        if (cinemachineCamera != null)
        {
            // Ensure the camera's position and rotation are set only once
            if (!isCameraPositionSet)
            {
                // Set the new position based on the "Center" object's position, with an offset in the Z-axis
                Vector3 position = myObject.transform.position;
                position.z = myObject.transform.position.z - 2f; // Offset Z value to position the camera correctly

                // Apply the calculated position and rotation to the camera
                cinemachineCamera.transform.position = position;
                cinemachineCamera.transform.eulerAngles = myObject.transform.eulerAngles;

                // Mark the camera position as set
                isCameraPositionSet = true;
            }

            // Check if the player presses the [G] key to trigger the drawing mode
            if (Input.GetKeyDown(KeyCode.G))
            {
                // Update the UI panel with instructions for drawing mode
                UIManager.Instance.ShowPanel(
                    "1. Press [C] To erase.\n"
                        + "2. Press [Right Click] to predict the digit.\n"
                        + "3. Press [Left Click] to draw!"
                );

                // Trigger the event for transitioning to canvas view
                EventManager.Instance.TriggerCanvasView();
            }
        }
    }

    // Method invoked when the player exits the interaction zone
    public void OnExit(GameObject interactor)
    {
        // Hide any active UI panels
        UIManager.Instance.HidePanel();
    }

    // Determine whether the interaction should repeat
    public bool ShouldRepeat()
    {
        return true; // Always allow repeated interactions
    }
}
