using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the spawning of items in rooms, implementing the ISpawner interface.
/// Items are spawned at random positions within a grid and with random Y-axis rotations.
/// </summary>
public class ItemSpawner : ISpawner
{
    /// <summary>
    /// Array of item prefabs to spawn.
    /// </summary>
    // private readonly GameObject[] _items;
    
    private Distributor<ItemInstance> itemsDistributor;
    
    /// <summary>
    /// The size of the room to determine spawning bounds.
    /// </summary>
    private readonly Vector2 _offset;

    /// <summary>
    /// Initializes a new instance of the ItemSpawner class.
    /// </summary>
    /// <param name="items">Array of item instance objects to spawn.</param>
    /// <param name="offset">The size of the room to determine spawning bounds.</param>
    public ItemSpawner(List<ItemInstance> items, Vector2 offset)
    {
        itemsDistributor = new Distributor<ItemInstance>(items);
        _offset = offset;
    }

    /// <summary>
    /// Spawns items in a specified room.
    /// </summary>
    /// <param name="room">The room in which items are spawned.</param>
    /// <param name="isStartRoom">Indicates whether the room is the starting room (no items should be spawned).</param>
    public void SpawnInRoom(RoomBehaviour room, bool isStartRoom)
    {
        // No items in the start room
        if (isStartRoom) return;

        int numberOfItems = Random.Range(1, 4);
        Bounds roomBounds = new Bounds(room.transform.position, new Vector3(_offset.x, 0, _offset.y));

        //@TODO: needs to be refactored after the voronoi diagram is the default way to create the dungeon
        List<Vector3> availablePositions = GenerateGridPositions(roomBounds, 3, 3);

        for (int i = 0; i < numberOfItems && availablePositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector3 position = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);
            
            // Random rotation on Y-axis
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            ItemInstance item = itemsDistributor.GetRandomElement();

            GameObject spawnedItem = GameObject.Instantiate(item.itemData._model, position, randomRotation, room.transform);
            
            CollectibleItem collectible = spawnedItem.GetComponent<CollectibleItem>();
            if (collectible != null)
            {
                //collectible.item = item;
            }
            
            // item.itemData.spawnObject.transform.position = position;
            // item.itemData.spawnObject.transform.position = position + room.transform.position;
            // item.itemData.spawnObject.transform.rotation = randomRotation;
            // item.itemData.spawnObject.transform.parent = room.transform;
            // item.itemData.spawnObject.SetActive(true);
            // Object.Instantiate(itemsDistributor.GetRandomElement().itemData.spawnObject,
            //     position,
            //     randomRotation,
            //     room.transform
            // );
        }
    }

    /// <summary>
    /// Generates a grid of positions within the room bounds for spawning items.
    /// </summary>
    /// <param name="bounds">The bounds of the room to define the grid area.</param>
    /// <param name="rows">The number of rows in the grid.</param>
    /// <param name="cols">The number of columns in the grid.</param>
    /// <returns>A list of positions within the grid.</returns>
    private List<Vector3> GenerateGridPositions(Bounds bounds, int rows, int cols)
    {
        List<Vector3> positions = new List<Vector3>();
        float cellWidth = bounds.size.x / cols;
        float cellHeight = bounds.size.z / rows;

        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Vector3 cellCenter = new Vector3(
                    bounds.min.x + cellWidth * i + cellWidth / 2,
                    0.5f,
                    bounds.min.z + cellHeight * j + cellHeight / 2
                );
                positions.Add(cellCenter);
            }
        }
        return positions;
    }
}