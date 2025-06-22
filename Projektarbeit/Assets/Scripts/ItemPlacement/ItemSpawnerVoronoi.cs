using System.Collections.Generic;
using Geometry;
using Spawning;
using UnityEngine;

/// <summary>
/// Handles the spawning of items in rooms, implementing the ISpawner interface.
/// Items are spawned at random positions within a room with the help of barycentric coordinates and with random Y-axis rotations.
/// </summary>
public class ItemSpawnerVoronoi : ISpawnerVoronoi
{
    /// <summary>
    /// Distributor that takes all items and just gives one item at a time
    /// </summary>
    private readonly Distributor<ItemInstance> _itemsDistributor;

    /// <summary>
    /// list of item rooms to distribute items
    /// </summary>
    private readonly List<Room> _rooms;
    
    /// <summary>
    /// Parent transform to group all spawned items in the hierarchy.
    /// </summary>
    private readonly Transform _parent;

    /// <summary>
    /// Creates a new ItemSpawner for given items and rooms.
    /// </summary>
    /// <param name="items">List of item instances to spawn.</param>
    /// <param name="rooms">List of rooms where items should be placed.</param>
    /// <param name="parent">Parent transform for hierarchy organization.</param>
    public ItemSpawnerVoronoi(List<ItemInstance> items, List<Room> rooms, Transform parent)
    {
        _itemsDistributor = new Distributor<ItemInstance>(items);
        _rooms = rooms;
        _parent = parent;
    }
    
    /// <summary>
    /// Spawns 1–3 items in each room, arranged in a circle around the room center.
    /// Items are instantiated slightly inside the incircle to avoid room borders.
    /// </summary>
    public void SpawnInRoom()
    {
        foreach (var room in _rooms)
        {
            var itemCount = Random.Range(1, 4); // 1–3 items per room
            var radius = room.getIncircleRadius();

            for (var i = 0; i < itemCount; i++)
            {
                var itemInstance = _itemsDistributor.GetRandomElement();
            
                // Place items in a circular pattern
                var angle = i * (360f / itemCount);
                var distanceFromCenter = Mathf.Min(radius * 0.6f, 3f); // keep items within safe bounds
            
                var xOffset = Mathf.Cos(angle * Mathf.Deg2Rad) * distanceFromCenter;
                var zOffset = Mathf.Sin(angle * Mathf.Deg2Rad) * distanceFromCenter;
            
                var spawnPos = new Vector3(room.center.x + xOffset, 0.5f, room.center.y + zOffset);
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            
                var spawnedItem = Object.Instantiate(itemInstance.itemData.spawnObject, spawnPos, rotation, _parent);
                spawnedItem.SetActive(true);
            
                var collectible = spawnedItem.GetComponent<CollectibleItem>();
                if (collectible is not null)
                {
                    //collectible.item = itemInstance;
                }
            }
        }
    }
    
    
    
    // --- PETER VERSION --- //
    /*public void SpawnInRoom()
    {
        foreach (Room room in _rooms)
        {
            int numberOfItems = Random.Range(1, 3);
            float firstPositionAngle = Random.Range(0f, 360f);
            ItemInstance[] items = new ItemInstance[numberOfItems];
            
            for (int i = 0; i < numberOfItems; i++)
            {
                items[i] = _itemsDistributor.GetRandomElement();
            }
            float firstItemRadius = calcItemDiameter(items[0]) / 2;
            float roomRadius = room.getIncircleRadius();
            
            if (numberOfItems == 1)
            {
                float itemScaling = roomRadius / firstItemRadius;
                if (itemScaling < 1.0)
                {
                    items[0].itemData.spawnObject.transform.localScale = new Vector3(itemScaling,itemScaling,itemScaling);
                    firstItemRadius *= itemScaling;
                }
                
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                Vector3 newPosition = calcNewPosition(room.center, firstItemRadius, roomRadius, firstPositionAngle);
                
                GameObject spawnedItem = GameObject.Instantiate(items[0].itemData.spawnObject, newPosition, randomRotation, _parent);
                spawnedItem.SetActive(true);
                
                CollectibleItem collectible = spawnedItem.GetComponent<CollectibleItem>();
                if (collectible != null)
                {
                    collectible.item = items[0];
                }
            }
            else
            {
                float secondItemRadius = calcItemDiameter(items[1]) / 2;
                float firstItemScale = roomRadius / firstItemRadius;
                float secondItemScale = roomRadius / secondItemRadius;
            
                if (firstItemScale < 1.0)
                {
                    items[0].itemData.spawnObject.transform.localScale = new Vector3(firstItemScale,firstItemScale,firstItemScale);
                    firstItemRadius *= firstItemScale;
                }
            
                if (secondItemScale < 1.0)
                {
                    items[1].itemData.spawnObject.transform.localScale = new Vector3(secondItemScale,secondItemScale,secondItemScale);
                    secondItemRadius *= secondItemScale;
                }
            
                float blockedAngleByFirstItem = blockedAngleRange(firstItemRadius, roomRadius);
                float maxAngleOfSecondItem = 360.0f - (blockedAngleByFirstItem + blockedAngleRange(secondItemRadius, roomRadius));
                float offsetSecondPositionAngle = firstPositionAngle + blockedAngleByFirstItem / 2;
                float secondPositionAngle = offsetSecondPositionAngle + Random.Range(0f, maxAngleOfSecondItem);
                
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                Vector3 firstItemPosition = calcNewPosition(room.center, firstItemRadius, roomRadius, firstPositionAngle);
                Vector3 secondItemPosition = calcNewPosition(room.center, secondItemRadius, roomRadius, secondPositionAngle);
                
                GameObject spawnedFirstItem = GameObject.Instantiate(items[0].itemData.spawnObject, firstItemPosition, randomRotation, _parent);
                GameObject spawnedSecondItem = GameObject.Instantiate(items[1].itemData.spawnObject, secondItemPosition, randomRotation, _parent);
                spawnedFirstItem.SetActive(true);
                spawnedSecondItem.SetActive(true);
                
                CollectibleItem firstCollectible = spawnedFirstItem.GetComponent<CollectibleItem>();
                CollectibleItem secondCollectible = spawnedSecondItem.GetComponent<CollectibleItem>();
                
                if (firstCollectible != null)
                {
                    firstCollectible.item = items[0];
                }
                
                if (secondCollectible != null)
                {
                    secondCollectible.item = items[0];
                }
            }
        }
    }

    private float calcItemDiameter(ItemInstance item)
    {
        Vector3 safeSpawnPosition = Vector3.zero;
        Quaternion safeSpawnRotation = Quaternion.identity;
        GameObject tempItem = GameObject.Instantiate(item.itemData.spawnObject, safeSpawnPosition, safeSpawnRotation, _parent);
        tempItem.SetActive(false);
        Renderer tempRenderer = tempItem.GetComponent<Renderer>();
        Bounds itemBounds = tempRenderer.bounds;
        Vector2 itemPositionSizes = new Vector2(itemBounds.size.x, itemBounds.size.z);
        Object.Destroy(tempItem);
        return Mathf.Sqrt(itemPositionSizes.magnitude);
    }

    private Vector3 calcNewPosition(Point center, float itemRadius, float roomRadius, float angle)
    {
        float distItemCenterToRoom = roomRadius - itemRadius;
        float xCoord = center.x + distItemCenterToRoom * Mathf.Cos(angle * Mathf.Deg2Rad);
        float zCoord = center.y + distItemCenterToRoom * Mathf.Sin(angle * Mathf.Deg2Rad);
        return new Vector3(xCoord, 0.5f, zCoord);
    }

    private float blockedAngleRange(float itemRadius, float roomRadius)
    {
        if (Mathf.Abs(roomRadius - itemRadius) < Mathf.Epsilon)
        {
            return 180f;
        }
        return (Mathf.Atan(itemRadius / roomRadius) * Mathf.Rad2Deg)*2;
    }*/
}