using System.Collections;
using System.Collections.Generic;
using ItemPlacement;
using Spawning;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Manages the core game logic for a Voronoi-based dungeon, including player spawning,
    /// tracking the current room, and reacting to room transitions (e.g., boss triggers).
    /// </summary>
    public class GameManagerVoronoi : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the GameManagerVoronoi, ensuring there is only one instance in the scene.
        /// Provides global access to event management functionality.
        /// </summary>
        public static GameManagerVoronoi Instance { get; private set; }
        
        /// <summary>
        /// The prefab used to instantiate the player at the start of the game.
        /// </summary>
        [SerializeField] private GameObject playerPrefab;
    
        /// <summary>
        /// Reference to the VoronoiGenerator responsible for dungeon generation.
        /// </summary>
        [SerializeField] private VoronoiGenerator voronoiGenerator;
    
        /// <summary>
        /// Distance threshold used to determine when the player has left the current room.
        /// </summary>
        [SerializeField] private float roomSwitchThreshold = 5f;
        
        [SerializeField] private List<ItemInstance> items;

        [SerializeField] private List<GameObject> minigamePrefabs;
        
        [SerializeField] private List<GameObject> enemyPrefabs;


        private GameObject _player;
        private DungeonGraph _dungeon;
        private Room _currentRoom;
        
        private List<ISpawnerVoronoi> _spawners;
        private EnemySpawnerVoronoi _enemySpawner;
        
        public DungeonGraph Graph => _dungeon;
        public Room CurrentRoom => _currentRoom;
        
        /// <summary>
        /// Ensures there is only one instance of the GameManagerVoronoi in the scene.
        /// If another instance exists, it will be destroyed.
        /// Persists the instance across scenes for consistent event state management.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        
            DontDestroyOnLoad(gameObject);
        }

        public void StartNewRun()
        {
            int newSeed = Random.Range(100000, 999999);
            SaveSystemManager.StartNewRun(newSeed);
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); // optional: Szene neuladen
        }
        
        /// <summary>
        /// Initializes the game by starting the dungeon wait coroutine.
        /// </summary>
        void Start()
        {
            
            // StartCoroutine(WaitForDungeon());
            int seed = SaveSystemManager.GetSeed();
            voronoiGenerator.GenerateDungeon(seed);
            
            _dungeon = voronoiGenerator.GetDungeonGraph();
            List<Room> rooms = _dungeon.GetAllItemRooms();
            List<Room> minigameRooms = _dungeon.GetAllMiniGameRooms();
            List<Room> enemyRooms = _dungeon.GetAllEnemyRooms();
            
            _spawners = new List<ISpawnerVoronoi>()
            {
                new ItemSpawnerVoronoi(items, rooms, transform),
                new MiniGameSpawnerVoronoi(minigameRooms, minigamePrefabs, transform)
            };
            
            PopulateDungeon();
            SpawnPlayerAtStartRoom();
            _enemySpawner = new EnemySpawnerVoronoi(enemyRooms, enemyPrefabs, this.transform);

        }

        /// <summary>
        /// Waits until the dungeon generation is complete before spawning the player.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator WaitForDungeon()
        {
            while (voronoiGenerator.GetDungeonGraph() is null || voronoiGenerator.GetDungeonGraph().GetStartRoom() is null)
            {
                yield return null;
            }

            _dungeon = voronoiGenerator.GetDungeonGraph();
            List<Room> rooms = _dungeon.GetAllItemRooms();
            List<Room> minigameRooms = _dungeon.GetAllMiniGameRooms();
            List<Room> enemyRooms = _dungeon.GetAllEnemyRooms();
            
            _spawners = new List<ISpawnerVoronoi>()
            {
                new ItemSpawnerVoronoi(items, rooms, transform),
                new MiniGameSpawnerVoronoi(minigameRooms, minigamePrefabs, transform)
            };
            
            
            PopulateDungeon();
            SpawnPlayerAtStartRoom();
            _enemySpawner = new EnemySpawnerVoronoi(enemyRooms, enemyPrefabs, this.transform);
        }

        /// <summary>
        /// Spawns the player in the start room and sets up all required references.
        /// Also sets the initial current room and triggers the room entry logic.
        /// </summary>
        private void SpawnPlayerAtStartRoom()
        {
            var spawnPosition = Vector3.zero;
            var rotation = Vector3.zero;
            var cam_rotation = Vector3.zero;
            if (SaveSystemManager.GetPlayerPosition() == Vector3.zero)
            {
                var startRoom = _dungeon.GetStartRoom();
                spawnPosition = new Vector3(startRoom.center.x, 1f, startRoom.center.y);
                _currentRoom = startRoom;
                OnRoomEntered(_currentRoom);
            }
            else
            {
                spawnPosition = SaveSystemManager.GetPlayerPosition();
                rotation = SaveSystemManager.GetPlayerRotation();
                cam_rotation = SaveSystemManager.GetCamRotation();
            }
            _player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            _player.transform.forward = rotation;
            
            // Initialize references
            var inputManager = FindFirstObjectByType<GameInputManager>();
            var poolManager = FindFirstObjectByType<ObjectPoolManager>();

            var controller = _player.GetComponent<FirstPersonPlayerController>();
            if (controller is not null && inputManager is not null)
                controller.Init(inputManager);

            var shooter = _player.GetComponent<PlayerShooting>();
            if (shooter is not null && poolManager is not null)
                shooter.Init(poolManager);

            UIManager.Instance?.SetPlayer(_player);
            CameraManager.Instance?.SetPlayer(_player);
            _player.transform.Find("FirstPersonCam").transform.forward = cam_rotation;
            
            SaveSystemManager.SetPlayerPosition(_player.transform.position);
            SaveSystemManager.SetPlayerRotation(_player.transform.forward);
            SaveSystemManager.SetCamRotation(_player.transform.Find("FirstPersonCam").transform.forward);
            SaveSystemManager.Save();
        }

        /// <summary>
        /// Called once per frame to track player behavior and transitions.
        /// </summary>
        void Update()
        {
            if (_player is null || _dungeon is null || _currentRoom is null) return;

            TrackCurrentRoom();
        }
        
        /// <summary>
        /// Checks if the player has moved into a different room.
        /// If so, updates the current room and triggers room entry logic.
        /// </summary>
        private void TrackCurrentRoom()
        {
            Vector2 playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
            float minDist = Vector2.Distance(playerPos, new Vector2(_currentRoom.center.x, _currentRoom.center.y));

            if (minDist <= roomSwitchThreshold)
                return;

            Room closest = _currentRoom;

            foreach (Room neighbor in _currentRoom.neighbors)
            {
                float dist = Vector2.Distance(playerPos, new Vector2(neighbor.center.x, neighbor.center.y));
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = neighbor;
                }
            }

            if (closest != _currentRoom)
            {
                _currentRoom = closest;
                OnRoomEntered(_currentRoom);
            }
        }

        /// <summary>
        /// Called when the player enters a new room. Used to trigger room-specific game logic,
        /// such as boss activation, enemy activation, item placement, or door locking.
        /// </summary>
        /// <param name="newRoom">The room that the player just entered.</param>
        private void OnRoomEntered(Room newRoom)
        {
            Debug.Log($"[GameManager] Player entered room {newRoom.id} (Type: {newRoom.type})");
            
            if (newRoom.visited)
            {
                return;
            }

            newRoom.visited = true;

            switch (newRoom.type)
            {
                case RoomType.Start:
                    break;
                case RoomType.Normal:
                    break;
                case RoomType.Item:
                    break;
                case RoomType.MiniGame:
                    EventManager.Instance.TriggerCloseDoors();
                    break;
                case RoomType.Enemy:
                    //if (!newRoom.visited)
                    {
                        _enemySpawner.ActivateEnemyInRoom(newRoom);
                    }
                    EventManager.Instance.TriggerCloseDoors();
                    break;
                case RoomType.Boss:
                    EventManager.Instance.TriggerCloseBossDoors();
                    break;
                default:
                    Debug.Log("Unknown room type");
                    break;
            }
        }

        private void PopulateDungeon()
        {
            foreach (var spawner in _spawners)
            {
                spawner.SpawnInRoom();
            }
        }
        
        /// <summary>
        /// Called by the BossKey item when the player uses the key.
        /// Opens boss room doors when the player is standing in front of the boss room.
        /// Returns whether the key was used "validly".
        /// </summary>
        public bool OnBossKeyUsed()
        {
            Room bossRoom = _dungeon.GetBossRoom();
    
            if (_currentRoom != null && bossRoom != null && _currentRoom.neighbors.Contains(bossRoom))
            {
                Debug.Log("[GameManager] Boss doors are opened!");
                EventManager.Instance.TriggerOpenBossDoors();
                return true;
            }
    
            Debug.Log("[GameManager] Boss key used, but not in front of the boss room.");
            return false;
        }
    }
}
