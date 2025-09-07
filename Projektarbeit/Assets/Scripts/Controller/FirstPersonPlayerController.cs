using System.Collections.Generic;
using UnityEngine;

namespace Controller
{
    /// <summary>
    /// First-person player controller for movement, looking, and basic interaction checks.
    /// Uses a capsule cast to block movement and slide along walls.
    /// Rotation is smoothed to reduce jitter. Pitch can be enabled for the camera.
    /// Also scans a small radius for interactable objects and updates a list.
    /// Can read speed from a Stats component if available.
    /// </summary>
    public class FirstPersonPlayerController : MonoBehaviour
    {
        /// <summary>
        /// Reference to the input handling system for player movement and look controls.
        /// </summary>
        [Header("References")]
        [SerializeField]
        private GameInputManager gameInput;

        /// <summary>
        /// Camera transform used for pitch rotation.
        /// </summary>
        [SerializeField] 
        private Transform cameraTransform;

        /// <summary>
        /// Movement speed.
        /// </summary>
        [Header("Movement Settings")]
        [SerializeField]
        private float moveSpeed = 7f;
    
        /// <summary>
        /// Index in the Stats list used to read the current speed. Falls back to moveSpeed if invalid.
        /// </summary>
        [SerializeField] 
        private int speedStatIndex = 2;

        /// <summary>
        /// Sensitivity of the mouse input for player rotation.
        /// </summary>
        [Header("Look Settings")]
        [SerializeField]
        private float mouseSensitivity = 0.2f;

        /// <summary>
        /// Smoothness factor for interpolating the player's rotation.
        /// </summary>
        [SerializeField]
        private float rotationSmoothness = 0.3f;

        /// <summary>
        /// If true, apply pitch to the camera. Otherwise, only yaw is applied to the body.
        /// </summary>
        [SerializeField] 
        private bool allowPitchRotation = true;

        /// <summary>
        /// Stats component used to get the current speed value.
        /// </summary>
        private Stats _stats;
    
        /// <summary>
        /// Current X-axis rotation of the player.
        /// </summary>
        private float _rotationX;
    
        /// <summary>
        /// Current Y-axis rotation of the player.
        /// </summary>
        private float _rotationY;

        /// <summary>
        /// Current smooth rotation state of the player.
        /// </summary>
        private Quaternion _currentRotation;
    
        /// <summary>
        /// Current Camera rotation state.
        /// </summary>
        private Quaternion _currentCameraRotation;
    
        /// <summary>
        /// Keeps track of the interactable objects the player is currently interacting with.
        /// </summary>
        private List<GameObject> _currentInteractables = new();

        /// <summary>
        /// Initializes the starting rotation of the player and get the Stats component.
        /// </summary>
        private void Start()
        {
            _currentRotation = transform.rotation;
            _currentCameraRotation = cameraTransform.localRotation;
        
            _stats = GetComponent<Stats>();
        }

        /// <summary>
        /// Handles player input for movement and rotation each frame. Also Scans for interactables.
        /// </summary>
        private void Update()
        {
            // Get normalized movement input and calculate the movement direction.
            var inputVector = gameInput.GetMovementVectorNormalized();
            var moveDir = transform.right * inputVector.x + transform.forward * inputVector.y;
            
            HandleMovement(moveDir);
            HandleRotation();
            CheckForObject();
        }

        /// <summary>
        /// Moves the player while checking for collisions and enabling smooth sliding along walls.
        /// </summary>
        /// <param name="moveDir">The direction vector for movement.</param>
        private void HandleMovement(Vector3 moveDir)
        {
            var speed = moveSpeed;
            if (_stats != null)
            {
                var curList = _stats.GetCurStatsList();
                if (speedStatIndex >= 0 && speedStatIndex < curList.Count)
                    speed = _stats.GetCurStats(speedStatIndex);
            }
        
            var moveDistance = speed * Time.deltaTime; // Maximum distance the player can move this frame.
            const float playerRadius = 0.85f; // Radius of the player's capsule for collision detection.
            const float playerHeight = 2f; // Height of the player's capsule for collision detection.

            // Check for collisions in the movement direction using CapsuleCast.
            if (
                Physics.CapsuleCast(
                    transform.position,
                    transform.position + Vector3.up * playerHeight,
                    playerRadius,
                    moveDir,
                    out RaycastHit hit,
                    moveDistance
                )
            )
            {
                // If a collision is detected, calculate the slide direction along the wall.
                if (hit.collider)
                {
                    Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal);

                    // Perform a secondary check to prevent sliding at sharp corners.
                    if (
                        Physics.CapsuleCast(
                            transform.position,
                            transform.position + Vector3.up * playerHeight,
                            playerRadius,
                            slideDir,
                            out var edgeHit,
                            moveDistance
                        )
                    )
                    {
                        slideDir = Vector3.zero; // Stop movement at sharp corners.
                    }

                    transform.position += slideDir * moveDistance;
                }
            }
            else
            {
                // If no collision is detected, move the player normally.
                transform.position += moveDir * moveDistance;
            }
        
            var pos = transform.position;
            pos.y = 1f;
            transform.position = pos;
        }

        /// <summary>
        /// Smoothly rotates the player based on mouse movement input.
        /// </summary>
        private void HandleRotation()
        {
            // Get mouse-look input.
            var lookDelta = gameInput.GetLookDelta();
            _rotationY += lookDelta.x * mouseSensitivity;
            if (lookDelta == Vector2.zero)
                return;
        
            // Calculate the target Yaw rotation based on mouse input.
            var targetYaw = Quaternion.Euler(0f, _rotationY, 0f);
        
            // Smoothly interpolate to the targetYaw rotation using Slerp.
            _currentRotation = Quaternion.Slerp(_currentRotation, targetYaw, rotationSmoothness);
            transform.rotation = _currentRotation;

            if (allowPitchRotation)
            {
                // Get the mouse-look input.
                _rotationX -= lookDelta.y * mouseSensitivity;
                _rotationX = Mathf.Clamp(_rotationX, -90f, 90f);
            
                // Calculate the target Pitch rotation based on mouse input.
                var targetPitch = Quaternion.Euler(_rotationX, 0f, 0f);
            
                // Smoothly interpolate to the targetPitch rotation using Slerp.
                _currentCameraRotation = Quaternion.Slerp(_currentCameraRotation, targetPitch, rotationSmoothness);
                cameraTransform.localRotation = _currentCameraRotation;
            }
        }

        /// <summary>
        /// Detects and interacts with any nearby interactable objects within a specified radius.
        /// </summary>
        private void CheckForObject()
        {
            // Define the radius within which we check for interactable objects
            const float pickupRadius = 2.0f;

            // Perform a SphereCast to detect all objects within the pickup radius, cast in the upward direction (Vector3.up)
            // The 0f range ensures we're only checking for objects at the current position
            var hits = Physics.SphereCastAll(transform.position, pickupRadius, Vector3.up, 0f);
            var newInteractables = new List<GameObject>();
        
            // Use the helper methods to manage interactions
            InteractionHelper.HandleInteractions(hits, newInteractables, _currentInteractables, gameObject);
            InteractionHelper.HandleExits(newInteractables, _currentInteractables, gameObject);

            // Update the list of current Interactable to reflect the new state
            _currentInteractables.Clear();
            _currentInteractables.AddRange(newInteractables);
        }
    
        /// <summary>
        /// Sets the input manager at runtime.
        /// </summary>
        /// <param name="inputManager">Input manager to use.</param>
        public void Init(GameInputManager inputManager)
        {
            gameInput = inputManager;
        }
    
        /// <summary>
        /// Syncs internal angles and cached rotations from the current transforms.
        /// Call this after loading or reparenting.
        /// </summary>
        public void SyncLoadedRotation()
        {
            _currentRotation       = transform.rotation;
            _currentCameraRotation = cameraTransform.localRotation;
        
            _rotationY = transform.eulerAngles.y;
            var pitchEuler = cameraTransform.localEulerAngles.x;
            _rotationX = pitchEuler > 180 ? pitchEuler - 360f : pitchEuler;
        }
    }
}