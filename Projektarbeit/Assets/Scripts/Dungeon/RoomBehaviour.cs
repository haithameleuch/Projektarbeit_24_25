using UnityEngine;

/// <summary>
/// Controls the behavior of room walls and doors, updating their visibility based on the room's status.
/// This ensures that doors are visible and walls are hidden when a path to another room exists, and vice versa.
/// </summary>
public class RoomBehaviour : MonoBehaviour
{
    /// <summary>
    /// Array of wall GameObjects, corresponding to the room's boundaries:
    /// Index 0 = Up, Index 1 = Down, Index 2 = Right, Index 3 = Left.
    /// </summary>
    [SerializeField] private GameObject[] walls;

    /// <summary>
    /// Array of door GameObjects, aligned with the wall array to represent door positions.
    /// </summary>
    [SerializeField] private GameObject[] doors;

    /// <summary>
    /// Updates the room's state by activating doors and deactivating walls based on the given status array.
    /// </summary>
    /// <param name="status">A boolean array representing the state of each wall/door. 
    /// True = Open door (deactivates the wall), False = Closed wall (no door).</param>
    public void UpdateRoom(bool[] status)
    {
        for (int i = 0; i < status.Length; i++)
        {
            // Enable or disable doors based on the status
            doors[i].SetActive(status[i]);

            // Enable or disable walls inversely to the status
            walls[i].SetActive(!status[i]);
        }
    }
}