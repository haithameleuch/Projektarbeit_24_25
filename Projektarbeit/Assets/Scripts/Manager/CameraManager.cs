using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Manages camera switching between top-down and first-person views and enables the corresponding player movement controls.
/// </summary>
public class CameraManager : MonoBehaviour
{
    /// <summary>
    /// The Cinemachine camera used for the top-down view.
    /// </summary>
    [SerializeField] private CinemachineCamera topDownCamera;

    /// <summary>
    /// The Cinemachine camera used for the first-person view.
    /// </summary>
    [SerializeField] private CinemachineCamera firstPersonCamera;

    /// <summary>
    /// The key used to toggle between the two camera views.
    /// </summary>
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    /// <summary>
    /// The script controlling player movement in the top-down view.
    /// </summary>
    [SerializeField] private TopDownPlayerController topDownPlayerController;

    /// <summary>
    /// The script controlling player movement in the first-person view.
    /// </summary>
    [SerializeField] private FirstPersonPlayerController firstPersonPlayerController;

    /// <summary>
    /// Initializes the starting camera view to the top-down perspective.
    /// </summary>
    private void Start()
    {
        SetTopDownView();
    }

    /// <summary>
    /// Monitors for input to toggle between the two camera views.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            // Check which camera is currently active and toggle the view
            if (topDownCamera.Priority > firstPersonCamera.Priority)
            {
                SetFirstPersonView();
            }
            else
            {
                SetTopDownView();
            }
        }
    }

    /// <summary>
    /// Activates the top-down view by setting the priority of the top-down camera higher
    /// and enabling the appropriate movement control script.
    /// </summary>
    private void SetTopDownView()
    {
        // Set camera priorities
        topDownCamera.Priority = 10;
        firstPersonCamera.Priority = 5;

        // Enable the top-down movement script and disable the first-person script
        topDownPlayerController.enabled = true;
        firstPersonPlayerController.enabled = false;
    }

    /// <summary>
    /// Activates the first-person view by setting the priority of the first-person camera higher
    /// and enabling the appropriate movement control script.
    /// </summary>
    private void SetFirstPersonView()
    {
        // Set camera priorities
        topDownCamera.Priority = 5;
        firstPersonCamera.Priority = 10;

        // Enable the first-person movement script and disable the top-down script
        topDownPlayerController.enabled = false;
        firstPersonPlayerController.enabled = true;
    }
}
