using System.Collections;
using System.Collections.Generic;
using ItemPlacement;
using Saving;
using Enemy;
using Spawning;
using UnityEngine;
using MiniGame;
using System.Linq;

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
        
        [SerializeField] private List<ItemInstance> mustItems;
        
        [SerializeField] private List<ItemInstance> glyphItems;

        [SerializeField] private List<GameObject> miniGamePrefabs;
        
        [SerializeField] private List<GameObject> enemyPrefabs;
        
        [SerializeField] private List<GameObject> bossEnemyPrefabs;
        
        [SerializeField] private List<GameObject> obstaclePrefabs;
        
        [SerializeField] private GameObject levelExitPrefab;


        private GameObject _player;
        private DungeonGraph _dungeon;
        private Room _currentRoom;
        
        private List<ISpawnerVoronoi> _spawners;
        private EnemySpawnerVoronoi _enemySpawner;
        private BossSpawnerVoronoi _bossSpawner;

        
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
        
            //DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// Initializes the game by starting the dungeon wait coroutine.
        /// </summary>
        private void Start()
        {
            EnemyDeathReporter.SetSceneChanging(false);
            
            GenerateDungeon();
            RestoreVisitedAndCurrentRoom();
            SpawnPlayer();
            InitializeSpawners();
            RestoreBossDoorState();
        }
        
        private void GenerateDungeon()
        {
            var seed = SaveSystemManager.GetSeed();
            voronoiGenerator.GenerateDungeon(seed);
            _dungeon = voronoiGenerator.GetDungeonGraph();
        }
        
        private void RestoreVisitedAndCurrentRoom()
        {
            var savedVisited = SaveSystemManager.GetVisitedRooms();
            
            if (savedVisited == null || savedVisited.Count != _dungeon.rooms.Count)
            {
                SaveSystemManager.InitializeVisitedRooms(_dungeon.rooms.Count);
                var startID = _dungeon.GetStartRoom().id;
                SaveSystemManager.SetRoomVisited(startID, true);
                SaveSystemManager.SetCurrentRoomID(startID);
            }
            
            savedVisited = SaveSystemManager.GetVisitedRooms();
            for (int i = 0; i < _dungeon.rooms.Count; i++)
                _dungeon.rooms[i].visited = savedVisited[i];
            
            int savedID = SaveSystemManager.GetCurrentRoomID();
            _currentRoom = _dungeon.GetRoomByID(savedID);
        }

        private void InitializeSpawners()
        {
            var rooms = _dungeon.GetAllItemRooms();
            var miniGameRooms = _dungeon.GetAllMiniGameRooms();
            var enemies = _dungeon.GetAllEnemyRooms();
            var bossRoom = _dungeon.GetBossRoom();
            
            // Set random glyphs keys and values and add them to MustItems
            var seed = SaveSystemManager.GetSeed();
            var glyphKeys = GenerateRandomGlyphs(seed);
            var glyphNames = GetGlyphItem(glyphKeys);
            glyphItems.RemoveAll(item => !glyphNames.Contains(item.itemData._name));
            mustItems.AddRange(glyphItems);

            // -----------------------------------
            // TODO: JUST DEBUGGING (REMOVE LATER)
            foreach (var mustItem in mustItems)
            {
                Debug.Log("MUST ITEMS: " + mustItem.itemData._name);
            }
            
            Debug.Log("----------------------");
            
            foreach (var glyph in glyphNames)
            {
                Debug.Log("GLYPH ITEMS: " + glyph);
            }
            // TODO: JUST DEBUGGING (REMOVE LATER)
            
            // -----------------------------------
           
            var filteredMust = new List<ItemInstance>(mustItems);
            var invComp = _player != null ? _player.GetComponent<Inventory>() : null;
            if (invComp != null && HasTool(invComp, ToolType.Pickaxe))
            {
                filteredMust.RemoveAll(IsPickaxeItem);
            }

            _spawners = new List<ISpawnerVoronoi>()
            {
                new ItemSpawnerVoronoi(items, rooms, transform, filteredMust),
                new MiniGameSpawnerVoronoi(miniGameRooms, miniGamePrefabs, transform)
            };
            PopulateDungeon();
            _enemySpawner = new EnemySpawnerVoronoi(enemies, enemyPrefabs, transform);
            _bossSpawner = new BossSpawnerVoronoi(bossRoom, bossEnemyPrefabs, obstaclePrefabs, transform, levelExitPrefab);
            
            // Pass the list of glyphs to the canvas draw script
            CanvasDraw.SetRefGlyph = glyphKeys;
        }

        private static List<string> GetGlyphItem(List<int> glyphKeys)
        {
            // Map of keys to glyph names
            var keyMapGlyph = new Dictionary<int, string>
            {
                { 0, "Air" },
                { 1, "Earth" },
                { 2, "Energy" },
                { 3, "Fire" },
                { 4, "Power" },
                { 5, "Power" },
                { 6, "Time" },
                { 7, "Water" },
            };

            // Get the glyph names for the given keys
            var glyphNames = glyphKeys
                .Where(key => keyMapGlyph.ContainsKey(key)) // ensures only valid keys
                .Select(key => keyMapGlyph[key])
                .ToList();
            
            return glyphNames;
        }

        private static List<int> GenerateRandomGlyphs(int seed = -1)
        {
            var setRefGlyphKeys = new List<int>();
            // Create Random with optional seed
            var rand = seed >= 0 ? new System.Random(seed) : new System.Random();
            // Decide how many additional numbers to add (2 to 4)
            var x = rand.Next(2, 5);
            
            // candidates: 0-7 excluding 5. since it repeated two times in glyphs
            var candidates = Enumerable.Range(0, 8).Where(n => n != 5 && n != 4).ToList();
            // Shuffle candidates
            candidates = candidates.OrderBy(n => rand.Next()).ToList();
            // Take x numbers and add
            setRefGlyphKeys.AddRange(candidates.Take(x));
            
            return setRefGlyphKeys;
        }
        
        private void SpawnPlayer()
        {
            var spawnPos    = Vector3.zero;
            var rotation    = Vector3.zero;
            var camRotation = Vector3.zero;

            if (SaveSystemManager.GetPlayerPosition() == Vector3.zero)
            {
                var startRoom = _dungeon.GetStartRoom();
                spawnPos      = new Vector3(startRoom.center.x, 1f, startRoom.center.y);
                _currentRoom  = startRoom;
                OnRoomEntered(_currentRoom);
            }
            else
            {
                spawnPos    = SaveSystemManager.GetPlayerPosition();
                rotation    = SaveSystemManager.GetPlayerRotation();
                camRotation = SaveSystemManager.GetCamRotation();
            }

            _player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            _player.transform.forward = rotation;
            
            var cameraTransform = _player.transform.Find("FirstPersonCam");
            if (cameraTransform != null)
                cameraTransform.forward = camRotation;

            var inputManager = FindFirstObjectByType<GameInputManager>();
            var poolManager  = FindFirstObjectByType<ObjectPoolManager>();

            var controller = _player.GetComponent<FirstPersonPlayerController>();
            if (controller is not null && inputManager is not null)
            {
                controller.Init(inputManager);
                controller.SyncLoadedRotation();
            }

            var shooter = _player.GetComponent<PlayerShooting>();
            if (shooter is not null && poolManager is not null) shooter.Init(poolManager);

            UIManager.Instance?.SetPlayer(_player);
            CameraManager.Instance?.SetPlayer(_player);
            
            SaveSystemManager.SetPlayerPosition(_player.transform.position);
            SaveSystemManager.SetPlayerRotation(_player.transform.forward);
            SaveSystemManager.SetCamRotation(cameraTransform != null ? cameraTransform.forward : Vector3.zero);
            
            var stats = SaveSystemManager.GetStats();

            if (stats.Item1.Count > 0)
            {
                _player.GetComponent<Stats>().SetStats(stats.Item1, stats.Item2);
            }
        }
        
        private void RestoreBossDoorState()
        {
            if (SaveSystemManager.GetBossRoomOpen())
                StartCoroutine(OpenBossDoorsNextFrame());
        }

        /// <summary>
        /// Called once per frame to track player behavior and transitions.
        /// </summary>
        private void Update()
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
            var playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
            var minDist = Vector2.Distance(playerPos, new Vector2(_currentRoom.center.x, _currentRoom.center.y));

            if (minDist <= roomSwitchThreshold)
                return;

            var closest = _currentRoom;

            foreach (Room neighbor in _currentRoom.neighbors)
            {
                var dist = Vector2.Distance(playerPos, new Vector2(neighbor.center.x, neighbor.center.y));
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
            
            if (newRoom.visited) return;
            newRoom.visited = true;
            
            SaveSystemManager.SetRoomVisited(newRoom.id, true);
            SaveSystemManager.SetCurrentRoomID(newRoom.id);

            switch (newRoom.type)
            {
                case RoomType.Start:
                    break;
                case RoomType.Normal:
                    break;
                case RoomType.Item:
                    break;
                case RoomType.MiniGame:
                    //EventManager.Instance.TriggerCloseDoors();
                    break;
                case RoomType.Enemy:
                    _enemySpawner.ActivateEnemyInRoom(newRoom);
                    EventManager.Instance.TriggerCloseDoors();
                    break;
                case RoomType.Boss:
                    _bossSpawner?.ActivateBossInRoom(newRoom);
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
            var bossRoom = _dungeon.GetBossRoom();
    
            if (_currentRoom != null && bossRoom != null && _currentRoom.neighbors.Contains(bossRoom))
            {
                Debug.Log("[GameManager] Boss doors are opened!");
                EventManager.Instance.TriggerOpenBossDoors();
                
                SaveSystemManager.SetBossRoomOpen(true);
                return true;
            }
    
            Debug.Log("[GameManager] Boss key used, but not in front of the boss room.");
            return false;
        }
        
        private IEnumerator OpenBossDoorsNextFrame()
        {
            yield return null;
            EventManager.Instance.TriggerOpenBossDoors();
        }
        
        private static bool IsPickaxeItem(ItemInstance instance)
        {
            return instance is { itemData: Equipment { toolType: ToolType.Pickaxe } };
        }

        private static bool HasTool(Inventory inventory, ToolType type)
        {
            if (inventory == null) return false;

            // search inventory
            var grid = inventory.getInventory();
            if (grid != null)
            {
                foreach (var slot in grid)
                {
                    if (slot is { itemData: Equipment e } && e.toolType == type)
                        return true;
                }
            }

            // search equipment
            var equip = inventory.getEquipment();
            if (equip != null)
            {
                foreach (var slot in equip)
                {
                    if (slot is { itemData: Equipment e } && e.toolType == type)
                        return true;
                }
            }

            return false;
        }
    }
}