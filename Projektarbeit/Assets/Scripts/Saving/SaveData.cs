using System.Collections.Generic;
using Inventory;
using UnityEngine;

namespace Saving
{
    /// <summary>
    /// Serializable save container for global state, dungeon state, player state, and inventory.
    /// </summary>
    public class SaveData
    {
        // ===== Global =====
        
        /// <summary>
        /// Random seed used for generation.
        /// </summary>
        public int Seed = 0;
        
        /// <summary>
        /// Current level number.
        /// </summary>
        public int Level = 0;
        
        // ===== Dungeon =====
        
        /// <summary>
        /// ID of the current room.
        /// </summary>
        public int CurrentRoomID;
        
        /// <summary>
        /// True if the boss room door is open.
        /// </summary>
        public bool BossRoomOpen = false;
        
        /// <summary>
        /// True if the boss fight is cleared.
        /// </summary>
        public bool BossCleared = false;
        
        /// <summary>
        /// Visited state for each room (true = visited).
        /// </summary>
        public List<bool> VisitedRooms = new();
        
        /// <summary>
        /// Active state for each destroyable wall.
        /// </summary>
        public List<bool> DestroyableWallsActive = new();
        
        /// <summary>
        /// Health values for each destroyable wall.
        /// </summary>
        public List<int> DestroyableWallsHealth = new();
        
        /// <summary>
        /// True if the digit mini-game is cleared.
        /// </summary>
        public bool DigitMiniGameCleared = false;
        
        /// <summary>
        /// True if the glyph mini-game is cleared.
        /// </summary>
        public bool GlyphMiniGameCleared = false;
        
        // ===== Player =====
        
        /// <summary>
        /// Saved player position.
        /// </summary>
        public Vector3 PlayerPosition= new(0,0,0);
        
        /// <summary>
        /// Saved player forward direction.
        /// </summary>
        public Vector3 PlayerForward = new(0,0,0);
        
        /// <summary>
        /// Saved camera forward direction.
        /// </summary>
        public Vector3 CameraForward = new(0,0,0);
        
        /// <summary>
        /// Current stat values.
        /// </summary>
        public List<float> CurrentStats = new();
        
        /// <summary>
        /// Max stat values.
        /// </summary>
        public List<float> MaxStats = new();
        
        // ===== Items =====
        
        /// <summary>
        /// Owned items flags per item index.
        /// </summary>
        public List<bool> Items = new();
        
        // ===== Inventory =====
        
        /// <summary>
        /// Saved inventory slots.
        /// </summary>
        public ItemInstance[] Inventory   = new ItemInstance[4 * 5];
        
        /// <summary>
        /// Saved equipment slots.
        /// </summary>
        public ItemInstance[] Equipment   = new ItemInstance[3 * 2];
    }
}
