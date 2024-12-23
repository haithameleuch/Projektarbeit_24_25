using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : ISpawner
{
    private readonly GameObject[] _items;
    private readonly Vector2 _offset;

    public ItemSpawner(GameObject[] items, Vector2 offset)
    {
        this._items = items;
        this._offset = offset;
    }

    public void SpawnInRoom(RoomBehaviour room, bool isStartRoom)
    {
        if (isStartRoom) return;

        int numberOfItems = Random.Range(1, 4);
        Bounds roomBounds = new Bounds(room.transform.position, new Vector3(_offset.x, 0, _offset.y));

        List<Vector3> availablePositions = GenerateGridPositions(roomBounds, 3, 3);

        for (int i = 0; i < numberOfItems && availablePositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector3 position = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);

            Object.Instantiate(_items[Random.Range(0, _items.Length)],
                position,
                Quaternion.identity,
                room.transform
            );
        }
    }

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