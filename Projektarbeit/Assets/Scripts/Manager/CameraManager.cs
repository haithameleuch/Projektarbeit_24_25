using Controller;
using MiniGame;
using Unity.Cinemachine;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Manages camera switching between first-person and canvas views and enables the corresponding player movement controls.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the CameraManager.
        /// </summary>
        public static CameraManager Instance { get; private set; }
    
        /// <summary>
        /// Currently active canvas drawing component, if any.
        /// </summary>
        public static CanvasDraw ActiveCanvasDraw { get; private set; }
    
        /// <summary>
        /// Key to switch back to the first-person view.
        /// </summary>
        [SerializeField]
        private KeyCode switchKey = KeyCode.Tab;
    
        /// <summary>
        /// Cinemachine camera used for the canvas view.
        /// </summary>
        [SerializeField]
        private CinemachineCamera canvCamera;
    
        /// <summary>
        /// Player’s first-person Cinemachine camera.
        /// </summary>
        private CinemachineCamera _firstPersonCamera;
        
        /// <summary>
        /// Player movement/controller script.
        /// </summary>
        private FirstPersonPlayerController _firstPersonPlayerController;
        
        /// <summary>
        /// Player GameObject.
        /// </summary>
        private GameObject _player;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Allows switching back to FirstPersonView via a key.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(switchKey))
            {
                SetFirstPersonView();
            }
        }
    
        /// <summary>
        /// Sets the currently active canvas drawing component.
        /// </summary>
        /// <param name="canvas">CanvasDraw to mark as active.</param>
        public void SetActiveCanvas(CanvasDraw canvas)
        {
            ActiveCanvasDraw = canvas;
        }
    
        /// <summary>
        /// Assigns the player references (camera and controller) and enters first-person view.
        /// </summary>
        /// <param name="player">The player GameObject.</param>
        public void SetPlayer(GameObject player)
        {
            _player = player;

            _firstPersonPlayerController = player.GetComponent<FirstPersonPlayerController>();
            _firstPersonCamera = player.transform.Find("FirstPersonCam")?.GetComponent<CinemachineCamera>();

            SetFirstPersonView();
        }

        /// <summary>
        /// Activates the first-person camera and enables player movement.
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
        /// Activates the canvas camera and disables player movement.
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
            foreach (var r in renderers)
            {
                if (r is not null)
                    r.enabled = visible;
            }
        }
    
        /// <summary>
        /// Moves the canvas camera to look at a target (e.g., the drawing center).
        /// </summary>
        /// <param name="target">Transform to focus and face.</param>
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
}