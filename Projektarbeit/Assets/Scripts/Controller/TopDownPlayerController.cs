using UnityEngine;

/// <summary>
/// Controls the movement of the player from top-down perspective, handling collisions with walls and obstacles.
/// </summary>
public class TopDownPlayerController : MonoBehaviour
{
    /// <summary>
    /// Reference to the input handler for capturing player movement inputs.
    /// </summary>
    [SerializeField]
    private GameInputManager gameInputManager;

    /// <summary>
    /// Movement speed of the player in units per second.
    /// </summary>
    [SerializeField]
    private float moveSpeed = 7f;

    /// <summary>
    /// Updates the player's movement each frame based on input and collision detection.
    /// </summary>
    private void Update()
    {
        // Get normalized movement input from the GameInputManager.
        Vector2 inputVector = gameInputManager.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        // Calculate the maximum distance the player can move in this frame.
        float moveDistance = moveSpeed * Time.deltaTime;

        // Define the player's physical dimensions for collision detection.
        float playerRadius = 0.7f; // Radius of the player capsule.
        float playerHeight = 2f; // Height of the player capsule.

        // Check if the player can move in the intended direction without hitting obstacles.
        bool canMove = !Physics.CapsuleCast(
            transform.position,
            transform.position + Vector3.up * playerHeight,
            playerRadius,
            moveDir,
            moveDistance
        );

        if (!canMove)
        {
            // If forward movement is blocked, attempt movement along the X-axis only.
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = !Physics.CapsuleCast(
                transform.position,
                transform.position + Vector3.up * playerHeight,
                playerRadius,
                moveDirX,
                moveDistance
            );

            if (canMove)
            {
                // If X-axis movement is possible, set the movement direction to X only.
                moveDir = moveDirX;
            }
            else
            {
                // If X-axis movement is also blocked, attempt movement along the Z-axis only.
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = !Physics.CapsuleCast(
                    transform.position,
                    transform.position + Vector3.up * playerHeight,
                    playerRadius,
                    moveDirZ,
                    moveDistance
                );

                if (canMove)
                {
                    // If Z-axis movement is possible, set the movement direction to Z only.
                    moveDir = moveDirZ;
                }
                // If both X and Z movements are blocked, the player remains stationary.
            }
        }

        // Move the player if movement is not obstructed.
        if (canMove)
        {
            transform.position += moveDir * moveDistance;
        }
        CheckForObject();
        // Call Drawing interactable
        //Drawing();
    }

    /// <summary>
    /// Detects and interacts with any nearby interactable objects within a specified radius.
    /// </summary>
    private void Drawing()
    {
        // Define the radius within which we check for interactable objects
        float pickupRadius = 1f;

        // Perform a SphereCast to detect all objects within the pickup radius, cast in the upward direction (Vector3.up)
        // The 0f range ensures we're only checking for objects at the current position
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, pickupRadius, Vector3.up, 0f);

        // Loop through all detected hits to check if any objects are interactable
        foreach (RaycastHit hit in hits)
        {
            // Attempt to get the IInteractable component from the object that was hit
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            // If the object implements the IInteractable interface, interact with it
            if (interactable is not null)
            {
                // Call the Interact method on the interactable object, passing this gameObject as the interacting object
                interactable.Interact(gameObject);
            }
        }
    }

    private void CheckForObject()
    {
        float pickupRadius = 1.0f;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, pickupRadius, Vector3.up, 0f);
        foreach (RaycastHit hit in hits)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable is not null)
            {
                interactable.Interact(gameObject);
            }
        }
    }
}
