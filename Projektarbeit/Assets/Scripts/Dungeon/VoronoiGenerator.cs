#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Geometry;
using Items;
using Saving;

public class VoronoiGenerator : MonoBehaviour
{
    [Header("Prefabs for Dungeon")]
    [SerializeField] private GameObject pillar;
    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject floorNormal;
    [SerializeField] private GameObject floorShader;
    [SerializeField] private GameObject floorWave;
    [SerializeField] private GameObject door;
    [SerializeField] private GameObject destroyableWall;
    
    [Header("Dungeon Settings")]
    [SerializeField] private float size = 40;
    [SerializeField] private int numPoints = 5;
    private int _seed;
    [SerializeField] private float minDoorEdgeLength = 4f;
    [SerializeField] private Material[] skyboxMaterials;
    
    [Header("Dungeon Settings for Grass, Trees and Rocks")]
    [SerializeField] private List<GameObject> grassSmallPrefabs;
    [SerializeField] private List<GameObject> grassBigPrefabs;
    [SerializeField] private List<GameObject> treePrefabs;
    [SerializeField] private List<GameObject> rockPrefabs;

    [SerializeField] private int grassSmallCount = 600;
    [SerializeField] private int grassBigCount = 80;
    [SerializeField] private int treeCount  = 40;
    [SerializeField] private int rockCount  = 80;

    [SerializeField] private float spawnMargin = 1f;
    [SerializeField] private float spawnY = 0f;


    private Light _light = new Light();
    
    public float DungeonSize => size;
    private DungeonGraph _dungeonGraph;
    private System.Random _rng;
    private readonly HashSet<string> _forcedDoorPairs = new();
    private const double DoorProbability = 0.5;
    
    // ---- DESTROYABLE WALL ----
    private int _breakableWallCounter = 0;
    // ---- DESTROYABLE WALL ----
    
    #region Debug Settings (Gizmos)
    [Header("Gizmos Debugging")]
    [SerializeField] private bool showPoints = true;
    [SerializeField] private bool showCenters = true;
    [SerializeField] private bool showTriangles = true;
    [SerializeField] private bool showShortestItemPath = true;
    [SerializeField] private bool showDoorEdgeID = true;
    [SerializeField] private bool showVoronoi = true;
    [SerializeField] private bool showBisectors = true;
    [SerializeField] private bool showVoronoiIntersections = true;
    [SerializeField] private bool showSuperTriangle = true;
    
    private List<Point> _debugPoints;
    private List<Triangle> _debugTriangles;
    private List<Point> _debugShortestItemPath;
    private List<Edge> _debugVoronoi;
    private List<Point> _debugCenters;
    private List<Edge> _debugBisectors;
    private List<Point> _debugVoronoiIntersections;
    private Triangle _debugSuperTriangle;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes dungeon generation
    /// </summary>
    public void GenerateDungeon(int seed)
    {
        _seed = seed;
        _rng = new System.Random(_seed);
        RandomizeSizeAndPoints();
        
        // ---- DESTROYABLE WALL ----
        _breakableWallCounter = 0;
        // ---- DESTROYABLE WALL ----
        
        _light = FindFirstObjectByType<Light>();
        
        GenerateDebugData();
        
        BuildAndPopulateGraph();
        AssignRoomTypes();
        PruneNeighborsByEdgeLength();
        ComputeForcedDoorPairs();
        ComputeDebugShortestItemPath();
        
        buildDungeon();
        buildVoronoi(_debugVoronoi);
        placePillars(_debugTriangles);
        
        // ONLY FOR DEBUGGING
        PrintBossRoomDoorInfo();
    }
    
    /// <summary>
    /// Generates all debug data including points, triangles, voronoi edges and centers
    /// </summary>
    private void GenerateDebugData()
    {
        _debugPoints = generatePoints(numPoints, 6.0f, size);
        _debugTriangles = BowyerWatson(_debugPoints);
        _debugVoronoi = generateVoronoi(_debugTriangles);
        _debugCenters = _debugTriangles
            .Select(t => t.getCircumcircle().center)
            .Where(c => c.x >= 0 && c.x <= size && c.y >= 0 && c.y <= size)
            .ToList();
    }
    
    private void RandomizeSizeAndPoints()
    {
        var level = Mathf.Max(1, Saving.SaveSystemManager.GetLevel());
        var rnd = new System.Random(_seed ^ 0xA2C2A);
        
        var t = Mathf.Clamp01((level - 1) / 9f);

        // Size 40..80 + small Seed-Noise (±5)
        var baseSize  = Mathf.Lerp(40f, 80f, t);
        var sizeNoise = (float)(rnd.NextDouble() * 2 - 1) * 5f;   // ±5
        size = Mathf.Clamp(Mathf.Round(baseSize + sizeNoise), 40f, 80f);

        // numPoints proportional with size 10..20 + Jitter (±2)
        var prop = (size - 40f) / 40f; // 0..1
        var basePoints = Mathf.RoundToInt(Mathf.Lerp(10f, 20f, prop));
        var jitter = rnd.Next(-2, 3);
        numPoints = Mathf.Clamp(basePoints + jitter, 10, 20);

        Debug.Log($"[Voronoi] Level{level} -> size={size}, numPoints={numPoints}");
    }
    #endregion
    
    #region Public Accessors
    /// <summary>
    /// Returns the current dungeon graph instance
    /// </summary>
    public DungeonGraph GetDungeonGraph()
    {
        return _dungeonGraph;
    }
    #endregion
    
    #region Dungeon Graph Construction
    /// <summary>
    /// Builds the dungeon graph and connects rooms with neighbors
    /// </summary>
    private void BuildAndPopulateGraph()
    {
        _dungeonGraph = new DungeonGraph(); // TODO
        var rooms = BuildRoomGraph();
        foreach (var room in rooms)
        {
            room.AddNeighbors(rooms, GetNeighbors(room.id, room));
            _dungeonGraph.AddRoom(room);
        }
    }
    
    /// <summary>
    /// Creates Room instances for each generated point
    /// </summary>
    private List<Room> BuildRoomGraph()
    {
        _dungeonGraph = new DungeonGraph(); // TODO
        List<Room> rooms = new List<Room>();
        for (int id = 0; id < _debugPoints.Count; id++)
        {
            Point roomCenter = _debugPoints[id];
            Room room = new Room(id, roomCenter);
            rooms.Add(room);
            for (int i = 0; i < 4; i++)
            {
                //i want to check the distance to the walls, not the corner points of the dungeon
                float xCoord = (i == 1) ? size : (i > 1 ? roomCenter.x : 0f);
                float yCoord = (i == 3) ? size : (i < 2 ? roomCenter.y : 0f);
                room.setIncircleRadius(Point.getDistance(roomCenter, new Point(xCoord, yCoord)));
            }
        }
        return rooms;
    }
    
    /// <summary>
    /// Finds all neighboring points (by index) in triangles that share the specified target point.
    /// </summary>
    /// <param name="pointID">Index of the target point in _debugPoints</param>
    /// <returns>A HashSet of indices representing all neighboring points</returns>
    public HashSet<int> GetNeighbors(int pointID, Room room)
    {
        Point target = _debugPoints[pointID];
        HashSet<int> neighbors = new HashSet<int>();

        foreach (Triangle t in _debugTriangles)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!Point.equals(t.points[i], target)) continue;

                int n1 = (i + 1) % 3;
                int n2 = (i + 2) % 3;

                int idx1 = _debugPoints.FindIndex(p => Point.equals(p, t.points[n1]));
                int idx2 = _debugPoints.FindIndex(p => Point.equals(p, t.points[n2]));

                if (idx1 != -1) neighbors.Add(idx1);
                if (idx2 != -1) neighbors.Add(idx2);

                break;
            }
        }

        return neighbors;
    }
    #endregion
    
    #region Dungeon Layout & Prefabs
    /// <summary>
    /// Builds the dungeon walls and the dungeon floor
    /// </summary>
    public void buildDungeon()
    {
        var mode = _seed % 3;

        if (mode == 0)
        {
            // Place a tiled floor
            for (var j = 0; j < size / 2; j++)
            {
                for (var i = 0; i < size / 2; i++)
                {
                    Instantiate(floorNormal, new Vector3((i * 2) + 1, 0, (j * 2) + 1), Quaternion.identity, transform);
                }
            }
        } else if (mode == 1)
        {
            // Place a single large floor object
            var floorObj = Instantiate(floorShader, new Vector3(size * 0.5f, 0f, size * 0.5f), Quaternion.identity, transform);
            floorObj.transform.localScale = new Vector3(size * 0.1f, 1f, size * 0.1f);

            // Material
            var rend = floorObj.GetComponent<Renderer>();
            var mat = rend.material;

            // Color list
            List<Color> baseColors = new List<Color>
            {
                new Color32(0x11, 0x00, 0xCF, 0x00),    // Blue
                new Color32(0xD4, 0x0A, 0x00, 0x00),    // Red
                new Color32(0x45, 0x8B, 0x00, 0x00)     // Green
            };

            List<Color> edgeColors = new List<Color>
            {
                new Color(93f / 255f, 246f / 255f, 255f / 255f, 0f),    // Light Blue
                new Color(255f / 255f, 243f / 255f, 0f, 0f),            // Light Red
                new Color(118f / 255f, 238f / 255f, 0f, 0f)             // Light Green
            };

            List<Color> lightColors = new List<Color>
            {
                new Color(42f / 255f, 96f / 255f, 111f / 255f),
                new Color(183f / 255f, 9f / 255f, 0f / 255f),
                new Color(76f / 255f, 1f, 101f / 255f),
            };

            // Same index for both Color lists
            var index = (_seed / 3) % baseColors.Count;

            mat.SetColor("_BaseColor", baseColors[index]);
            mat.SetColor("_EdgeColor", edgeColors[index]);
            _light.color = lightColors[index];
            RenderSettings.skybox = skyboxMaterials[index];
        }
        else
        {
            // Place a large floor with animated wave shader
            var floorObj = Instantiate(floorWave, new Vector3(size * 0.5f, 0f, size * 0.5f), Quaternion.identity, transform);
            floorObj.transform.localScale = new Vector3(size * 100f, 1f, size * 100f);
            
            // Material
            var rend = floorObj.GetComponent<Renderer>();
            var mat = rend.material;

            // Set the grid size based on dungeon size
            var gridSize = size * 1.5f;
            mat.SetFloat("_GridSize", gridSize);
        }
        
        ScatterCategory(grassSmallPrefabs, grassSmallCount, 0x1111);
        ScatterCategory(grassBigPrefabs, grassBigCount, 0x2222);
        ScatterCategory(treePrefabs,  treeCount,  0x3333);
        ScatterCategory(rockPrefabs,  rockCount,  0x4444);
        
        // Create outside walls of the dungeon
        CreateWall(new Vector3(0, 0, 0), new Vector3(size, 0, 0), -1);
        CreateWall(new Vector3(size, 0, 0), new Vector3(size, 0, size), -2);
        CreateWall(new Vector3(size, 0, size), new Vector3(0, 0, size), -3);
        CreateWall(new Vector3(0, 0, size), new Vector3(0, 0, 0), -4);
    }
    
    private void ScatterCategory(List<GameObject> prefabs, int count, int seedSalt)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0) return;
        
        var rng = new System.Random(_seed ^ seedSalt);

        float minX = spawnMargin;
        float maxX = size - spawnMargin;
        float minZ = spawnMargin;
        float maxZ = size - spawnMargin;

        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(minX, maxX, (float)rng.NextDouble());
            float z = Mathf.Lerp(minZ, maxZ, (float)rng.NextDouble());

            var prefab = prefabs[rng.Next(prefabs.Count)];
            var rotY   = (float)(rng.NextDouble() * 360.0);
            Instantiate(prefab, new Vector3(x, spawnY, z), Quaternion.Euler(0f, rotY, 0f), transform);
        }
    }

    
    /// <summary>
    /// Builds only the segmented Walls for the voronoi-edges
    /// </summary>
    /// <param name="voronoi">The list of voronoi edges</param>
    public void buildVoronoi(List<Edge> voronoi)
    {
        foreach (Edge e in voronoi)
        {
            Room r1 = _dungeonGraph.GetRoomByPoint(e.Room1);
            Room r2 = _dungeonGraph.GetRoomByPoint(e.Room2);
            
            if (r1 != null) r1.walls.Add(e.Id);
            if (r2 != null) r2.walls.Add(e.Id);
            
            CreateWall(new Vector3(e.A.x,0,e.A.y),new Vector3(e.B.x,0,e.B.y), e.Id);
        }
    }
    
    /// <summary>
    /// Places all Pillars at the intersection points of edges
    /// </summary>
    /// <param name="delaunay">The delaunay triangulation to get the circle centers</param>
    public void placePillars(List<Triangle> delaunay)
    {
        // Place Pillars on the edges to hide wall-clipping
        Instantiate(pillar, new Vector3(0, 0, 0), Quaternion.identity, transform);
        Instantiate(pillar, new Vector3(size, 0, 0), Quaternion.identity, transform);
        Instantiate(pillar, new Vector3(size, 0, size), Quaternion.identity, transform);
        Instantiate(pillar, new Vector3(0, 0, size), Quaternion.identity, transform);
        foreach (Triangle triangle in delaunay)
        {
            Point center = triangle.getCircumcircle().center;
            
            // Only Centers within the map
            if (center.x < 0 || center.x > size || center.y < 0 || center.y > size)
                continue;

            Instantiate(pillar, new Vector3(center.x, 0, center.y), Quaternion.identity, transform);
        }
    }
    
    /// <summary>
    /// Create a wall using scaled wall segments, possibly placing a door or breakable wall
    /// </summary>
    /// <param name="start">Start of the wall edge</param>
    /// <param name="end">End of the wall edge</param>
    /// <param name="edgeId">Unique identifier for this wall edge</param>
    void CreateWall(Vector3 start, Vector3 end, int edgeId)
    {
        // Get all rooms that are touching this edge
        var roomsWithEdge = _dungeonGraph.rooms.Where(r => r.walls.Contains(edgeId));
        
        // Prefab width and number of segments
        int widthOfPrefab = 2;
        int numberOfSegments = Mathf.Max(1, (int)(Vector3.Distance(start, end) / widthOfPrefab));
        float scaleOfSegment = (Vector3.Distance(start, end) / widthOfPrefab) / numberOfSegments;
        Vector3 step = (end - start) / numberOfSegments;

        // Detect whether this entire edge is an exterior wall
        bool isBoundary =
            (Mathf.Approximately(start.z, 0f)   && Mathf.Approximately(end.z, 0f))    ||    // lower edge
            (Mathf.Approximately(start.z, size) && Mathf.Approximately(end.z, size))  ||    // top edge
            (Mathf.Approximately(start.x, 0f)   && Mathf.Approximately(end.x, 0f))    ||    // left edge
            (Mathf.Approximately(start.x, size) && Mathf.Approximately(end.x, size));       // right edge

        bool longEnoughForDoor = Vector3.Distance(start, end) >= minDoorEdgeLength;
        
        // Which segment is in the middle
        int midIndex = numberOfSegments / 2;

        for (int i = 0; i < numberOfSegments; i++)
        {
            Vector3 segStart = start + step * i;
            Vector3 segEnd   = start + step * (i + 1);

            // Determine whether this edge must contain a door
            bool forceBossDoor = roomsWithEdge.Any(r => r.type == RoomType.Boss);

            bool forcePathDoor = false;
            if (roomsWithEdge.Count() == 2)
            {
                int a = roomsWithEdge.ElementAt(0).id;
                int b = roomsWithEdge.ElementAt(1).id;
                forcePathDoor = _forcedDoorPairs.Contains(GetPairKey(a, b));
            }

            bool forceDoor = forceBossDoor || forcePathDoor;
            bool forbidBreakable = roomsWithEdge.Any(r => r.type == RoomType.Boss);
            bool placeDoor = forceDoor || (_rng.NextDouble() < DoorProbability);

            bool isMiddle   = i == midIndex;
            bool candidate  = !isBoundary && isMiddle && longEnoughForDoor;

            if (candidate && (placeDoor || forceDoor))
            {
                // Place a door prefab in the middle
                Vector3 mid = (segStart + segEnd) * 0.5f;
                Quaternion rot = Quaternion.FromToRotation(Vector3.right, segEnd - segStart);

                var doorObj = Instantiate(door, mid, rot, transform);
                doorObj.transform.localScale = new Vector3(scaleOfSegment, 1f, 1f);

                if (forceBossDoor)
                {
                    SetupBossDoor(doorObj, roomsWithEdge.ToList());
                }

                // Add door to each room
                foreach (var room in roomsWithEdge)
                    room.doors.Add(edgeId);

                _dungeonGraph.idDoorDict[edgeId] = doorObj;
            }
            else if (candidate && !forbidBreakable)
            {
                // Place a breakable wall instead of a door
                
                // ---- DESTROYABLE WALL ----
                int localIdx = _breakableWallCounter++;
                
                var activeList = SaveSystemManager.GetDestroyableWallsActive();
                while (activeList.Count <= localIdx)
                    activeList.Add(true);
                
                var healthList = SaveSystemManager.GetDestroyableWallsHealth();
                while (healthList.Count <= localIdx)
                    healthList.Add(Random.Range(1, 6));
                
                if (activeList[localIdx])
                {
                    Vector3 mid = (segStart + segEnd) * 0.5f;
                    Quaternion rot = Quaternion.FromToRotation(Vector3.right, segEnd - segStart);
                    var brkObj = Instantiate(destroyableWall, mid, rot, transform);
                    brkObj.transform.localScale = new Vector3(scaleOfSegment, 1f, 1f);
                    
                    var interaction = brkObj.GetComponent<DestroyableWallInteraction>();
                    if (interaction != null)
                    {
                        interaction.edgeID = localIdx;
                        interaction.InitializeFromSave();
                    }
                }
                // ---- DESTROYABLE WALL ----
            }
            else
            {
                // Place a regular wall segment
                CreateWallSegment(segStart, segEnd, scaleOfSegment);
            }
        }
    }
    
    /// <summary>
    /// Configures a door as a boss door by adding logic and visuals 
    /// (fog wall, narrow door panel, correct rotation).
    /// </summary>
    /// <param name="doorObj">The door GameObject to configure.</param>
    /// <param name="roomsWithEdge">The two connected rooms (one must be the boss room).</param>
    void SetupBossDoor(GameObject doorObj, List<Room> roomsWithEdge)
    {
        // Ensure the door has an OpenDoor component, add one if missing
        var openDoor = doorObj.GetComponent<OpenDoor>() ??
                       doorObj.GetComponentInChildren<OpenDoor>(true) ??
                       doorObj.AddComponent<OpenDoor>();
        openDoor.isBossDoor = true;

        // Find the child object Wall_Entrance
        Transform entrance = doorObj.transform.Find("Wall_Entrance");
        if (entrance == null) return;

        // Find objects fog wall and door panel
        Transform fog = entrance.Find("Room_Door_Fog");
        Transform doorPanel = entrance.Find("Door");

        // Identify the boss room and the adjacent room (otherRoom)
        var roomList = roomsWithEdge.ToList();
        var bossRoom = roomList.FirstOrDefault(r => r.type == RoomType.Boss);
        var otherRoom = roomList.FirstOrDefault(r => r != bossRoom);

        if (bossRoom != null && otherRoom != null)
        {
            // Determine direction from boss room to the other room
            Vector3 dir = (otherRoom.center.ToVector3() - bossRoom.center.ToVector3()).normalized;
            
            // Check if the entrance is facing the wrong direction
            float alignment = Vector3.Dot(dir, entrance.forward);

            // Rotate 180° if entrance is facing the wrong direction
            if (alignment > 0f)
                entrance.Rotate(0, 180f, 0);
        }

        // Enable fog wall
        if (fog != null)
            fog.gameObject.SetActive(true);

        // Make door visually narrower
        if (doorPanel != null)
        {
            Vector3 scale = doorPanel.localScale;
            scale.z = 0.75f;
            doorPanel.localScale = scale;
        }
    }
    
    /// <summary>
    /// Create a wall segment from the prefab and scale and rotate it between two points
    /// </summary>
    /// <param name="start">The beginning of the wall</param>
    /// <param name="end">The end of the wall</param>
    void CreateWallSegment(Vector3 start, Vector3 end, float scale)
    {
        // Instantiate the wall as child
        GameObject cube = Instantiate(wall, transform);

        // Position the location of the prefab to the middle between start and end
        cube.transform.position = (start + end) / 2;
        // Scale the wall accordingly
        cube.transform.localScale = new Vector3(scale, 1f, 1f);

        // Rotate the wall correctly
        cube.transform.rotation = Quaternion.FromToRotation(Vector3.right, end - start);
    }
    #endregion
    
    #region Room Types & Door Logic
    /// <summary>
    /// Assigns room types such as Start, Boss, Item, Enemy, etc.
    /// </summary>
    private void AssignRoomTypes()
    {
        // Starting room
        int startIndex = _rng.Next(_dungeonGraph.rooms.Count);
        var startRoom = _dungeonGraph.rooms[startIndex];
        startRoom.type    = RoomType.Start;
        startRoom.visited = true;

        // Boss room
        var bossRoom = _dungeonGraph.GetFarthestRoomFrom(startRoom);
        bossRoom.type = RoomType.Boss;

        // Put all the remaining rooms that are still “Normal” into a list
        var normals = _dungeonGraph.rooms
            .Where(r => r.type == RoomType.Normal)
            .ToList();

        // Minigame rooms
        for (int i = 0; i < 2 && normals.Count > 0; i++)
        {
            int idx = _rng.Next(normals.Count);
            normals[idx].type = RoomType.MiniGame;
            normals.RemoveAt(idx);
        }

        // Item rooms
        int itemCount = Mathf.Max(1, (int)(0.2f * _dungeonGraph.rooms.Count));
        for (int i = 0; i < itemCount && normals.Count > 0; i++)
        {
            int idx = _rng.Next(normals.Count);
            normals[idx].type = RoomType.Item;
            normals.RemoveAt(idx);
        }

        // Enemy rooms
        int enemyCount = Mathf.Max(1, (int)(0.2f * _dungeonGraph.rooms.Count));
        for (int i = 0; i < enemyCount && normals.Count > 0; i++)
        {
            int idx = _rng.Next(normals.Count);
            normals[idx].type = RoomType.Enemy;
            normals.RemoveAt(idx);
        }

        // Rest are normal rooms
    }
    
    /// <summary>
    /// Computes the door path that must be traversable (start → item room)
    /// </summary>
    private void ComputeForcedDoorPairs()
    {
        _forcedDoorPairs.Clear();

        var start = _dungeonGraph.GetStartRoom();
        var firstItem = _dungeonGraph.GetAllItemRooms().FirstOrDefault();
        if (start == null || firstItem == null) return;

        var path = _dungeonGraph.FindShortestPath(start, firstItem);
        if (path == null) return;

        for (int i = 0; i < path.Count - 1; i++)
            _forcedDoorPairs.Add(GetPairKey(path[i].id, path[i + 1].id));
    }
    
    /// <summary>
    /// Returns a normalized key string for two room IDs
    /// </summary>
    private static string GetPairKey(int a, int b) => a < b ? $"{a}_{b}" : $"{b}_{a}";
    
    /// <summary>
    /// Computes the shortest path from the start room to the first item room (debug only)
    /// </summary>
    private void ComputeDebugShortestItemPath()
    {
        _debugShortestItemPath = null;

        var start = _dungeonGraph.GetStartRoom();
        var item  = _dungeonGraph.GetAllItemRooms().FirstOrDefault();
        if (start == null || item == null) return;

        var pathRooms = _dungeonGraph.FindShortestPath(start, item);
        if (pathRooms == null) return;

        _debugShortestItemPath = pathRooms.Select(r => r.center).ToList();
    }
    #endregion
    
    #region Voronoi & Delaunay Computation
    /// <summary>
    /// Generates random points with minimum spacing
    /// </summary>
    public List<Point> generatePoints(int count, float radius, float size)
    {
        List<Point> points = new List<Point>();
        int maxAttempts = 1000;

        while (points.Count < count && maxAttempts > 0)
        {
            float margin = 2f;
            float x = margin + (float)_rng.NextDouble() * (size - 2 * margin);
            float y = margin + (float)_rng.NextDouble() * (size - 2 * margin);
            Point newPoint = new Point(x, y);
            bool isValid = true;

            foreach (Point existing in points)
            {
                if (Point.getDistance(existing, newPoint) < radius)
                {
                    isValid = false;
                    break;
                }
            }
            
            if (isValid)
            {
                points.Add(newPoint);
            }
            
            maxAttempts--;
        }
        
        return points;
    }
    
    /// <summary>
    /// Calculates the Delaunay-Triangulation with a set of given Points
    /// </summary>
    /// <param name="points">The set of points used to calculate the delaunay triangulation</param>
    /// <returns>The Delaunay-Triangulation as a set of Triangles</returns>
    public List<Triangle> BowyerWatson(List<Point> points)
    {
        // List of triangles forming the Delaunay-Triangulation
        List<Triangle> triangulation = new List<Triangle>();

        // A triangle that is big enough so all points fall within its bound
        Triangle superTriangle = new Triangle(new Point(-size,-size/2), new Point(size/2,size*2.5f), new Point(2*size,-size/2));
        triangulation.Add(superTriangle);
        
        // ONLY FOR DEBUGGING
        _debugSuperTriangle = superTriangle;
        // ONLY FOR DEBUGGING

        // Add each point after point
        foreach (var point in points)
        {
            // List of invalid triangles in the triangulation
            List<Triangle> badTriangles = new List<Triangle>();
            
            // Find all invalid triangles in the triangulation
            foreach (var triangle in triangulation)
            {
                if (triangle.isInCircumcircle(point))
                {
                    badTriangles.Add(triangle);
                }
            }

            List<Edge> polygon = new List<Edge>();

            // Find boundary polygon for generation of new triangles
            foreach (var triangle in badTriangles)
            {
                foreach (var edge in triangle.edges)
                {
                    int count = 0;
                    foreach (var other in badTriangles)
                    {
                        if (Edge.equals(edge, other.edges[0]))
                        {
                            count++;
                        }
                        else if (Edge.equals(edge, other.edges[1]))
                        {
                            count++;
                        }
                        else if (Edge.equals(edge, other.edges[2]))
                        {
                            count++;
                        }
                    }

                    // Edge is only in one triangle -> at the border
                    if (count == 1)
                    {
                        polygon.Add(edge);
                    }
                }
            }

            // Remove all bad triangles
            triangulation.RemoveAll(t => badTriangles.Contains(t));
            
            // Build new triangle for every edge of the polygon
            foreach (var edge in polygon)
            {
                Triangle newTriangle = new Triangle(edge.A,edge.B,point);
                triangulation.Add(newTriangle);
            }
        }

        // Remove all triangles which contain a point of the super triangle
        triangulation.RemoveAll(t => t.containsPoints(superTriangle.points));

        return triangulation;
    }
    
    /// <summary>
    /// This Method generates the Voronoi-Diagram by drawing the bisectors of every triangle, joining matching edges and extending the border ones
    /// </summary>
    /// <param name="triangulation">A valid Delaunay-Triangulation</param>
    /// <returns>The List of Voronoi Edges</returns>
    public List<Edge> generateVoronoi(List<Triangle> triangulation)
    {
        List<Edge> bisectors = new List<Edge>();
        List<int> toRemove = new List<int>();
        List<Edge> voronoi = new List<Edge>();
        
        // ONLY FOR DEBUGGING
        _debugBisectors = new List<Edge>();
        _debugVoronoiIntersections = new List<Point>();
        // ONLY FOR DEBUGGING

        // Generate three edges from every circumcenter to the edge of the triangle
        foreach (Triangle triangle in triangulation)
        {
            Point center = triangle.getCircumcircle().center;
            
            // Only Centers within the map
            if (center.x < 0 || center.x > size || center.y < 0 || center.y > size)
                continue;
            
            // Add bisectors to a list for later use
            foreach (Edge edge in triangle.edges)
            {
                Edge eTmp = new Edge(center, new Point(((edge.A.x + edge.B.x) / 2), ((edge.A.y + edge.B.y) / 2)));
                eTmp.Room1 = edge.A;
                eTmp.Room2 = edge.B;
                bisectors.Add(eTmp);
                
                // ONLY FOR DEBUGGING
                _debugBisectors.Add(new Edge(center, new Point(((edge.A.x + edge.B.x) / 2), ((edge.A.y + edge.B.y) / 2))));
                // ONLY FOR DEBUGGING
            }
        }

        int edgeCount = bisectors.Count;

        // Connect two bisectors that meet to one big edge
        for (int i = 0; i < edgeCount - 1; i++)
        {
            for (int j = i + 1; j < edgeCount; j++)
            {
                if (Point.equals(bisectors[i].B, bisectors[j].B))
                {
                    Edge longEdge = new Edge(bisectors[i].A, bisectors[j].A);
                    longEdge.Room1 = bisectors[i].Room1;
                    longEdge.Room2 = bisectors[i].Room2;
                    longEdge.giveID();
                    voronoi.Add(longEdge);
                    toRemove.Add(i);
                    toRemove.Add(j);
                }
            }
        }

        // Prevent out of bounds exception
        int offset = 0;
        toRemove.Sort();

        // Remove all bisectors that have been connected
        foreach (int count in toRemove)
        {
            bisectors.RemoveAt(count - offset);
            offset++;
        }

        // For each remaining bisector, so every bisector that goes to the outside of the dungeon
        foreach (Edge e in bisectors)
        {
            // Finde das eine Dreieck, dem dieser Bisektor entstammt
            Triangle owner = null;
            foreach (Triangle tri in triangulation)
            {
                if (Point.equals(tri.getCircumcircle().center, e.A))
                {
                    owner = tri;
                    break;
                }
            }
            if (owner == null) continue;

            // direction bisector
            Vector2 dir = new Vector2(e.B.x - e.A.x, e.B.y - e.A.y);
            dir.Normalize();

            // Checkpoints slightly forward and backward
            Point forwardTest = new Point(e.B.x + dir.x * 0.01f, e.B.y + dir.y * 0.01f);
            bool forwardInside = !PointOutTriangle(forwardTest, owner.points[0], owner.points[1], owner.points[2]);

            // If the forward point is inside,
            // we have to extend backwards outwards, otherwise forwards.
            Vector2 rayDir = forwardInside ? new Vector2(-dir.x, -dir.y) : dir;

            // Build a ray from the circumcenter far outwards
            Point far = new Point(e.A.x + rayDir.x * size * 2f, e.A.y + rayDir.y * size * 2f);
            Edge ray = new Edge(e.A, far);

            // Intersection with the map boundary
            Point inter = FindIntersectionWithMapBoundary(ray, size);
            _debugVoronoiIntersections.Add(inter);

            // New Voronoi-Edge
            Edge newEdge = new Edge(e.A, inter);
            newEdge.Room1 = e.Room1;
            newEdge.Room2 = e.Room2;
            newEdge.giveID();
            voronoi.Add(newEdge);
        }
        // Return the voronoi diagramm as a list of Edges
        return voronoi;
    }
    #endregion
    
    #region Geometric Helpers
    /// <summary>
    /// Checks if a Point is outside a triangle
    /// </summary>
    /// <param name="pt">The point we want to know</param>
    /// <param name="v1">Point A</param>
    /// <param name="v2">Point B</param>
    /// <param name="v3">Point C</param>
    /// <returns>Wether the point is outside(true) or inside (false)</returns>
    private bool PointOutTriangle(Point pt, Point v1, Point v2, Point v3)
    {
        float d1, d2, d3;
        bool has_neg, has_pos;

        d1 = Sign(pt, v1, v2);
        d2 = Sign(pt, v2, v3);
        d3 = Sign(pt, v3, v1);

        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        
        return (has_neg && has_pos);
    }
    
    /// <summary>
    /// Computes the sign of the area for three points (used in triangle check)
    /// </summary>
    private float Sign(Point p1, Point p2, Point p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
    
    /// <summary>
    /// Finds the intersection point of an edge with the map boundary
    /// </summary>
    private Point FindIntersectionWithMapBoundary(Edge edge, float size)
    {
        float dx = edge.B.x - edge.A.x;
        float dy = edge.B.y - edge.A.y;

        float m = 0;
        float b = 0;

        bool vertical = Mathf.Abs(dx) < 1e-5f;
        bool horizontal = Mathf.Abs(dy) < 1e-5f;

        List<Point> candidates = new List<Point>();

        if (vertical)
        {
            candidates.Add(new Point(edge.B.x, 0));
            candidates.Add(new Point(edge.B.x, size));
        }
        else if (horizontal)
        {
            candidates.Add(new Point(0, edge.B.y));
            candidates.Add(new Point(size, edge.B.y));
        }
        else
        {
            m = dy / dx;
            b = edge.B.y - m * edge.B.x;

            candidates.Add(new Point(0, b));                    // left
            candidates.Add(new Point(size, m * size + b));      // right
            candidates.Add(new Point((0 - b) / m, 0));          // bottom
            candidates.Add(new Point((size - b) / m, size));    // top
        }

        // Only valid points in Range [0, size]
        candidates.RemoveAll(p => p.x < 0 || p.x > size || p.y < 0 || p.y > size);

        return checkPoints(candidates, edge.B);
    }
    
    /// <summary>
    /// Find the closest Point from a list to another point
    /// </summary>
    /// <param name="points">The list of points we want to get the closest from</param>
    /// <param name="reference">The point we want to get closest to</param>
    /// <returns>the point from the list that is closest to reference</returns>
    private Point checkPoints(List<Point> points, Point reference)
    {
        Point result= new Point(0,0);
        float minDistance = 10000;
        foreach (Point p in points)
        {
            float distx = reference.x - p.x;
            float disty = reference.y - p.y;
            float distance = Mathf.Sqrt((distx*distx)+(disty*disty));
            if (distance < minDistance)
            {
                minDistance = distance;
                result = p;
            }
        }
        return result;
    }
    #endregion
    
    #region Graph Pruning & Edge Filtering
    /// <summary>
    /// Removes neighbors connected by too short Voronoi edges
    /// </summary>
    private void PruneNeighborsByEdgeLength()
    {
        foreach (Room room in _dungeonGraph.rooms)
        {
            room.neighbors.RemoveAll(other => !HasTraversableEdge(room, other));
        }
    }
    
    /// <summary>
    /// Checks if there is a traversable Voronoi edge between two rooms
    /// </summary>
    private bool HasTraversableEdge(Room a, Room b)
    {
        foreach (Edge e in _debugVoronoi)
        {
            bool matches = (Point.equals(e.Room1, a.center) && Point.equals(e.Room2, b.center)) ||
                           (Point.equals(e.Room1, b.center) && Point.equals(e.Room2, a.center));

            if (!matches) continue;

            float len = Point.getDistance(e.A, e.B);
            if (len >= minDoorEdgeLength) return true;
        }
        return false;
    }
    #endregion
    
    #region Debug Gizmos & Logging
    /// <summary>
    /// Logs detailed info about the boss room and its door edges
    /// </summary>
    private void PrintBossRoomDoorInfo()
    {
        var bossRoom = _dungeonGraph.GetBossRoom();
        if (bossRoom == null)
        {
            Debug.LogError("Boss room not found!");
            return;
        }

        Debug.Log($"[BossRoom] ID: {bossRoom.id} has {bossRoom.doors.Count} door(s)");
        Debug.Log($"[BossRoom] ID: {bossRoom.id} has {bossRoom.neighbors.Count} neighbor(s)");

        string neighborIds = string.Join(", ", bossRoom.neighbors.Select(n => n.id));
        Debug.Log($"[BossRoom] Neighbor IDs: {neighborIds}");

        foreach (int edgeId in bossRoom.doors)
        {
            string isInstantiated = _dungeonGraph.idDoorDict.ContainsKey(edgeId) ? "✅ Door exists in dict" : "❌ Not in dict";
            Debug.Log($"[BossRoom] Door EdgeID: {edgeId} — {isInstantiated}");
        }

        Debug.Log("[BossRoom] All wall edges:");
        foreach (int wallId in bossRoom.walls)
        {
            Debug.Log($"[BossRoom] Wall EdgeID: {wallId}");
        }
    }
    
    /// <summary>
    /// Draws various debug visualizations (points, triangles, labels, edges, etc.)
    /// </summary>
    private void OnDrawGizmos()
    {
        // RANDOM POINTS (GREEN)
        if (_debugPoints == null) return;

        #if UNITY_EDITOR
        if (showPoints)
        {
            Gizmos.color = Color.green;
        
            for(int i = 0; i < _debugPoints.Count; i++)
            {
                Point p = _debugPoints[i];
                Vector3 pos = new Vector3(p.x, 0.5f, p.y);
                Gizmos.DrawCube(pos, Vector3.one);
                
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                Room room = _dungeonGraph.GetRoomByID(i);
                Color labelColor = Color.white;

                switch (room.type)
                {
                    case RoomType.Boss:     labelColor = Color.red;     break;
                    case RoomType.MiniGame: labelColor = Color.cyan;    break;
                    case RoomType.Item:     labelColor = Color.yellow;  break;
                    case RoomType.Enemy:    labelColor = Color.magenta; break;
                    case RoomType.Start:    labelColor = Color.blue;    break;
                    case RoomType.Normal:   labelColor = Color.white;   break;
                }

                //Set the color before drawing the label
                style.normal.textColor = labelColor;
                Handles.Label(pos + Vector3.up * 0.5f, $"\nPoint {i} : {room.type}\nVisited: {room.visited}", style);
            }
        }
        
        // CENTER (WHITE)
        if (showCenters && _debugCenters != null)
        {
            Gizmos.color = Color.white;
            foreach (Point center in _debugCenters)
            {
                Vector3 pos = new Vector3(center.x, 0.5f, center.y);
                Gizmos.DrawSphere(pos, 0.5f);
            }
        }
        
        // DELAUNAY TRIANGULATION (BLUE)
        if (showTriangles && _debugTriangles != null)
        {
            Gizmos.color = Color.blue;
            foreach (Triangle triangle in _debugTriangles)
            {
                DrawLine(triangle.points[0], triangle.points[1]);
                DrawLine(triangle.points[1], triangle.points[2]);
                DrawLine(triangle.points[2], triangle.points[0]);
            }
        }
        
        // SHOW SHORTEST ITEM PATH (ORANGE)
        if (showShortestItemPath && _debugShortestItemPath != null && _debugShortestItemPath.Count > 1)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            for (int i = 0; i < _debugShortestItemPath.Count - 1; i++)
                DrawLine(_debugShortestItemPath[i], _debugShortestItemPath[i + 1]);
        }
        
        // DOOR + EDGE ID LABELS (WHITE)
        if (showDoorEdgeID && _dungeonGraph != null)
        {
            GUIStyle doorStyle = new GUIStyle();
            doorStyle.normal.textColor = Color.white;
            
            foreach (var kvp in _dungeonGraph.idDoorDict)
            {
                int id = kvp.Key;
                GameObject doorObj = kvp.Value;
                if (doorObj == null) continue;

                Vector3 labelPos = doorObj.transform.position + Vector3.up * 0.5f;
                Handles.Label(labelPos, $"Edge: {id}\nDoor: {id}", doorStyle);
            }
        }
        
        // ALL EDGE ID THAT DO NOT HAVE A DOOR (ORANGE) 
        if (showDoorEdgeID && _debugVoronoi != null && _dungeonGraph != null)
        {
            GUIStyle wallStyle = new GUIStyle();
            wallStyle.normal.textColor = new Color(1f, 0.5f, 0f);
    
            foreach (Edge edge in _debugVoronoi)
            {
                if (!_dungeonGraph.idDoorDict.ContainsKey(edge.Id))
                {
                    Vector3 labelPos = new Vector3((edge.A.x + edge.B.x) * 0.5f, 0.5f, (edge.A.y + edge.B.y) * 0.5f);
                    Handles.Label(labelPos, $"Edge: {edge.Id}\n(Wall)", wallStyle);
                }
            }
        }
        
        // VORONOI TRIANGULATION (YELLOW)
        if (showVoronoi && _debugVoronoi != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Edge edge in _debugVoronoi)
            {
                DrawLine(edge.A, edge.B);
            }
        }
        
        // BISEKTOREN (MAGENTA)
        if (showBisectors && _debugBisectors != null)
        {
            Gizmos.color = Color.magenta;
            foreach (Edge edge in _debugBisectors)
            {
                DrawLine(edge.A, edge.B);
            }
        }

        // INTERSECTIONS (GRAY)
        if (showVoronoiIntersections && _debugVoronoiIntersections != null)
        {
            Gizmos.color = Color.gray;
            foreach (Point point in _debugVoronoiIntersections)
            {
                Vector3 pos = new Vector3(point.x, 0.5f, point.y);
                Gizmos.DrawSphere(pos, 0.5f);
            }
        }
        
        // SUPERTRIANGLE (BLACK)
        if (showSuperTriangle && _debugSuperTriangle != null)
        {
            Gizmos.color = Color.black;
            DrawLine(_debugSuperTriangle.points[0], _debugSuperTriangle.points[1]);
            DrawLine(_debugSuperTriangle.points[1], _debugSuperTriangle.points[2]);
            DrawLine(_debugSuperTriangle.points[2], _debugSuperTriangle.points[0]);
        }
        #endif
    }
    
    /// <summary>
    /// Draws a line between two points in Gizmos
    /// </summary>
    private void DrawLine(Point a, Point b)
    {
        Vector3 start = new Vector3(a.x, 0.5f, a.y);
        Vector3 end = new Vector3(b.x, 0.5f, b.y);
        Gizmos.DrawLine(start, end);
    }
    #endregion
}
