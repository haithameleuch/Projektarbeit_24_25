using UnityEngine;

/// <summary>
/// Controls first-person movement and rotation with collision detection.
/// Enables smooth sliding along walls and avoids jitter when moving against obstacles.
/// </summary>
public class FirstPersonPlayerController : MonoBehaviour
{
    /// <summary>
    /// Reference to the input handling system for player movement and look controls.
    /// </summary>
    [SerializeField] private GameInputManager gameInput;

    /// <summary>
    /// Speed of the player's movement.
    /// </summary>
    [SerializeField] private float moveSpeed = 7f;

    /// <summary>
    /// Sensitivity of the mouse input for player rotation.
    /// </summary>
    [SerializeField] private float mouseSensitivity = 0.2f;

    /// <summary>
    /// Smoothness factor for interpolating the player's rotation.
    /// </summary>
    [SerializeField] private float rotationSmoothness = 0.3f;

    /// <summary>
    /// Current Y-axis rotation of the player.
    /// </summary>
    private float _rotationY;

    /// <summary>
    /// Current smooth rotation state of the player.
    /// </summary>
    private Quaternion _currentRotation;

    /// <summary>
    /// Initializes the starting rotation of the player.
    /// </summary>
    private void Start()
    {
        _currentRotation = transform.rotation;
    }

    /// <summary>
    /// Handles player input for movement and rotation each frame.
    /// </summary>
    private void Update()
    {
        // Get normalized movement input and calculate movement direction.
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        Vector3 moveDir = transform.right * inputVector.x + transform.forward * inputVector.y;

        // Apply movement with collision detection.
        HandleMovement(moveDir);

        // Apply mouse-based rotation.
        HandleRotation();
    }

    /// <summary>
    /// Moves the player while checking for collisions and enabling smooth sliding along walls.
    /// </summary>
    /// <param name="moveDir">The direction vector for movement.</param>
    private void HandleMovement(Vector3 moveDir)
    {
        float moveDistance = moveSpeed * Time.deltaTime;    // Maximum distance the player can move this frame.
        float playerRadius = 0.95f;                         // Radius of the player's capsule for collision detection.
        float playerHeight = 2f;                            // Height of the player's capsule for collision detection.

        // Check for collisions in the movement direction using CapsuleCast.
        if (Physics.CapsuleCast(
                transform.position,
                transform.position + Vector3.up * playerHeight,
                playerRadius,
                moveDir,
                out RaycastHit hit,
                moveDistance))
        {
            // If a collision is detected, calculate the slide direction along the wall.
            if (hit.collider)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal);

                // Perform a secondary check to prevent sliding at sharp corners.
                if (Physics.CapsuleCast(
                        transform.position,
                        transform.position + Vector3.up * playerHeight,
                        playerRadius,
                        slideDir,
                        out RaycastHit edgeHit,
                        moveDistance))
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
    }

    /// <summary>
    /// Smoothly rotates the player based on mouse movement input.
    /// </summary>
    private void HandleRotation()
    {
        // Get the mouse look input.
        Vector2 lookDelta = gameInput.GetLookDelta();
        _rotationY += lookDelta.x * mouseSensitivity;

        // Calculate the target rotation based on mouse input.
        Quaternion targetRotation = Quaternion.Euler(0f, _rotationY, 0f);

        // Smoothly interpolate to the target rotation using Slerp.
        _currentRotation = Quaternion.Slerp(_currentRotation, targetRotation, rotationSmoothness);
        transform.rotation = _currentRotation;
    }
}