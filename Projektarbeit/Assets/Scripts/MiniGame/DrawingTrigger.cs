using UnityEngine;

public class DrawingTrigger : MonoBehaviour, IInteractable
{
    private Renderer playerRenderer; // Player's renderer

    public void Interact(GameObject interactor)
    {
        // Find the "Point" GameObject
        GameObject myObject = GameObject.Find("Center");

        // Find the Cinemachine camera by its name
        GameObject cinemachineCamera = GameObject.FindWithTag("CanvCamera");

        if (cinemachineCamera != null)
        {
            // Update the camera's position
            Vector3 position = cinemachineCamera.transform.position;

            // Set the new position based on the "Point" object
            position = myObject.transform.position;
            position.z = myObject.transform.position.z - 2f; // Offset Z value
            // Apply the new position to the camera
            cinemachineCamera.transform.position = position;
            cinemachineCamera.transform.eulerAngles = myObject.transform.eulerAngles;
            // Trigger the Canvas View event
            if (Input.GetKeyDown(KeyCode.G))
            {
                EventManager.Instance.TriggerCanvasView();
            }
        }
    }
}
