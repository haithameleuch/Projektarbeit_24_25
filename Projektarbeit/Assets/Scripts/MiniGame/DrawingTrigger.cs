using Interfaces;
using Manager;
using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// Handles player interaction with a drawing canvas.
    /// Displays instructions, activates the camera for drawing,
    /// and triggers the canvas view when the player presses the draw key.
    /// Implements the IInteractable interface.
    /// </summary>
    public class DrawingTrigger : MonoBehaviour, IInteractable
    {
        /// <summary>
        /// The central point to focus the camera on when the canvas is active.
        /// </summary>
        [SerializeField] private Transform center;

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Called when the player interacts with the object.
        /// Displays instructions and activates the canvas for drawing.
        /// </summary>
        /// <param name="interactor">The GameObject player interacting with this object.</param>
        public void Interact(GameObject interactor)
        {
            if (!CanvasDraw.ToDraw)
            {
                UIManager.Instance.ShowPanel("Press [G] to Draw!");
            }

            var canvas = GetComponent<CanvasDraw>();

            if (CameraManager.ActiveCanvasDraw != canvas)
            {
                CameraManager.Instance.SetCanvasTarget(center);
                CameraManager.Instance.SetActiveCanvas(canvas);
            }

            if (!Input.GetKeyDown(KeyCode.G)) return;
            CanvasDraw.ToDraw = true; // Mark that drawing has started
            UIManager.Instance.ShowPanel(
                "1. Press [C] To erase.\n" +
                "2. Press [Right Click] to predict the digit.\n" +
                "3. Press [Left Click] to draw!"
            );

            EventManager.Instance.TriggerCanvasView();
        }

        /// <summary>
        /// Called when the player exits the interaction area.
        /// Hides any instruction panel related to drawing.
        /// </summary>
        /// <param name="interactor">The GameObject player exiting the interaction.</param>
        public void OnExit(GameObject interactor)
        {
            UIManager.Instance.HidePanel();
        }

        /// <summary>
        /// Determines whether the interaction can be repeated.
        /// </summary>
        /// <returns>Always returns true, allowing repeated interaction.</returns>
        public bool ShouldRepeat()
        {
            return true;
        }
    }
}