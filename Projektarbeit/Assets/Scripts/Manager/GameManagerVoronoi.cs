using UnityEngine;
using System.Collections;

public class GameManagerVoronoi : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private VoronoiGenerator voronoiGenerator;
    
    void Start()
    {
        StartCoroutine(WaitForDungeon());
    }

    private IEnumerator WaitForDungeon()
    {
        while (voronoiGenerator.GetDungeonGraph() == null || voronoiGenerator.GetDungeonGraph().GetStartRoom() == null)
        {
            yield return null;
        }

        SpawnPlayerAtStartRoom();
    }
    
    private void SpawnPlayerAtStartRoom()
    {
        DungeonGraph dungeon = voronoiGenerator.GetDungeonGraph();
        Room startRoom = dungeon.GetStartRoom();

        Vector3 spawnPosition = new Vector3(startRoom.center.x, 1f, startRoom.center.y);
        GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        // Initialize references
        var inputManager = FindFirstObjectByType<GameInputManager>();
        var poolManager = FindFirstObjectByType<ObjectPoolManager>();

        var controller = player.GetComponent<FirstPersonPlayerController>();
        if (controller is not null && inputManager is not null)
            controller.Init(inputManager);

        var shooter = player.GetComponent<PlayerShooting>();
        if (shooter is not null && poolManager is not null)
            shooter.Init(poolManager);
    }

}
