using UnityEngine;

// Script to handle interaction and triggering of drawing functionality
public class DrawingTrigger : MonoBehaviour, IInteractable
{
    private Renderer _playerRenderer; // Player's renderer (unused, could be removed if unnecessary)
    private bool _isCameraPositionSet; // Flag to ensure the camera position is set only once

    // Method invoked when the player interacts with the object
    // ReSharper disable Unity.PerformanceAnalysis
    public void Interact(GameObject interactor)
    {
        // Find the "Center" GameObject, which determines the camera's new position
        GameObject myObject = GameObject.Find("Center");
		Debug.Log("drawing");

        // Find the Cinemachine camera using the "CanvCamera" tag
        GameObject cinemachineCamera = GameObject.FindWithTag("CanvCamera");

        // Display a UI panel instructing the player to press [G] to draw
        UIManager.Instance.ShowPanel("Press [G] to Draw!");

        // Check if the Cinemachine camera exists
        if (cinemachineCamera != null)
        {
            // Ensure the camera's position and rotation are set only once
            if (!_isCameraPositionSet)
            {
                // Get the direction orthogonal to the canvas (its "back" direction)
                Vector3 offsetDirection = -myObject.transform.forward; // or +forward depending on camera setup

                // Calculate the new position: 2 units away from the canvas along the normal
                Vector3 position = myObject.transform.position + offsetDirection * 2f;

                // Apply the calculated position and rotation to the camera
                cinemachineCamera.transform.position = position;

                // Make the camera look at the canvas
                cinemachineCamera.transform.LookAt(myObject.transform);

                // Mark the camera position as set
                _isCameraPositionSet = true;
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
