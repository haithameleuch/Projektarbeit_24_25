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
    [SerializeField]
    private CinemachineCamera topDownCamera;

    /// <summary>
    /// The Cinemachine camera used for the first-person view.
    /// </summary>
    [SerializeField]
    private CinemachineCamera firstPersonCamera;

    /// <summary>
    /// The Cinemachine camera used for the canvas view.
    /// </summary>
    [SerializeField]
    public CinemachineCamera CanvCamera;

    /// <summary>
    /// The key used to toggle between the two camera views.
    /// </summary>
    [SerializeField]
    private KeyCode switchKey = KeyCode.LeftAlt;

    /// <summary>
    /// The key used to switch to the canvas view (Alt key).
    /// </summary>
    [SerializeField]
    private KeyCode canvasSwitchKey = KeyCode.Tab;

    /// <summary>
    /// The script controlling player movement in the top-down view.
    /// </summary>
    [SerializeField]
    private TopDownPlayerController topDownPlayerController;

    /// <summary>
    /// The script controlling player movement in the first-person view.
    /// </summary>
    [SerializeField]
    private FirstPersonPlayerController firstPersonPlayerController;

    /// <summary>
    /// The shooting script used for the top-down view.
    /// </summary>
    [SerializeField]
    private TopDownShooting topDownShooting;

    /// <summary>
    /// The shooting script used for the first-person view.
    /// </summary>
    [SerializeField]
    private PlayerShooting firstPersonShooting;

    /// <summary>
    /// The transform representing the shooting point for projectiles.
    /// </summary>
    [SerializeField]
    private Transform shootPoint;

    /// <summary>
    /// The offset of the shooting point in the top-down view.
    /// </summary>
    private Vector3 topDownShootPointOffset = Vector3.zero;

    /// <summary>
    /// The offset of the shooting point in the first-person view.
    /// </summary>
    private Vector3 firstPersonShootPointOffset = new Vector3(0, 0, 1);

    public GameObject player;
    Renderer playerRenderer;

    /// <summary>
    /// Initializes the starting camera view to the top-down perspective.
    /// </summary>
    private void Start()
    {
        SetTopDownView();
    }

    /// <summary>
    /// Monitors for input to toggle between the two camera views or switch to the canvas view.
    /// </summary>
    private void Update()
    {
        // Check for switching between top-down, first-person, or canvas view
        if (Input.GetKeyDown(switchKey))
        {
            // Check which camera is currently active and toggle the view
            if (
                topDownCamera.Priority > firstPersonCamera.Priority
                && topDownCamera.Priority > CanvCamera.Priority
            )
            {
                SetFirstPersonView();
            }
            else if (
                firstPersonCamera.Priority > topDownCamera.Priority
                && firstPersonCamera.Priority > CanvCamera.Priority
            )
            {
                SetCanvView();
            }
            else
            {
                SetTopDownView();
            }
        }

        // Check if Alt is pressed to switch to the canvas view
        if (Input.GetKeyDown(canvasSwitchKey))
        {
            SetCanvView();
        }
    }

    void Awake()
    {
        // Initialize the player GameObject in Awake or Start
        player = GameObject.FindWithTag("Player");
        playerRenderer = player.GetComponent<Renderer>();
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

    /// <summary>
    /// Activates the canvas view by setting the priority of the canvas camera higher.
    /// </summary>
    private void SetCanvView()
    {
        // Ensure the mouse cursor is always visible
        Cursor.visible = true;

        // Unlock the cursor so it can move freely
        Cursor.lockState = CursorLockMode.None;
        // Set camera priorities
        topDownCamera.Priority = 5;
        firstPersonCamera.Priority = 5;
        CanvCamera.Priority = 10;
        // Ensure the player is active but invisible in the canvas view
        GameObject player = GameObject.Find("Capsule");

        if (player != null)
        {
            // Get the player's current position
            Vector3 currentPosition = player.transform.position;

            // Modify the position (subtract 5 from the z-axis)
            currentPosition.z -= 5;

            // Assign the modified position back to the player
            player.transform.position = currentPosition;

            // Disable both movement and shooting scripts when in canvas view
            topDownPlayerController.enabled = false;
            firstPersonPlayerController.enabled = false;
            topDownShooting.enabled = false;
            firstPersonShooting.enabled = false;

            // Adjust the shooting point position to match the canvas view
            shootPoint.localPosition = Vector3.zero;
        }
    }
}
