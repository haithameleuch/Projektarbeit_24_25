using System.Collections.Generic;
using System.IO;
using Inventory;
using Saving;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Manages saving and loading of game data and exposes helpers to read and update the current save.
    /// </summary>
    public static class SaveSystemManager
    {
        /// <summary>
        /// File name of the save.
        /// </summary>
        private const string SaveFileName = "save.json";

        /// <summary>
        /// Full path to the save file in <see cref="Application.persistentDataPath"/>.
        /// </summary>
        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>
        /// The active save object used by the game.
        /// </summary>
        public static SaveData SaveData { get; private set; }

        /// <summary>
        /// Serializes <see cref="SaveData"/> to JSON and writes it to disk.
        /// </summary>
        public static void Save()
        {
            var json = JsonUtility.ToJson(SaveData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("Save written to: " + SavePath);
        }
        
        /// <summary>
        /// Loads save data from disk if it exists; otherwise keeps <see cref="SaveData"/> null and logs a message.
        /// </summary>
        public static void Load()
        {
            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath);
                SaveData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("Save loaded from: " + SavePath);
            }
            else
            {
                Debug.Log("No save found. Creating default save.");
            }
        }

        /// <summary>
        /// Creates a fresh save for a new run and writes it immediately.
        /// </summary>
        /// <param name="newSeed">Seed to use for generation.</param>
        public static void StartNewRun(int newSeed)
        {
            SaveData = new SaveData
            {
                Seed = newSeed,
                Level = 1,
                PlayerPosition = Vector3.zero,
                BossCleared = false,
                Items = new List<bool>(),
                DigitMiniGameCleared = false,
                GlyphMiniGameCleared = false
            };
            Save();
        }
        
        /// <summary>
        /// Advances to the next level, updates the seed, and resets per-level fields.
        /// </summary>
        /// <param name="newSeed">Seed for the next level.</param>
        public static void AdvanceLevel(int newSeed)
        {
            if (SaveData == null) SaveData = new SaveData();

            // Level + 1 and new seed
            SaveData.Level = Mathf.Max(1, SaveData.Level) + 1;
            SaveData.Seed  = newSeed;
            
            SaveData.CurrentRoomID = 0;
            SaveData.BossRoomOpen  = false;
            SaveData.BossCleared = false;
            SaveData.VisitedRooms  = new List<bool>();
            SaveData.DestroyableWallsActive = new List<bool>();
            SaveData.DestroyableWallsHealth = new List<int>();

            SaveData.Items = new List<bool>();

            SaveData.DigitMiniGameCleared = false;
            SaveData.GlyphMiniGameCleared = false;
            
            SaveData.PlayerPosition = Vector3.zero;
            SaveData.PlayerForward  = Vector3.zero;
            SaveData.CameraForward  = Vector3.zero;
        }
        
        // ===== GETTER AND SETTER METHODS =====
        
        // ===== GLOBAL =====
        /// <summary>
        /// Gets the current seed.
        /// </summary>
        public static int GetSeed() => SaveData.Seed;

        /// <summary>
        /// Gets the current level.
        /// </summary>
        public static int GetLevel() => SaveData.Level;
        
        // ===== CURRENT ROOM ID =====

        /// <summary>
        /// Gets the current room id.
        /// </summary>
        public static int GetCurrentRoomID() => SaveData.CurrentRoomID;

        /// <summary>
        /// Sets the current room id.
        /// </summary>
        public static void SetCurrentRoomID(int id) => SaveData.CurrentRoomID = id;
        
        // ===== BOSS ROOM DOOR OPEN =====

        /// <summary>
        /// Gets whether the boss room door is open.
        /// </summary>
        public static bool GetBossRoomOpen() => SaveData.BossRoomOpen;

        /// <summary>
        /// Sets whether the boss room door is open.
        /// </summary>
        public static void SetBossRoomOpen(bool open) => SaveData.BossRoomOpen = open;
        
        // ===== BOSS CLEARED =====

        /// <summary>
        /// Gets whether the boss fight is cleared.
        /// </summary>
        public static bool GetBossCleared() => SaveData.BossCleared;

        /// <summary>
        /// Sets whether the boss fight is cleared.
        /// </summary>
        public static void SetBossCleared(bool cleared) => SaveData.BossCleared = cleared;
        
        // ===== VISITED ROOMS =====

        /// <summary>
        /// Gets the visited flags list for all rooms.
        /// </summary>
        public static List<bool> GetVisitedRooms() => SaveData.VisitedRooms;

        /// <summary>
        /// Initializes the visited list with the given room count (all false).
        /// </summary>
        /// <param name="roomCount">Number of rooms.</param>
        public static void InitializeVisitedRooms(int roomCount) => SaveData.VisitedRooms = new List<bool>(new bool[roomCount]);

        /// <summary>
        /// Sets the visited state for a room id (safe bounds check).
        /// </summary>
        public static void SetRoomVisited(int roomId, bool visited)
        {
            if (SaveData.VisitedRooms != null
                && roomId >= 0
                && roomId < SaveData.VisitedRooms.Count)
            {
                SaveData.VisitedRooms[roomId] = visited;
            }
        }
        
        // ===== DESTROYABLE WALLS ACTIVE =====

        /// <summary>
        /// Gets the active flags list for destroyable walls (creates if null).
        /// </summary>
        public static List<bool> GetDestroyableWallsActive()
        {
            if (SaveData.DestroyableWallsActive == null)
                SaveData.DestroyableWallsActive = new List<bool>();
            return SaveData.DestroyableWallsActive;
        }

        /// <summary>
        /// Sets the active flag for a destroyable wall by index (safe bounds check).
        /// </summary>
        public static void SetDestroyableWallActive(int idx, bool active)
        {
            var list = GetDestroyableWallsActive();
            if (idx >= 0 && idx < list.Count)
                list[idx] = active;
        }
        
        // ===== DESTROYABLE WALLS HEALTH =====

        /// <summary>
        /// Gets the health list for destroyable walls (creates if null).
        /// </summary>
        public static List<int> GetDestroyableWallsHealth()
        {
            if (SaveData.DestroyableWallsHealth == null)
                SaveData.DestroyableWallsHealth = new List<int>();
            return SaveData.DestroyableWallsHealth;
        }

        /// <summary>
        /// Sets the health value for a destroyable wall by index (safe bounds check).
        /// </summary>
        public static void SetDestroyableWallHealth(int idx, int hp)
        {
            var list = GetDestroyableWallsHealth();
            if (idx >= 0 && idx < list.Count)
                list[idx] = hp;
        }

        /// <summary>
        /// Gets the health for a destroyable wall by index, or 0 if out of range.
        /// </summary>
        public static int GetDestroyableWallHealth(int idx)
        {
            var list = GetDestroyableWallsHealth();
            if (idx >= 0 && idx < list.Count)
                return list[idx];
            return 0;
        }
        
        // ===== PLAYER =====

        /// <summary>
        /// Gets the saved player position.
        /// </summary>
        public static Vector3 GetPlayerPosition() => SaveData.PlayerPosition;

        /// <summary>
        /// Sets the saved player position.
        /// </summary>
        public static void SetPlayerPosition(Vector3 pos) => SaveData.PlayerPosition = pos;

        /// <summary>
        /// Gets the saved player forward direction.
        /// </summary>
        public static Vector3 GetPlayerRotation() => SaveData.PlayerForward;

        /// <summary>
        /// Sets the saved player forward direction.
        /// </summary>
        public static void SetPlayerRotation(Vector3 direct) => SaveData.PlayerForward = direct;

        /// <summary>
        /// Gets the saved camera forward direction.
        /// </summary>
        public static Vector3 GetCamRotation() => SaveData.CameraForward;

        /// <summary>
        /// Sets the saved camera forward direction.
        /// </summary>
        public static void SetCamRotation(Vector3 direct) => SaveData.CameraForward = direct;
    
        // ===== STATS =====

        /// <summary>
        /// Gets current and max stats lists.
        /// </summary>
        /// <returns>Tuple of (currentStats, maxStats).</returns>
        public static (List<float>, List<float>) GetStats() => (SaveData.CurrentStats, SaveData.MaxStats);

        /// <summary>
        /// Sets current and max stats lists.
        /// </summary>
        public static void SetStats(List<float> currStats, List<float> maxStats)
        {
            SaveData.CurrentStats = currStats;
            SaveData.MaxStats = maxStats;
        }
        
        // ===== ITEMS =====

        /// <summary>
        /// Gets whether a collectible at index is active. Grows the list if needed.
        /// </summary>
        public static bool IsCollectibleActive(int idx)
        {
            if (SaveData.Items == null)
                SaveData.Items = new List<bool>();
    
            while (SaveData.Items.Count <= idx)
                SaveData.Items.Add(true);
            
            return SaveData.Items[idx];
        }

        /// <summary>
        /// Sets whether a collectible at index is active. Grows the list if needed.
        /// </summary>
        public static void SetCollectibleActive(int idx, bool active)
        {
            if (SaveData.Items == null)
                SaveData.Items = new List<bool>();
    
            while (SaveData.Items.Count <= idx)
                SaveData.Items.Add(true);

            SaveData.Items[idx] = active;
        }

        // ===== INVENTORY =====

        /// <summary>
        /// Returns a 4x5 grid view of the inventory from the 1D array, filtering out empty entries.
        /// </summary>
        public static ItemInstance[,] GetInventory()
        {
            var grid = new ItemInstance[4, 5];
            for (var row = 0; row < 4; row++)
                for (var col = 0; col < 5; col++)
                {
                    var inst = SaveData.Inventory[row * 5 + col];
                    grid[row, col] = (inst != null && inst.itemData != null) ? inst : null;
                }
            return grid;
        }

        /// <summary>
        /// Writes a 4x5 inventory grid back to the 1D save array.
        /// </summary>
        public static void SetInventory(ItemInstance[,] grid)
        {
            for (var row = 0; row < 4; row++)
                for (var col = 0; col < 5; col++)
                    SaveData.Inventory[row * 5 + col] = grid[row, col];
        }

        /// <summary>
        /// Returns a 3x2 grid view of the equipment from the 1D array, filtering out empty entries.
        /// </summary>
        public static ItemInstance[,] GetEquipment()
        {
            var grid = new ItemInstance[3, 2];
            for (var row = 0; row < 3; row++)
                for (var col = 0; col < 2; col++)
                {
                    var inst = SaveData.Equipment[row * 2 + col];
                    grid[row, col] = (inst != null && inst.itemData != null) ? inst : null;
                }
            return grid;
        }

        /// <summary>
        /// Writes a 3x2 equipment grid back to the 1D save array.
        /// </summary>
        public static void SetEquipment(ItemInstance[,] grid)
        {
            for (var row = 0; row < 3; row++)
                for (var col = 0; col < 2; col++)
                    SaveData.Equipment[row * 2 + col] = grid[row, col];
        }
    }
}
