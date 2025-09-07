// LEGACY CODE !!!

using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the camera movement to smoothly follow the player as they move between rooms.
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>
    /// Reference to the player's Transform component.
    /// </summary>
    [SerializeField]
    private Transform player;

    /// <summary>
    /// The offset from the center of each room on the X and Z axes.
    /// </summary>
    [SerializeField]
    private Vector2 roomOffset = new(9f, 5f);

    /// <summary>
    /// The height of the camera above the current room.
    /// </summary>
    [SerializeField]
    private float cameraHeight = 12f;

    /// <summary>
    /// The duration of the camera's smooth transition between rooms.
    /// </summary>
    [SerializeField]
    private float transitionDuration = 0.5f;

    /// <summary>
    /// Stores the current room coordinates of the player.
    /// </summary>
    private Vector2Int _currentRoom;

    /// <summary>
    /// Reference to the currently active camera transition coroutine.
    /// </summary>
    private Coroutine _currentTransition;

    /// <summary>
    /// Initializes the camera position over the starting room.
    /// </summary>
    private void Start()
    {
        UpdateCameraPosition();
        GameManager.Instance.HandleRoomEntry(_currentRoom);
    }

    /// <summary>
    /// Monitors the player's movement between rooms and updates the camera position when necessary.
    /// </summary>
    private void Update()
    {
        // Determine the player's current room based on their position and room dimensions.
        Vector2Int newRoom = new Vector2Int(
            Mathf.FloorToInt((player.position.x + roomOffset.x) / (roomOffset.x * 2)),
            Mathf.FloorToInt((-player.position.z + roomOffset.y) / (roomOffset.y * 2))
        );

        // Update the camera position only if the player has entered a new room.
        if (newRoom != _currentRoom)
        {
            _currentRoom = newRoom;
            UpdateCameraPosition();
            GameManager.Instance.HandleRoomEntry(_currentRoom);
        }
    }

    /// <summary>
    /// Updates the camera's target position based on the player's current room and starts a smooth transition.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Calculate the target position for the camera.
        Vector3 targetPosition = new Vector3(
            _currentRoom.x * (roomOffset.x * 2),
            cameraHeight,
            -(_currentRoom.y * (roomOffset.y * 2))
        );

        // Stop any ongoing transition and start a new one.
        if (_currentTransition != null)
        {
            StopCoroutine(_currentTransition);
        }
        _currentTransition = StartCoroutine(SmoothTransition(targetPosition));
    }

    /// <summary>
    /// Smoothly transitions the camera's position to the specified target position.
    /// </summary>
    /// <param name="targetPosition">The destination position for the camera.</param>
    /// <returns>An enumerator used for coroutine execution.</returns>
    private IEnumerator SmoothTransition(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float time = 0;

        while (time < transitionDuration)
        {
            // Interpolate between the current and target positions.
            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                time / transitionDuration
            );
            time += Time.deltaTime;
            yield return null;
        }

        // Ensure the camera ends exactly at the target position.
        transform.position = targetPosition;
    }
}
