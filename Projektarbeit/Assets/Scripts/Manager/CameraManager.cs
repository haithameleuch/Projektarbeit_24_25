using Unity.Cinemachine;
using UnityEngine;
using static CanvasDraw;

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
    private CinemachineCamera canvCamera;

    /// <summary>
    /// The key used to toggle between the two camera views.
    /// </summary>
    [SerializeField]
    private KeyCode switchKey = KeyCode.Tab;

    /// <summary>
    /// The key used to toggle between the painting camera and global camera.
    /// </summary>
    [SerializeField]
    private KeyCode canvSwitchKey = KeyCode.G;

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

    /// <summary>
    /// Stores the player's renderer components.
    /// </summary>
    private Renderer[] playerRenderers;

    /// <summary>
    /// Initializes the starting camera view to the top-down perspective.
    /// </summary>
    private void Start()
    {
        GameObject player = GameObject.Find("Capsule");
        if (player != null)
        {
            playerRenderers = player.GetComponentsInChildren<Renderer>();
        }

        SetTopDownView();
    }

    /// <summary>
    /// Monitors for input to toggle between the two camera views.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
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
    /// Activates the top-down view.
    /// </summary>
    private void SetTopDownView()
    {
        SetPlayerVisible(true);

        topDownCamera.Priority = 10;
        firstPersonCamera.Priority = 5;

        topDownPlayerController.enabled = true;
        firstPersonPlayerController.enabled = false;
        topDownShooting.enabled = true;
        firstPersonShooting.enabled = false;
        CanvasDraw.draw = false;

        shootPoint.localPosition = topDownShootPointOffset;
    }

    /// <summary>
    /// Activates the first-person view.
    /// </summary>
    private void SetFirstPersonView()
    {
        SetPlayerVisible(true);

        topDownCamera.Priority = 5;
        firstPersonCamera.Priority = 10;

        topDownPlayerController.enabled = false;
        firstPersonPlayerController.enabled = true;
        topDownShooting.enabled = false;
        firstPersonShooting.enabled = true;
        CanvasDraw.draw = false;

        shootPoint.localPosition = firstPersonShootPointOffset;
    }

    /// <summary>
    /// Activates the canvas view.
    /// </summary>
    private void SetCanvView()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        topDownCamera.Priority = 5;
        firstPersonCamera.Priority = 5;
        canvCamera.Priority = 10;

        SetPlayerVisible(false);
        topDownPlayerController.enabled = false;
        firstPersonPlayerController.enabled = false;
        topDownShooting.enabled = false;
        firstPersonShooting.enabled = false;
        CanvasDraw.draw = true;

        shootPoint.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Toggles the visibility of the player.
    /// </summary>
    /// <param name="visible">Whether the player should be visible.</param>
    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderers != null)
        {
            foreach (Renderer renderer in playerRenderers)
            {
                renderer.enabled = visible;
            }
        }
    }

    /// <summary>
    /// Called when the script instance is being enabled.
    /// Subscribes to the OnActivateCanvasView event to ensure the camera switches to Canvas View
    /// whenever the event is triggered.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to the Canvas View activation event
        EventManager.OnActivateCanvasView += SetCanvView;
    }

    /// <summary>
    /// Called when the script instance is being disabled.
    /// Unsubscribes from the OnActivateCanvasView event to prevent potential memory leaks
    /// or unexpected behavior if the event is triggered while the script is inactive.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks
        EventManager.OnActivateCanvasView -= SetCanvView;
    }
}
