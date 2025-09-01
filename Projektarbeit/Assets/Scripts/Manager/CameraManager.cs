using MiniGame;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Manages camera switching between first-person and canvas views and enables the corresponding player movement controls.
/// </summary>
public class CameraManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the CameraManager.
    /// </summary>
    public static CameraManager Instance { get; private set; }
    
    public static CanvasDraw ActiveCanvasDraw { get; private set; }
    
    /// <summary>
    /// The key used to toggle between views (for debugging only).
    /// </summary>
    [SerializeField]
    private KeyCode switchKey = KeyCode.Tab;
    
    /// <summary>
    /// The Cinemachine camera used for the canvas view.
    /// </summary>
    [SerializeField]
    private CinemachineCamera canvCamera;
    
    private CinemachineCamera _firstPersonCamera;
    private FirstPersonPlayerController _firstPersonPlayerController;
    private GameObject _player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        //DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// DEBUG ONLY: Allows switching back to FirstPersonView via key.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            SetFirstPersonView();
        }
    }
    
    public void SetActiveCanvas(CanvasDraw canvas)
    {
        ActiveCanvasDraw = canvas;
    }
    
    /// <summary>
    /// Sets the references to the player, its camera, and controller.
    /// </summary>
    public void SetPlayer(GameObject player)
    {
        _player = player;

        _firstPersonPlayerController = player.GetComponent<FirstPersonPlayerController>();
        _firstPersonCamera = player.transform.Find("FirstPersonCam")?.GetComponent<CinemachineCamera>();

        SetFirstPersonView();
    }

    /// <summary>
    /// Activates the first-person view.
    /// </summary>
    private void SetFirstPersonView()
    {
        GameInputManager.Instance.MouseLocked(true);
        
        if (canvCamera.gameObject.activeSelf)
            canvCamera.gameObject.SetActive(false);

        _firstPersonCamera.Priority = 10;
        canvCamera.Priority = 5;

        SetPlayerVisible(true);
        _firstPersonPlayerController.enabled = true;
        CanvasDraw.ToDraw = false;
    }

    /// <summary>
    /// Activates the canvas view.
    /// </summary>
    private void SetCanvView()
    {
        GameInputManager.Instance.MouseLocked(false);
        
        if (!canvCamera.gameObject.activeSelf)
            canvCamera.gameObject.SetActive(true);

        _firstPersonCamera.Priority = 5;
        canvCamera.Priority = 10;

        SetPlayerVisible(false);
        _firstPersonPlayerController.enabled = false;
        CanvasDraw.ToDraw = true;
    }

    /// <summary>
    /// Shows or hides all Renderer components of the player.
    /// </summary>
    /// <param name="visible">Whether the player should be visible.</param>
    private void SetPlayerVisible(bool visible)
    {
        if (_player is null) return;

        var renderers = _player.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer is not null)
                renderer.enabled = visible;
        }
    }
    
    /// <summary>
    /// Moves and orients the canvas camera to a specific target (e.g., the drawing center).
    /// </summary>
    public void SetCanvasTarget(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("SetCanvasTarget called with null target.");
            return;
        }
        
        // activate the canvas camera if it is disabled
        if (!canvCamera.gameObject.activeSelf)
            canvCamera.gameObject.SetActive(true);

        Vector3 offset = -target.forward * 2f;
        canvCamera.transform.position = target.position + offset;
        canvCamera.transform.LookAt(target);
    }

    /// <summary>
    /// Subscribes to the OnActivateCanvasView event.
    /// </summary>
    private void OnEnable()
    {
        EventManager.OnActivateCanvasView += SetCanvView;
    }

    /// <summary>
    /// Unsubscribes from the OnActivateCanvasView event.
    /// </summary>
    private void OnDisable()
    {
        EventManager.OnActivateCanvasView -= SetCanvView;
    }
}