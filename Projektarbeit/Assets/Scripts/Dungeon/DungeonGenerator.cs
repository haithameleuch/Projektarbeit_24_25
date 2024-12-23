using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a dungeon using a maze algorithm to create interconnected rooms with walls and doors.
/// Each room is represented by a cell in a grid and can connect to its neighbors based on the maze layout.
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
    }

    /// <summary>
    /// Dimensions of the dungeon grid (width x height).
    /// </summary>
    [Header("Dungeon Settings")]
    [SerializeField] private Vector2 size;

    /// <summary>
    /// The starting cell index for the maze generation algorithm.
    /// </summary>
    [SerializeField] private int startPos;

    /// <summary>
    /// Array of room prefabs used to populate the dungeon.
    /// </summary>
    [SerializeField] private GameObject[] rooms;

    /// <summary>
    /// Offset distance between rooms in world space.
    /// </summary>
    [SerializeField] private Vector2 offset;

    [Header("Item Settings")]
    [SerializeField] private GameObject[] items;

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
    /// Initializes the dungeon generation process by setting up the grid and generating the maze.
    /// </summary>
    private void Start()
    {
        _maxCells = (int)(size.x * size.y);

        _spawners = new List<ISpawner>
        {
            new ItemSpawner(items, offset)
        };
        
        MazeGenerator();
    }

    /// <summary>
    /// Instantiates room prefabs in the dungeon based on the generated maze structure.
    /// Each room is positioned in the grid and updated to reflect its door connections.
    /// </summary>
    private void GenerateDungeon()
    {
        for (var i = 0; i < size.x; i++)
        {
            for (var j = 0; j < size.y; j++)
            {
                Cell currentCell = _board[Mathf.FloorToInt(i + j * size.x)];
                if (currentCell.Visited)
                {
                    int randomRoom = Random.Range(0, rooms.Length);
                    var newRoom = Instantiate(
                        rooms[randomRoom],
                        new Vector3(i * offset.x, 0, -j * offset.y),
                        Quaternion.identity,
                        transform
                    ).GetComponent<RoomBehaviour>();

                    newRoom.UpdateRoom(currentCell.Status);
                    newRoom.name += $" {i}-{j}";

                    bool isStartRoom = (i == 0 && j == 0);
                    foreach (var spawner in _spawners)
                    {
                        spawner.SpawnInRoom(newRoom, isStartRoom);
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
                if (path.Count == 0) break;
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

        GenerateDungeon();
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
}
