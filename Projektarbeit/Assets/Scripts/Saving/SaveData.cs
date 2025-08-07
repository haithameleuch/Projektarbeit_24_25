using System.Collections.Generic;
using UnityEngine;

namespace Saving
{
    public class SaveData
    {
        // Global
        public int Seed = 0;
        public int Level = 0;
    
        // Player
        public Vector3 PlayerPosition= new Vector3(0,0,0);
        public Vector3 PlayerForward = new Vector3(0,0,0);
        // --> Do not use new with Mono behaviour class // --> public Stats Stats = new();
        public Vector3 CameraForward = new Vector3(0,0,0);
        public bool PickaxeEquipped = false;
    
        // Inventory
        public ItemInstance[] I_Row_1 = new ItemInstance[5];
        public ItemInstance[] I_Row_2 = new ItemInstance[5];
        public ItemInstance[] I_Row_3 = new ItemInstance[5];
        public ItemInstance[] I_Row_4 = new ItemInstance[5];
        //public ItemInstance[,] Inventory = new ItemInstance[4,5];
        //public ItemInstance[,] Equipment = new ItemInstance[3,2];
        public ItemInstance[] E_Row_1 = new ItemInstance[2];
        public ItemInstance[] E_Row_2 = new ItemInstance[2];
        public ItemInstance[] E_Row_3 = new ItemInstance[2];
        
    
        // Items
        public List<bool> Items = new List<bool>();
    
        // Dungeon
        public int CurrentRoomID;
        public List<bool> VisitedRooms = new List<bool>();
        public List<bool> DestroyableWallsActive = new List<bool>();
        public List<int> DestroyableWallsHealth = new List<int>();
        public bool BossRoomOpen = false;
    }
}
