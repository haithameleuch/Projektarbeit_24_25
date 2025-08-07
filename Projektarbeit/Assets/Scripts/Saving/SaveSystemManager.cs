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

        // ------------Inventory------------
        public static void SetInventory(ItemInstance[,] item)
        {
            for (int i = 0; i < item.GetLength(1); i++)
            {
                SaveData.I_Row_1[i] = item[0, i];
            }
            for (int i = 0; i < item.GetLength(1); i++)
            {
                SaveData.I_Row_2[i] = item[1, i];
            }
            for (int i = 0; i < item.GetLength(1); i++)
            {
                SaveData.I_Row_3[i] = item[2, i];

            }
            for (int i = 0; i < item.GetLength(1); i++)
            {
                SaveData.I_Row_4[i] = item[3, i];
            }
        }

        public static ItemInstance[,] GetInventory()
        {
            Debug.Log("ItemLadenHIERTEXTZUMFINDEN:"+(SaveData.I_Row_1[4].itemData == null));
            ItemInstance[,] inventory = new ItemInstance[4, 5];
            for (int i = 0; i < SaveData.I_Row_1.GetLength(0); i++)
            {
                if (SaveData.I_Row_1[i].itemData != null)
                {
                    inventory[0, i] = SaveData.I_Row_1[i];
                }
                else
                {
                    inventory[0, i] = null;
                }
                
            }
            for (int i = 0; i < SaveData.I_Row_2.GetLength(0); i++)
            {
                if (SaveData.I_Row_2[i].itemData != null)
                {
                    inventory[1, i] = SaveData.I_Row_2[i];
                }
                else
                {
                    inventory[1, i] = null;
                }
            }
            for (int i = 0; i < SaveData.I_Row_3.GetLength(0); i++)
            {
                
                if (SaveData.I_Row_3[i].itemData != null)
                {
                    inventory[2, i] = SaveData.I_Row_3[i];
                }
                else
                {
                    inventory[2, i] = null;
                }
            }
            for (int i = 0; i < SaveData.I_Row_4.GetLength(0); i++)
            {
                
                if (SaveData.I_Row_4[i].itemData != null)
                {
                    inventory[3, i] = SaveData.I_Row_4[i];
                }
                else
                {
                    inventory[3, i] = null;
                }
            }
            return inventory;
        }

        public static void SetEquipment(ItemInstance[,] equip)
        {
            for (int i = 0; i < equip.GetLength(1); i++)
            {
                SaveData.E_Row_1[i] = equip[0, i];
            }
            for (int i = 0; i < equip.GetLength(1); i++)
            {
                SaveData.E_Row_2[i] = equip[1, i];
            }
            for (int i = 0; i < equip.GetLength(1); i++)
            {
                SaveData.E_Row_3[i] = equip[2, i];
            }
        }

        public static ItemInstance[,] GetEquipment()
        {
            ItemInstance[,] equip = new ItemInstance[3, 2];
            for (int i = 0; i < SaveData.E_Row_1.GetLength(0); i++)
            {
                
                if (SaveData.E_Row_1[i].itemData != null)
                {
                    equip[0, i] = SaveData.E_Row_1[i];
                }
                else
                {
                    equip[0, i] = null;
                }
            }
            for (int i = 0; i < SaveData.E_Row_2.GetLength(0); i++)
            {
                
                if (SaveData.E_Row_2[i].itemData != null)
                {
                    equip[1, i] = SaveData.E_Row_2[i];
                }
                else
                {
                    equip[1, i] = null;
                }
            }
            for (int i = 0; i < SaveData.E_Row_3.GetLength(0); i++)
            {
                
                if (SaveData.E_Row_3[i].itemData != null)
                {
                    equip[2, i] = SaveData.E_Row_3[i];
                }
                else
                {
                    equip[2, i] = null;
                }
            }

            return equip;
        }
    }
}
