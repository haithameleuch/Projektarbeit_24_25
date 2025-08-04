using System.Collections.Generic;
using UnityEngine;

public class SaveData
{
    // Global
    public int seed = 0;
    public int level = 0;
    
    // Player
    public Vector3 playerPosition= new Vector3(0,0,0);
    public Vector3 playerRotation = new Vector3(0,0,0);
    public Stats stats = new Stats();
    public Vector3 cameraRotation = new Vector3(0,0,0);
    public bool pickaxe = false;
    
    // Inventory
    public ItemInstance[,] inventory = new ItemInstance[4,5];
    public ItemInstance[,] equipment = new ItemInstance[3,2];
    
    // Items
    public List<bool> items = new List<bool>();
    
    // Dungeon
    public List<bool> visited_Rooms = new List<bool>();
    public List<int> destroyable_Walls_Health = new List<int>();
    public bool boss_Room_Open = false;
}
