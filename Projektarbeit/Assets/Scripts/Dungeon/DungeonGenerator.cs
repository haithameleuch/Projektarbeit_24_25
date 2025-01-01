using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates a dungeon using a maze algorithm to create interconnected rooms with walls and doors.
/// Each room is represented by a cell in a grid and can connect to its neighbors based on the maze layout.
/// It handles the placement of rooms and spawners.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    /// <summary>
    /// Represents each cell in the dungeon grid, tracking visitation status and door connections.
    /// </summary>
    private class Cell
    {
        /// <summary>
        /// Indicates whether the cell has been visited during maze generation.
        /// </summary>
        public bool Visited;

        /// <summary>
        /// Door status for each direction:
        /// [0] Up, [1] Down, [2] Right, [3] Left.
        /// </summary>
        public bool[] Status = new bool[4];

        /// <summary>
        /// Holds the data for the room assigned to this cell.
        /// </summary>
        public RoomData RoomData;
    }

    /// <summary>
    /// Dimensions of the dungeon grid (width x height).
    /// </summary>
    [Header("Dungeon Settings")]
    [SerializeField]
    private Vector2 size;

    /// <summary>
    /// The starting cell index for the maze generation algorithm.
    /// </summary>
    [SerializeField]
    private int startPos;

    /// <summary>
    /// Array of room prefabs (Scriptable Objects) used to populate the dungeon.
    /// </summary>
    [SerializeField]
    private RoomData[] roomDataArray;

    /// <summary>
    /// Offset distance between rooms in world space.
    /// </summary>
    [SerializeField]
    private Vector2 offset;

    /// <summary>
    /// Array of item prefabs to be spawned in the dungeon rooms.
    /// </summary>
    [Header("Item Settings")]
    [SerializeField] private GameObject[] items;

    /// <summary>
    /// List of spawners used to populate dungeon rooms.
    /// </summary>
    private List<ISpawner> _spawners;

    /// <summary>
    /// Collection of cells representing the dungeon grid.
    /// </summary>
    private List<Cell> _board;

    /// <summary>
    /// Total number of cells in the dungeon grid.
    /// </summary>
    private int _maxCells;

    /// <summary>
    /// Counter for the number of cells visited during maze generation.
    /// </summary>
    private int _visitedCells;
    
    /// <summary>
    /// Keeps track of how many times each room type has been used in the dungeon.
    /// </summary>
    private Dictionary<RoomData, int> _usageCount = new();
    
    /// <summary>
    /// Maps grid coordinates to their corresponding room behavior for quick access.
    /// </summary>
    private Dictionary<Vector2Int, RoomBehaviour> _roomBehaviourMap = new();
    
    /// <summary>
    /// Singleton instance of the DungeonGenerator, ensuring there is only one instance in the scene.
    /// Provides global access to the DungeonGenerator functionality.
    /// </summary>
    public static DungeonGenerator Instance { get; private set; }

    /// <summary>
    /// Ensures there is only one instance of the DungeonGenerator in the scene.
    /// If another instance exists, it will be destroyed.
    /// Optionally persists the instance across scenes if uncommented.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Uncomment to persist the DungeonGenerator across scene loads
        // DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Initializes the dungeon generation process by setting up the grid and generating the maze.
    /// Initializes also the spawner setup.
    /// </summary>
    private void Start()
    {
        _maxCells = (int)(size.x * size.y);

        _spawners = new List<ISpawner>
        {
            new ItemSpawner(items, offset)
        };
        
        InitializeRoomDataArray(roomDataArray);
        
        MazeGenerator();
    }

    /// <summary>
    /// Instantiates room prefabs in the dungeon based on the generated maze structure.
    /// Each room is positioned in the grid and updated to reflect its door connections.
    /// Utilizes spawners to populate rooms.
    /// </summary>
    private void GenerateDungeon()
    {
        for (var i = 0; i < size.x; i++)
        {
            for (var j = 0; j < size.y; j++)
            {
                int cellIndex = Mathf.FloorToInt(i + j * size.x);
                Cell currentCell = _board[cellIndex];
                if (currentCell.Visited)
                {
                    RoomData roomData = currentCell.RoomData;
                    if (roomData == null)
                    {
                        roomData = GetRandomRoomData();
                        currentCell.RoomData = roomData;
                    }
                    
                    var newRoom = Instantiate(
                            roomData.roomPrefab,
                            new Vector3(i * offset.x, 0, -j * offset.y),
                            Quaternion.identity,
                            transform
                        )
                        .GetComponent<RoomBehaviour>();

                    newRoom.SetRoomData(roomData);
                    newRoom.UpdateRoom(currentCell.Status);
                    newRoom.name += $" {i}-{j}";
                    
                    _usageCount[roomData]++;
                    _roomBehaviourMap[new Vector2Int(i, j)] = newRoom;

                    bool isStartRoom = (i == 0 && j == 0);
                    if (roomData.roomType == RoomType.Item)
                    {
                        SpawnItems(newRoom, isStartRoom);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates a maze layout by creating paths and setting door connections between cells.
    /// Uses a depth-first search algorithm with backtracking to ensure all cells are visited.
    /// </summary>
    private void MazeGenerator()
    {
        _board = new List<Cell>();

        // Initialize the grid with unvisited cells
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                _board.Add(new Cell());
            }
        }

        int currentCell = startPos;
        Stack<int> path = new Stack<int>();

        while (_visitedCells < _maxCells)
        {
            _visitedCells++;
            _board[currentCell].Visited = true;

            // Check neighboring cells
            List<int> neighbors = CheckNeighbors(currentCell);

            if (neighbors.Count == 0)
            {
                // Backtrack if no unvisited neighbors are available
                if (path.Count == 0)
                    break;
                currentCell = path.Pop();
            }
            else
            {
                // Choose a random neighbor and move to it
                path.Push(currentCell);
                int newCell = neighbors[Random.Range(0, neighbors.Count)];

                // Set door connections based on the direction of movement
                if (newCell > currentCell)
                {
                    if (newCell - 1 == currentCell) // Moving right
                    {
                        _board[currentCell].Status[2] = true;
                        _board[newCell].Status[3] = true;
                    }
                    else // Moving down
                    {
                        _board[currentCell].Status[1] = true;
                        _board[newCell].Status[0] = true;
                    }
                }
                else
                {
                    if (newCell + 1 == currentCell) // Moving left
                    {
                        _board[currentCell].Status[3] = true;
                        _board[newCell].Status[2] = true;
                    }
                    else // Moving up
                    {
                        _board[currentCell].Status[0] = true;
                        _board[newCell].Status[1] = true;
                    }
                }
                currentCell = newCell;
            }
        }

        AssignStartRoom();
        GenerateDungeon();
        EnforceMinCounts();
    }

    /// <summary>
    /// Checks for unvisited neighboring cells to continue the maze generation.
    /// </summary>
    /// <param name="cell">The current cell index.</param>
    /// <returns>A list of unvisited neighboring cell indices.</returns>
    private List<int> CheckNeighbors(int cell)
    {
        List<int> neighbors = new List<int>();

        // Check the cell above
        if (cell - size.x >= 0 && !_board[cell - (int)size.x].Visited)
        {
            neighbors.Add(cell - (int)size.x);
        }

        // Check the cell below
        if (cell + size.x < _board.Count && !_board[cell + (int)size.x].Visited)
        {
            neighbors.Add(cell + (int)size.x);
        }

        // Check the cell to the right
        if ((cell + 1) % size.x != 0 && !_board[cell + 1].Visited)
        {
            neighbors.Add(cell + 1);
        }

        // Check the cell to the left
        if (cell % size.x != 0 && !_board[cell - 1].Visited)
        {
            neighbors.Add(cell - 1);
        }

        return neighbors;
    }
    
    /// <summary>
    /// Initializes the usage count for all room types to zero.
    /// This ensures all rooms are tracked for usage restrictions during generation.
    /// </summary>
    /// <param name="roomDataArr">The array of room data used in dungeon generation.</param>
    private void InitializeRoomDataArray(RoomData[] roomDataArr)
    {
        foreach (var roomData in roomDataArr)
        {
            _usageCount[roomData] = 0;
        }
    }
    
    /// <summary>
    /// Spawns items in the specified room using all available spawners.
    /// The start room can optionally exclude certain items or spawners.
    /// </summary>
    /// <param name="room">The room where items should be spawned.</param>
    /// <param name="isStartRoom">Indicates if the room is the starting room.</param>
    private void SpawnItems(RoomBehaviour room, bool isStartRoom)
    {
        // Use all spawners to populate the room
        foreach (var spawner in _spawners)
        {
            spawner.SpawnInRoom(room, isStartRoom);
        }
    }

    /// <summary>
    /// Assigns the starting room data to the initial cell of the dungeon grid.
    /// Ensures the start room type is set as required for the dungeon generation.
    /// </summary>
    private void AssignStartRoom()
    {
        RoomData startRoomData = roomDataArray.FirstOrDefault(roomData => roomData.roomType == RoomType.Start);
        if (startRoomData == null) return;

        int startIndex = 0;
        _board[startIndex].RoomData = startRoomData;
    }

    /// <summary>
    /// Retrieves a random room data object that meets the generation constraints.
    /// Ensures maxCount restrictions are respected, and defaults to normal rooms if no other options are valid.
    /// </summary>
    /// <returns>A valid RoomData object for dungeon placement.</returns>
    private RoomData GetRandomRoomData()
    {
        var validRoomDataList = new List<RoomData>();
        foreach (var roomData in roomDataArray)
        {
            int usage = _usageCount[roomData];

            // Skip if room has reached its maximum usage count
            if (roomData.maxCount > 0 && usage >= roomData.maxCount)
            {
                continue;
            }
            validRoomDataList.Add(roomData);
        }

        // Fallback: use normal room instead
        if (validRoomDataList.Count == 0)
        {
            var normal = roomDataArray.FirstOrDefault(roomData => roomData.roomType == RoomType.Normal);
            return normal ?? roomDataArray[0];
        }

        // Otherwise, choose a random valid room
        return validRoomDataList[Random.Range(0, validRoomDataList.Count)];
    }

    /// <summary>
    /// Ensures all rooms meet their minimum appearance requirements after dungeon generation.
    /// Logs warnings if any room type is underrepresented.
    /// </summary>
    private void EnforceMinCounts()
    {
        foreach (RoomData roomData in roomDataArray)
        {
            if (_usageCount[roomData] < roomData.minCount)
            {
                Debug.Log($"Warning! {roomData.minCount}x \"{roomData.roomName}\" required, but only {_usageCount[roomData]}x set!");
            }
        }
    }

    /// <summary>
    /// Retrieves the RoomBehaviour associated with a specific coordinate in the dungeon grid.
    /// Allows for quick access to room functionality based on its grid position.
    /// </summary>
    /// <param name="coordinate">The grid coordinate of the desired room.</param>
    /// <returns>The RoomBehaviour of the room at the specified coordinate, or null if none exists.</returns>
    public RoomBehaviour GetRoomBehaviour(Vector2Int coordinate)
    {
        if (_roomBehaviourMap.TryGetValue(coordinate, out var roomBehaviour))
        {
            return roomBehaviour;
        }

        return null;
    }
}
