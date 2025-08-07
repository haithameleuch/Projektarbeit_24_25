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

        public static void Save()
        {
            string json = JsonUtility.ToJson(SaveData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("Save written to: " + SavePath);
        }

        public static void StartNewRun(int newSeed)
        {
            SaveData = new SaveData
            {
                Seed = newSeed,
                Level = 1,
                PlayerPosition = Vector3.zero
            };
            Save();
        }
    
        public static void SetRoomVisited(int roomId, bool visited)
        {
            if (SaveData.VisitedRooms != null
                && roomId >= 0
                && roomId < SaveData.VisitedRooms.Count)
            {
                SaveData.VisitedRooms[roomId] = visited;
            }
        }
    
        // Getter and Setter
        public static void InitializeVisitedRooms(int roomCount) => SaveData.VisitedRooms = new List<bool>(new bool[roomCount]);
        public static void SetCurrentRoomID(int id) => SaveData.CurrentRoomID = id;
        public static List<bool> GetVisitedRooms() => SaveData.VisitedRooms;
        public static int GetCurrentRoomID() => SaveData.CurrentRoomID;
        public static int GetSeed() => SaveData.Seed;
        public static int GetLevel() => SaveData.Level;
        public static Vector3 GetPlayerPosition() => SaveData.PlayerPosition;
        public static void SetPlayerPosition(Vector3 pos) => SaveData.PlayerPosition = pos;
    
        public static Vector3 GetPlayerRotation() => SaveData.PlayerForward;
        public static void SetPlayerRotation(Vector3 direct) => SaveData.PlayerForward = direct;
    
        public static Vector3 GetCamRotation() => SaveData.CameraForward;
        public static void SetCamRotation(Vector3 direct) => SaveData.CameraForward = direct;
        
        // -------- DESTROYABLE WALLS ACTIVE ---------
        
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
        
        // -------- DESTROYABLE WALLS HEALTH ---------
        
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
        
        // -------- PICKAXE --------
        public static bool GetPickaxeEquipped() => SaveData.PickaxeEquipped;

        public static void SetPickaxeEquipped(bool equipped) => SaveData.PickaxeEquipped = equipped;
        
        // ------- BOSS ROOM DOOR OPEN --------
        public static bool GetBossRoomOpen() => SaveData.BossRoomOpen;
        
        public static void SetBossRoomOpen(bool open) => SaveData.BossRoomOpen = open;
    }
}
