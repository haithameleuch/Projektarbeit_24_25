using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Saving
{
    public static class SaveSystemManager
    {
        private const string SaveFileName = "save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static SaveData SaveData { get; private set; }

        public static void Save()
        {
            var json = JsonUtility.ToJson(SaveData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("Save written to: " + SavePath);
        }
        
        public static void Load()
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                SaveData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("Save loaded from: " + SavePath);
            }
            else
            {
                Debug.Log("No save found. Creating default save.");
            }
        }

        public static void StartNewRun(int newSeed)
        {
            SaveData = new SaveData
            {
                Seed = newSeed,
                Level = 1,
                PlayerPosition = Vector3.zero,
                BossCleared = false,
                Items = new List<bool>() 
            };
            Save();
        }
        
        public static void AdvanceLevel(int newSeed)
        {
            if (SaveData == null) SaveData = new SaveData();

            // Level +1 and new seed
            SaveData.Level = Mathf.Max(1, SaveData.Level) + 1;
            SaveData.Seed  = newSeed;
            
            SaveData.CurrentRoomID = 0;
            SaveData.BossRoomOpen  = false;
            SaveData.BossCleared = false;
            SaveData.VisitedRooms  = new List<bool>();
            SaveData.DestroyableWallsActive = new List<bool>();
            SaveData.DestroyableWallsHealth = new List<int>();

            SaveData.Items = new List<bool>();
            
            SaveData.PlayerPosition = Vector3.zero;
            SaveData.PlayerForward  = Vector3.zero;
            SaveData.CameraForward  = Vector3.zero;
        }

        
        /// -------------------------------------------------------
        /// GETTER AND SETTER METHODS
        /// -------------------------------------------------------
    
        // ------- GLOBAL --------
        public static int GetSeed() => SaveData.Seed;
        public static int GetLevel() => SaveData.Level;
        
        // ------- CURRENT ROOM ID --------
        public static int GetCurrentRoomID() => SaveData.CurrentRoomID;
        public static void SetCurrentRoomID(int id) => SaveData.CurrentRoomID = id;
        
        // ------- BOSS ROOM DOOR OPEN --------
        public static bool GetBossRoomOpen() => SaveData.BossRoomOpen;
        public static void SetBossRoomOpen(bool open) => SaveData.BossRoomOpen = open;
        
        // ------- BOSS CLEARED --------
        public static bool GetBossCleared() => SaveData.BossCleared;
        public static void SetBossCleared(bool cleared) => SaveData.BossCleared = cleared;
        
        // ------- VISITED ROOMS --------
        public static List<bool> GetVisitedRooms() => SaveData.VisitedRooms;
        public static void InitializeVisitedRooms(int roomCount) => SaveData.VisitedRooms = new List<bool>(new bool[roomCount]);
        public static void SetRoomVisited(int roomId, bool visited)
        {
            if (SaveData.VisitedRooms != null
                && roomId >= 0
                && roomId < SaveData.VisitedRooms.Count)
            {
                SaveData.VisitedRooms[roomId] = visited;
            }
        }
        
        // ------- DESTROYABLE WALLS ACTIVE --------
        public static List<bool> GetDestroyableWallsActive()
        {
            if (SaveData.DestroyableWallsActive == null)
                SaveData.DestroyableWallsActive = new List<bool>();
            return SaveData.DestroyableWallsActive;
        }
        
        public static void SetDestroyableWallActive(int idx, bool active)
        {
            var list = GetDestroyableWallsActive();
            if (idx >= 0 && idx < list.Count)
                list[idx] = active;
        }
        
        // ------- DESTROYABLE WALLS HEALTH --------
        public static List<int> GetDestroyableWallsHealth()
        {
            if (SaveData.DestroyableWallsHealth == null)
                SaveData.DestroyableWallsHealth = new List<int>();
            return SaveData.DestroyableWallsHealth;
        }
        
        public static void SetDestroyableWallHealth(int idx, int hp)
        {
            var list = GetDestroyableWallsHealth();
            if (idx >= 0 && idx < list.Count)
                list[idx] = hp;
        }
        
        public static int GetDestroyableWallHealth(int idx)
        {
            var list = GetDestroyableWallsHealth();
            if (idx >= 0 && idx < list.Count)
                return list[idx];
            return 0;
        }
        
        // ------- PLAYER --------
        public static Vector3 GetPlayerPosition() => SaveData.PlayerPosition;
        public static void SetPlayerPosition(Vector3 pos) => SaveData.PlayerPosition = pos;
        public static Vector3 GetPlayerRotation() => SaveData.PlayerForward;
        public static void SetPlayerRotation(Vector3 direct) => SaveData.PlayerForward = direct;
        public static Vector3 GetCamRotation() => SaveData.CameraForward;
        public static void SetCamRotation(Vector3 direct) => SaveData.CameraForward = direct;
    
        // -------- STATS ---------
        public static (List<float>,List<float>) GetStats() => (SaveData.CurrentStats, SaveData.MaxStats);
        public static void SetStats(List<float> currStats, List<float> maxStats)
        {
            SaveData.CurrentStats = currStats;
            SaveData.MaxStats = maxStats;
        }
        
        // ------- ITEMS --------
        public static bool IsCollectibleActive(int idx)
        {
            if (SaveData.Items == null)
                SaveData.Items = new List<bool>();
    
            while (SaveData.Items.Count <= idx)
                SaveData.Items.Add(true);
            
            return SaveData.Items[idx];
        }

        public static void SetCollectibleActive(int idx, bool active)
        {
            if (SaveData.Items == null)
                SaveData.Items = new List<bool>();
    
            while (SaveData.Items.Count <= idx)
                SaveData.Items.Add(true);

            SaveData.Items[idx] = active;
        }

        // ------- INVENTORY --------
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

        public static void SetInventory(ItemInstance[,] grid)
        {
            for (var row = 0; row < 4; row++)
                for (var col = 0; col < 5; col++)
                    SaveData.Inventory[row * 5 + col] = grid[row, col];
        }

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

        public static void SetEquipment(ItemInstance[,] grid)
        {
            for (var row = 0; row < 3; row++)
                for (var col = 0; col < 2; col++)
                    SaveData.Equipment[row * 2 + col] = grid[row, col];
        }
    }
}
