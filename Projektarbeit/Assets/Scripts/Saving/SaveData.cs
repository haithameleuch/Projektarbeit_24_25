using System.Collections.Generic;
using UnityEngine;

namespace Saving
{
    public class SaveData
    {
        // Global
        public int Seed = 0;
        public int Level = 0;
        
        // Dungeon
        public int CurrentRoomID;
        public bool BossRoomOpen = false;
        public bool BossCleared = false;
        public List<bool> VisitedRooms = new();
        public List<bool> DestroyableWallsActive = new();
        public List<int> DestroyableWallsHealth = new();
        public bool DigitMiniGameCleared = false;
        public bool GlyphMiniGameCleared = false;
        
        // Player
        public Vector3 PlayerPosition= new(0,0,0);
        public Vector3 PlayerForward = new(0,0,0);
        public Vector3 CameraForward = new(0,0,0);
        public List<float> CurrentStats = new(); 
        public List<float> MaxStats = new();
        
        // Items
        public List<bool> Items = new();
        
        // Inventory
        public ItemInstance[] Inventory   = new ItemInstance[4 * 5];
        public ItemInstance[] Equipment   = new ItemInstance[3 * 2];
    }
}
