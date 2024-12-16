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
    /// The shooting script used for the top-down view.
    /// </summary>
    [SerializeField] private TopDownShooting topDownShooting;
    
    /// <summary>
    /// The shooting script used for the first-person view.
    /// </summary>
    [SerializeField] private PlayerShooting firstPersonShooting;
    
    /// <summary>
    /// The transform representing the shooting point for projectiles.
    /// </summary>
    [SerializeField] private Transform shootPoint;
    
    /// <summary>
    /// The offset of the shooting point in the top-down view.
    /// </summary>
    private Vector3 topDownShootPointOffset = Vector3.zero;
    
    /// <summary>
    /// The offset of the shooting point in the first-person view.
    /// </summary>
    private Vector3 firstPersonShootPointOffset = new Vector3(0, 0, 1);
    
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

        // Enable the top-down movement and shooting scripts, disable the first-person ones
        topDownPlayerController.enabled = true;
        firstPersonPlayerController.enabled = false;
        topDownShooting.enabled = true;
        firstPersonShooting.enabled = false;

        // Adjust the shooting point position to match the top-down view
        shootPoint.localPosition = topDownShootPointOffset;
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

        // Enable the first-person movement and shooting scripts, disable the top-down ones
        topDownPlayerController.enabled = false;
        firstPersonPlayerController.enabled = true;
        topDownShooting.enabled = false;
        firstPersonShooting.enabled = true;

        // Adjust the shooting point position to match the first-person view
        shootPoint.localPosition = firstPersonShootPointOffset;
    }
}
