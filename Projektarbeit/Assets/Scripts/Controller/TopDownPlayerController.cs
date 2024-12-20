using UnityEngine;

/// <summary>
/// Controls the movement of the player from top-down perspective, handling collisions with walls and obstacles.
/// </summary>
public class TopDownPlayerController : MonoBehaviour
{
    /// <summary>
    /// Reference to the input handler for capturing player movement inputs.
    /// </summary>
    [SerializeField] private GameInputManager gameInputManager;

    /// <summary>
    /// Movement speed of the player in units per second.
    /// </summary>
    [SerializeField] private float moveSpeed = 7f;

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
        float playerRadius = 0.7f;       // Radius of the player capsule.
        float playerHeight = 2f;         // Height of the player capsule.

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
