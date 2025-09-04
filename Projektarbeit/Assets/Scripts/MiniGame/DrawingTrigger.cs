using MiniGame;
using UnityEngine;

/// <summary>
/// Script to handle interaction and triggering of drawing functionality.
/// </summary>
public class DrawingTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform center;

    // Method invoked when the player interacts with the object
    public void Interact(GameObject interactor)
    {
        UIManager.Instance.ShowPanel("Press [G] to Draw!");
        
        // Reference to the CanvasDraw of this object
        CanvasDraw canvas = GetComponent<CanvasDraw>();

        // If this is not already the active canvas, set the camera and mark it as active
        if (CameraManager.ActiveCanvasDraw != canvas)
        {
            CameraManager.Instance.SetCanvasTarget(center);
            CameraManager.Instance.SetActiveCanvas(canvas);
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            UIManager.Instance.ShowPanel(
                "1. Press [C] To erase.\n"
                + "2. Press [Right Click] to predict the digit.\n"
                + "3. Press [Left Click] to draw!"
            );

            EventManager.Instance.TriggerCanvasView();
        }
    }

    public void OnExit(GameObject interactor)
    {
        UIManager.Instance.HidePanel();
    }

    public bool ShouldRepeat()
    {
        return true;
    }
}