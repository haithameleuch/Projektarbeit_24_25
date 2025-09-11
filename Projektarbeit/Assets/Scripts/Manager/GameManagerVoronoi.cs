using System.Collections;
using System.Collections.Generic;
using ItemPlacement;
using Saving;
using Enemy;
using Spawning;
using UnityEngine;
using MiniGame;
using System.Linq;
using Controller;
using Dungeon;
using Shooting;

namespace Manager
{
    /// <summary>
    /// Runs the core game loop for the Voronoi dungeon: generates the level,
    /// spawns the player, tracks the current room, and reacts to room changes
    /// (enemies, boss, minigames, doors, items).
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

        /// <summary>
        /// Regular items that can appear in item rooms.
        /// </summary>
        [SerializeField] private List<ItemInstance> items;
        
        /// <summary>
        /// Items that must be available.
        /// </summary>
        [SerializeField] private List<ItemInstance> mustItems;
        
        /// <summary>
        /// Prefabs for Glyph Items.
        /// </summary>
        [SerializeField] private List<ItemInstance> glyphItems;

        /// <summary>
        /// Prefabs for possible minigames.
        /// </summary>
        [SerializeField] private List<GameObject> miniGamePrefabs;
        
        /// <summary>
        /// Prefabs for regular enemies.
        /// </summary>
        [SerializeField] private List<GameObject> enemyPrefabs;
        
        /// <summary>
        /// Prefabs for boss enemies.
        /// </summary>
        [SerializeField] private List<GameObject> bossEnemyPrefabs;
        
        /// <summary>
        /// Prefabs for room obstacles used in boss arenas.
        /// </summary>
        [SerializeField] private List<GameObject> obstaclePrefabs;
        
        /// <summary>
        /// Prefab for the level exit after defeating the boss.
        /// </summary>
        [SerializeField] private GameObject levelExitPrefab;
        
        /// <summary>
        /// Exposes the generated dungeon graph.
        /// </summary>
        public DungeonGraph Graph => _dungeon;
        
        /// <summary>
        /// Exposes the room the player is currently in.
        /// </summary>
        public Room CurrentRoom => _currentRoom;
        
        /// <summary>
        /// The instantiated player.
        /// </summary>
        private GameObject _player;
        
        /// <summary>
        /// Active dungeon graph for this run.
        /// </summary>
        private DungeonGraph _dungeon;
        
        /// <summary>
        /// The Room the player is currently in.
        /// </summary>
        private Room _currentRoom;
        
        /// <summary>
        /// Active content spawners (items, minigames, etc.).
        /// </summary>
        private List<ISpawnerVoronoi> _spawners;
        
        /// <summary>
        /// Spawner that manages regular enemies.
        /// </summary>
        private EnemySpawnerVoronoi _enemySpawner;
        
        /// <summary>
        /// Spawner that manages the boss room.
        /// </summary>
        private BossSpawnerVoronoi _bossSpawner;
        
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
        }
        
        /// <summary>
        /// Sets up the run: generate the dungeon, restore visited state,
        /// spawn the player, create spawners, and restore boss door state.
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
        
        /// <summary>
        /// Builds the dungeon based on the saved seed.
        /// </summary>
        private void GenerateDungeon()
        {
            var seed = SaveSystemManager.GetSeed();
            voronoiGenerator.GenerateDungeon(seed);
            _dungeon = voronoiGenerator.GetDungeonGraph();
        }
        
        /// <summary>
        /// Restores visited flags and current room from the save.
        /// Initializes visited data if it does not match the new dungeon size.
        /// </summary>
        private void RestoreVisitedAndCurrentRoom()
        {
            var savedVisited = SaveSystemManager.GetVisitedRooms();
            
            if (savedVisited == null || savedVisited.Count != _dungeon.Rooms.Count)
            {
                SaveSystemManager.InitializeVisitedRooms(_dungeon.Rooms.Count);
                var startID = _dungeon.GetStartRoom().ID;
                SaveSystemManager.SetRoomVisited(startID, true);
                SaveSystemManager.SetCurrentRoomID(startID);
            }
            
            savedVisited = SaveSystemManager.GetVisitedRooms();
            for (var i = 0; i < _dungeon.Rooms.Count; i++)
                _dungeon.Rooms[i].Visited = savedVisited[i];
            
            var savedID = SaveSystemManager.GetCurrentRoomID();
            _currentRoom = _dungeon.GetRoomByID(savedID);
        }

        /// <summary>
        /// Creates and runs spawners for items, minigames, enemies, and boss content.
        /// Also sets up glyph logic for this run.
        /// </summary>
        private void InitializeSpawners()
        {
            var rooms = _dungeon.GetAllItemRooms();
            var miniGameRooms = _dungeon.GetAllMiniGameRooms();
            var enemies = _dungeon.GetAllEnemyRooms();
            var bossRoom = _dungeon.GetBossRoom();
            
            // Place the item room guaranteed by the generator (boss-free) at the very front
            var forcedItemRoomId = (voronoiGenerator != null) ? voronoiGenerator.ForcedItemRoomId : -1;
            if (forcedItemRoomId >= 0)
                rooms = rooms.OrderBy(r => r.ID == forcedItemRoomId ? 0 : 1).ToList();
            
            // Set random glyphs keys and values and add them to MustItems
            var seed = SaveSystemManager.GetSeed();
            var glyphKeys = GenerateRandomGlyphs(seed);
            var glyphNames = GetGlyphItem(glyphKeys);
            glyphItems.RemoveAll(item => !glyphNames.Contains(item.itemData._name));
            mustItems.AddRange(glyphItems);
            
            /*  JUST DEBUGGING (REMOVE LATER)
            foreach (var mustItem in mustItems)
            {
                Debug.Log("MUST ITEMS: " + mustItem.itemData._name);
            }
            
            Debug.Log("----------------------");
            
            foreach (var glyph in glyphNames)
            {
                Debug.Log("GLYPH ITEMS: " + glyph);
            }
                JUST DEBUGGING (REMOVE LATER)*/

            _spawners = new List<ISpawnerVoronoi>()
            {
                new ItemSpawnerVoronoi(items, rooms, transform, mustItems),
                new MiniGameSpawnerVoronoi(miniGameRooms, miniGamePrefabs, transform)
            };
            PopulateDungeon();
            _enemySpawner = new EnemySpawnerVoronoi(enemies, enemyPrefabs, transform);
            _bossSpawner = new BossSpawnerVoronoi(bossRoom, bossEnemyPrefabs, obstaclePrefabs, transform, levelExitPrefab);
            
            // Pass the list of glyphs to the canvas draw script
            CanvasDraw.SetRefGlyph = glyphKeys;
        }

        /// <summary>
        /// Maps glyph key IDs to their item names and returns the list for this run.
        /// </summary>
        /// <param name="glyphKeys">Glyph IDs selected for the run.</param>
        /// <returns>List of glyph item names.</returns>
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

        /// <summary>
        /// Generates a random set of glyph IDs for the run.
        /// </summary>
        /// <param name="seed">Seed from the save; if negative, a random seed is used.</param>
        /// <returns>List of unique glyph IDs.</returns>
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
        
        /// <summary>
        /// Spawns the player at the start room or at the saved position,
        /// restores facing and camera, and wires up input and shooting.
        /// </summary>
        private void SpawnPlayer()
        {
            var spawnPos    = Vector3.zero;
            var rotation    = Vector3.zero;
            var camRotation = Vector3.zero;

            if (SaveSystemManager.GetPlayerPosition() == Vector3.zero)
            {
                var startRoom = _dungeon.GetStartRoom();
                spawnPos      = new Vector3(startRoom.Center.X, 1f, startRoom.Center.Y);
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
        
        /// <summary>
        /// Opens boss doors on load if the save says they are already open.
        /// </summary>
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
        /// Checks player distance to current and neighbor rooms to detect a room switch.
        /// Calls <see cref="OnRoomEntered"/> when a new room is entered.
        /// </summary>
        private void TrackCurrentRoom()
        {
            var playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
            var minDist = Vector2.Distance(playerPos, new Vector2(_currentRoom.Center.X, _currentRoom.Center.Y));

            if (minDist <= roomSwitchThreshold)
                return;

            var closest = _currentRoom;

            foreach (var neighbor in _currentRoom.Neighbors)
            {
                var dist = Vector2.Distance(playerPos, new Vector2(neighbor.Center.X, neighbor.Center.Y));
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
            Debug.Log($"[GameManager] Player entered room {newRoom.ID} (Type: {newRoom.Type})");
            
            if (newRoom.Visited) return;
            newRoom.Visited = true;
            
            SaveSystemManager.SetRoomVisited(newRoom.ID, true);
            SaveSystemManager.SetCurrentRoomID(newRoom.ID);

            switch (newRoom.Type)
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

        /// <summary>
        /// Runs all registered spawners to populate the dungeon.
        /// </summary>
        private void PopulateDungeon()
        {
            foreach (var spawner in _spawners)
            {
                spawner.SpawnInRoom();
            }
        }
        
        /// <summary>
        /// Called by the BossKey when used.
        /// Opens boss doors if the player is next to the boss room.
        /// </summary>
        /// <returns>True if the key was used in a valid spot, otherwise false.</returns>
        public bool OnBossKeyUsed()
        {
            var bossRoom = _dungeon.GetBossRoom();
    
            if (_currentRoom != null && bossRoom != null && _currentRoom.Neighbors.Contains(bossRoom))
            {
                Debug.Log("[GameManager] Boss doors are opened!");
                EventManager.Instance.TriggerOpenBossDoors();
                
                SaveSystemManager.SetBossRoomOpen(true);
                return true;
            }
    
            Debug.Log("[GameManager] Boss key used, but not in front of the boss room.");
            return false;
        }
        
        /// <summary>
        /// Opens boss doors on the next frame (used during a load).
        /// </summary>
        /// <returns>Coroutine handle.</returns>
        private IEnumerator OpenBossDoorsNextFrame()
        {
            yield return null;
            EventManager.Instance.TriggerOpenBossDoors();
        }
    }
}