using UnityEngine;

public class DrawingTrigger : MonoBehaviour, IInteractable
{
    private Renderer playerRenderer; // Player's renderer
    private bool isCameraPositionSet = false; // Flag to ensure the position is only calculated once

    public void Interact(GameObject interactor)
    {
        // Find the "Point" GameObject
        GameObject myObject = GameObject.Find("Center");

        // Find the Cinemachine camera by its name
        GameObject cinemachineCamera = GameObject.FindWithTag("CanvCamera");

        if (cinemachineCamera != null)
        {
            if (!isCameraPositionSet)
            {
                // Set the new position based on the "Point" object
                Vector3 position = myObject.transform.position;
                position.z = myObject.transform.position.z - 2f; // Offset Z value
                
                // Apply the new position to the camera
                cinemachineCamera.transform.position = position;
                cinemachineCamera.transform.eulerAngles = myObject.transform.eulerAngles;

                isCameraPositionSet = true;
            }
            
            // Trigger the Canvas View event
            if (Input.GetKeyDown(KeyCode.G))
            {
                EventManager.Instance.TriggerCanvasView();
            }
        }
    }
    
    public void OnExit(GameObject interactor)
    {
        // UIManager.Instance.HidePanel();
    }

    public bool ShouldRepeat()
    {
        return true;
    }
}
