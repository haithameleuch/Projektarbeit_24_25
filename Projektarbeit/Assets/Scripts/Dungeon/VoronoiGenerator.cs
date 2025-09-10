using System.Collections.Generic;
using System.Linq;
using Items;
using Saving;
using UnityEditor;
using UnityEngine;

namespace Dungeon
{
    /// <summary>
    /// Generates a procedural dungeon using Voronoi/Delaunay and builds the scene
    /// (floors, walls, doors, pillars). Also assigns room types and compute paths.
    /// </summary>
    public class VoronoiGenerator : MonoBehaviour
    {
        /// <summary>
        /// Pillar prefab placed at room intersections and map corners.
        /// </summary>
        [Header("Prefabs for Dungeon")]
        [SerializeField] private GameObject pillar;
        
        /// <summary>
        /// Wall segment prefab for regular walls.
        /// </summary>
        [SerializeField] private GameObject wall;
        
        /// <summary>
        /// Tiled floor prefab (placed in a grid).
        /// </summary>
        [SerializeField] private GameObject floorNormal;
        
        /// <summary>
        /// Large floor prefab that uses a shader material.
        /// </summary>
        [SerializeField] private GameObject floorShader;
        
        /// <summary>
        /// Large floor prefab with an animated wave shader.
        /// </summary>
        [SerializeField] private GameObject floorWave;
        
        /// <summary>
        /// Door prefab inserted into wall lines.
        /// </summary>
        [SerializeField] private GameObject door;
        
        /// <summary>
        /// Breakable wall prefab used instead of a door in some spots.
        /// </summary>
        [SerializeField] private GameObject destroyableWall;
    
        /// <summary>
        /// Side length of the square dungeon area.
        /// </summary>
        [Header("Dungeon Settings")]
        [SerializeField] private float size = 40;
        
        /// <summary>
        /// Number of seed points used to generate rooms.
        /// </summary>
        [SerializeField] private int numPoints = 5;

        /// <summary>
        /// Minimum-edge length required to allow a door.
        /// </summary>
        [SerializeField] private float minDoorEdgeLength = 4f;
        
        /// <summary>
        /// Skybox materials rotated based on the seed.
        /// </summary>
        [SerializeField] private Material[] skyboxMaterials;

        /// <summary>
        /// Small grass prefabs for scattering.
        /// </summary>
        [Header("Dungeon Settings for Grass, Trees and Rocks")]
        [SerializeField] private List<GameObject> grassSmallPrefabs;

        /// <summary>
        /// Big grass prefabs for scattering.
        /// </summary>
        [SerializeField] private List<GameObject> grassBigPrefabs;
        
        /// <summary>
        /// Tree prefabs for scattering.
        /// </summary>
        [SerializeField] private List<GameObject> treePrefabs;
        
        /// <summary>
        /// Rock prefabs for scattering.
        /// </summary>
        [SerializeField] private List<GameObject> rockPrefabs;

        /// <summary>
        /// Number of small grass instances to spawn.
        /// </summary>
        [SerializeField] private int grassSmallCount = 600;
        
        /// <summary>
        /// Number of big grass instances to spawn.
        /// </summary>
        [SerializeField] private int grassBigCount = 80;
        
        /// <summary>
        /// Number of trees to spawn.
        /// </summary>
        [SerializeField] private int treeCount  = 40;
        
        /// <summary>
        /// Number of rocks to spawn.
        /// </summary>
        [SerializeField] private int rockCount  = 80;

        /// <summary>
        /// Margin from the outer walls used when scattering props.
        /// </summary>
        [SerializeField] private float spawnMargin = 1f;
        
        /// <summary>
        /// Y position for scattered props.
        /// </summary>
        [SerializeField] private float spawnY;

        /// <summary>
        /// Public accessor for the current dungeon size.
        /// </summary>
        public float DungeonSize => size;
        
        /// <summary>
        /// ID of the chosen item room used for the forced path (-1 if none).
        /// </summary>
        public int ForcedItemRoomId { get; private set; } = -1;

        /// <summary>
        /// The generated dungeon graph (rooms, doors, walls).
        /// </summary>
        private DungeonGraph _dungeonGraph;
        
        /// <summary>
        /// Random generator scoped to this run.
        /// </summary>
        private System.Random _rng;
        
        /// <summary>
        /// Current generation seed.
        /// </summary>
        private int _seed;
        
        /// <summary>
        /// Set of normalized "roomA_roomB" keys that must have a door.
        /// </summary>
        private readonly HashSet<string> _forcedDoorPairs = new();
        
        /// <summary>
        /// Base probability to place a door on eligible edges.
        /// </summary>
        private const double DoorProbability = 0.5;
        
        /// <summary>
        /// Reference to the main directional light (for stylistic changes).
        /// </summary>
        private Light _light = new();

        /// <summary>
        /// Running counter used to index breakable walls in the save.
        /// </summary>
        private int _breakableWallCounter;
        
    
        #region Debug Settings (Gizmos)
        
        /// <summary>
        /// Show generated points in the scene view.
        /// </summary>
        [Header("Gizmos Debugging")]
        [SerializeField] private bool showPoints = true;
        
        /// <summary>
        /// Show circumcenters used by Voronoi.
        /// </summary>
        [SerializeField] private bool showCenters = true;
        
        /// <summary>
        /// Show Delaunay triangles.
        /// </summary>
        [SerializeField] private bool showTriangles = true;
        
        /// <summary>
        /// Show the computed shortest path to an item room.
        /// </summary>
        [SerializeField] private bool showShortestItemPath = true;
        
        /// <summary>
        /// Show edge IDs and placed door IDs.
        /// </summary>
        [SerializeField] private bool showDoorEdgeID = true;
        
        /// <summary>
        /// Show Voronoi edges.
        /// </summary>
        [SerializeField] private bool showVoronoi = true;
        
        /// <summary>
        /// Show triangle bisectors.
        /// </summary>
        [SerializeField] private bool showBisectors = true;
        
        /// <summary>
        /// Show intersection points with the map boundary.
        /// </summary>
        [SerializeField] private bool showVoronoiIntersections = true;
        
        /// <summary>
        /// Show the super triangle used during triangulation.
        /// </summary>
        [SerializeField] private bool showSuperTriangle = true;
    
        /// <summary>
        /// Random points used for generation.
        /// </summary>
        private List<Point> _debugPoints;
        
        /// <summary>
        /// Delaunay triangles.
        /// </summary>
        private List<Triangle> _debugTriangles;
        
        /// <summary>
        /// Path centers for the item path.
        /// </summary>
        private List<Point> _debugShortestItemPath;
        
        /// <summary>
        /// Voronoi edges.
        /// </summary>
        private List<Edge> _debugVoronoi;
        
        /// <summary>
        /// Circumcenters inside the map.
        /// </summary>
        private List<Point> _debugCenters;
        
        /// <summary>
        /// Raw bisectors drawn per triangle.
        /// </summary>
        private List<Edge> _debugBisectors;
        
        /// <summary>
        /// Intersections of rays with the boundary.
        /// </summary>
        private List<Point> _debugVoronoiIntersections;
        
        /// <summary>
        /// Super triangle.
        /// </summary>
        private Triangle _debugSuperTriangle;
        #endregion

        #region Initialization
        /// <summary>
        /// Entry point to generate a new dungeon with the given seed.
        /// </summary>
        /// <param name="seed">Seed for random generation.</param>
        public void GenerateDungeon(int seed)
        {
            _seed = seed;
            _rng = new System.Random(_seed);
            RandomizeSizeAndPoints();
            
            _breakableWallCounter = 0;
        
            _light = FindFirstObjectByType<Light>();
        
            GenerateDebugData();
        
            BuildAndPopulateGraph();
            AssignRoomTypes();
            PruneNeighborsByEdgeLength();
            ComputeForcedDoorPairs();
            ComputeDebugShortestItemPath();
        
            BuildDungeon();
            BuildVoronoi(_debugVoronoi);
            PlacePillars(_debugTriangles);
        
            // ONLY FOR DEBUGGING
            // PrintBossRoomDoorInfo();
        }
    
        /// <summary>
        /// Generates data: points, triangles, Voronoi edges, and centers.
        /// </summary>
        private void GenerateDebugData()
        {
            _debugPoints = GeneratePoints(numPoints, 6.0f, size);
            _debugTriangles = BowyerWatson(_debugPoints);
            _debugVoronoi = GenerateVoronoi(_debugTriangles);
            _debugCenters = _debugTriangles
                .Select(t => t.GetCircumcircle().Center)
                .Where(c => c.X >= 0 && c.X <= size && c.Y >= 0 && c.Y <= size)
                .ToList();
        }
    
        /// <summary>
        /// Randomizes dungeon size and number of points based on the current level and seed.
        /// </summary>
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
        /// Returns the current dungeon graph instance.
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
            _dungeonGraph = new DungeonGraph();
            var rooms = BuildRoomGraph();
            foreach (var room in rooms)
            {
                room.AddNeighbors(rooms, GetNeighbors(room.ID, room));
                _dungeonGraph.AddRoom(room);
            }
        }
        
        /// <summary>
        /// Creates room instances from the generated points and seeds their incircle radius.
        /// </summary>
        private List<Room> BuildRoomGraph()
        {
            _dungeonGraph = new DungeonGraph();
            var rooms = new List<Room>();
            for (var id = 0; id < _debugPoints.Count; id++)
            {
                var roomCenter = _debugPoints[id];
                var room = new Room(id, roomCenter);
                rooms.Add(room);
                for (var i = 0; i < 4; i++)
                {
                    //I want to check the distance to the walls, not the corner points of the dungeon
                    var xCoord = (i == 1) ? size : (i > 1 ? roomCenter.X : 0f);
                    var yCoord = (i == 3) ? size : (i < 2 ? roomCenter.Y : 0f);
                    room.SetIncircleRadius(Point.GetDistance(roomCenter, new Point(xCoord, yCoord)));
                }
            }
            return rooms;
        }
        
        /// <summary>
        /// Finds neighboring point indices that share a triangle with the given point.
        /// </summary>
        /// <param name="pointID">Index of the target point in _debugPoints</param>
        /// <param name="room">Room for which neighbors are evaluated (unused, kept for context).</param>
        /// <returns>A HashSet of indices representing all neighboring points</returns>
        private HashSet<int> GetNeighbors(int pointID, Room room)
        {
            var target = _debugPoints[pointID];
            var neighbors = new HashSet<int>();

            foreach (var t in _debugTriangles)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (!Point.Equals(t.Points[i], target)) continue;

                    var n1 = (i + 1) % 3;
                    var n2 = (i + 2) % 3;

                    var idx1 = _debugPoints.FindIndex(p => Point.Equals(p, t.Points[n1]));
                    var idx2 = _debugPoints.FindIndex(p => Point.Equals(p, t.Points[n2]));

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
        /// Builds the dungeon floor, scatters props, and creates outer walls.
        /// </summary>
        private void BuildDungeon()
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
                var baseColors = new List<Color>
                {
                    new Color32(0x11, 0x00, 0xCF, 0x00),    // Blue
                    new Color32(0xD4, 0x0A, 0x00, 0x00),    // Red
                    new Color32(0x45, 0x8B, 0x00, 0x00)     // Green
                };

                var edgeColors = new List<Color>
                {
                    new Color(93f / 255f, 246f / 255f, 255f / 255f, 0f),    // Light Blue
                    new Color(255f / 255f, 243f / 255f, 0f, 0f),            // Light Red
                    new Color(118f / 255f, 238f / 255f, 0f, 0f)             // Light Green
                };

                var lightColors = new List<Color>
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
    
        /// <summary>
        /// Scatters a category of prefabs randomly inside the map borders.
        /// </summary>
        /// <param name="prefabs">Prefabs to choose from.</param>
        /// <param name="count">How many instances to spawn.</param>
        /// <param name="seedSalt">Salt added to the seed for independent randomness.</param>
        private void ScatterCategory(List<GameObject> prefabs, int count, int seedSalt)
        {
            if (prefabs == null || prefabs.Count == 0 || count <= 0) return;
        
            var rng = new System.Random(_seed ^ seedSalt);

            var minX = spawnMargin;
            var maxX = size - spawnMargin;
            var minZ = spawnMargin;
            var maxZ = size - spawnMargin;

            for (var i = 0; i < count; i++)
            {
                var x = Mathf.Lerp(minX, maxX, (float)rng.NextDouble());
                var z = Mathf.Lerp(minZ, maxZ, (float)rng.NextDouble());

                var prefab = prefabs[rng.Next(prefabs.Count)];
                var rotY   = (float)(rng.NextDouble() * 360.0);
                Instantiate(prefab, new Vector3(x, spawnY, z), Quaternion.Euler(0f, rotY, 0f), transform);
            }
        }
        
        /// <summary>
        /// Builds only the segmented Walls for the voronoi-edges
        /// </summary>
        /// <param name="voronoi">The list of voronoi edges</param>
        private void BuildVoronoi(List<Edge> voronoi)
        {
            foreach (Edge e in voronoi)
            {
                var r1 = _dungeonGraph.GetRoomByPoint(e.Room1);
                var r2 = _dungeonGraph.GetRoomByPoint(e.Room2);
            
                if (r1 != null) r1.Walls.Add(e.Id);
                if (r2 != null) r2.Walls.Add(e.Id);
            
                CreateWall(new Vector3(e.A.X,0,e.A.Y),new Vector3(e.B.X,0,e.B.Y), e.Id);
            }
        }
    
        /// <summary>
        /// Places all Pillars at the intersection points of edges and at the corner points
        /// </summary>
        /// <param name="delaunay">The delaunay triangulation to get the circle centers</param>
        private void PlacePillars(List<Triangle> delaunay)
        {
            // Place Pillars on the edges to hide wall-clipping
            Instantiate(pillar, new Vector3(0, 0, 0), Quaternion.identity, transform);
            Instantiate(pillar, new Vector3(size, 0, 0), Quaternion.identity, transform);
            Instantiate(pillar, new Vector3(size, 0, size), Quaternion.identity, transform);
            Instantiate(pillar, new Vector3(0, 0, size), Quaternion.identity, transform);
            foreach (var triangle in delaunay)
            {
                var center = triangle.GetCircumcircle().Center;
            
                // Only Centers within the map
                if (center.X < 0 || center.X > size || center.Y < 0 || center.Y > size)
                    continue;

                Instantiate(pillar, new Vector3(center.X, 0, center.Y), Quaternion.identity, transform);
            }
        }
    
        /// <summary>
        /// Create a wall using scaled wall segments, possibly placing a door or breakable wall
        /// </summary>
        /// <param name="start">Start of the wall edge</param>
        /// <param name="end">End of the wall edge</param>
        /// <param name="edgeId">Unique identifier for this wall edge</param>
        private void CreateWall(Vector3 start, Vector3 end, int edgeId)
        {
            // Get all rooms that are touching this edge
            var roomsWithEdge = _dungeonGraph.Rooms.Where(r => r.Walls.Contains(edgeId));
        
            // Prefab width and number of segments
            var widthOfPrefab = 2;
            var numberOfSegments = Mathf.Max(1, (int)(Vector3.Distance(start, end) / widthOfPrefab));
            var scaleOfSegment = (Vector3.Distance(start, end) / widthOfPrefab) / numberOfSegments;
            var step = (end - start) / numberOfSegments;

            // Detect whether this entire edge is an exterior wall
            var isBoundary =
                (Mathf.Approximately(start.z, 0f)   && Mathf.Approximately(end.z, 0f))    ||    // lower edge
                (Mathf.Approximately(start.z, size) && Mathf.Approximately(end.z, size))  ||    // top edge
                (Mathf.Approximately(start.x, 0f)   && Mathf.Approximately(end.x, 0f))    ||    // left edge
                (Mathf.Approximately(start.x, size) && Mathf.Approximately(end.x, size));       // right edge

            var longEnoughForDoor = Vector3.Distance(start, end) >= minDoorEdgeLength;
        
            // Which segment is in the middle
            var midIndex = numberOfSegments / 2;

            for (var i = 0; i < numberOfSegments; i++)
            {
                var segStart = start + step * i;
                var segEnd   = start + step * (i + 1);

                // Determine whether this edge must contain a door
                var forceBossDoor = roomsWithEdge.Any(r => r.Type == RoomType.Boss);

                var forcePathDoor = false;
                if (roomsWithEdge.Count() == 2)
                {
                    var a = roomsWithEdge.ElementAt(0).ID;
                    var b = roomsWithEdge.ElementAt(1).ID;
                    forcePathDoor = _forcedDoorPairs.Contains(GetPairKey(a, b));
                }

                var forceDoor = forceBossDoor || forcePathDoor;
                var forbidBreakable = roomsWithEdge.Any(r => r.Type == RoomType.Boss);
                var placeDoor = forceDoor || (_rng.NextDouble() < DoorProbability);

                var isMiddle   = i == midIndex;
                var candidate  = !isBoundary && isMiddle && longEnoughForDoor;

                if (candidate && (placeDoor || forceDoor))
                {
                    // Place a door prefab in the middle
                    var mid = (segStart + segEnd) * 0.5f;
                    var rot = Quaternion.FromToRotation(Vector3.right, segEnd - segStart);

                    var doorObj = Instantiate(door, mid, rot, transform);
                    doorObj.transform.localScale = new Vector3(scaleOfSegment, 1f, 1f);

                    if (forceBossDoor)
                    {
                        SetupBossDoor(doorObj, roomsWithEdge.ToList());
                    }

                    // Add a door to each room
                    foreach (var room in roomsWithEdge)
                        room.Doors.Add(edgeId);

                    _dungeonGraph.IDDoorDict[edgeId] = doorObj;
                }
                else if (candidate && !forbidBreakable)
                {
                    // Place a breakable wall instead of a door
                    
                    var localIdx = _breakableWallCounter++;
                
                    var activeList = SaveSystemManager.GetDestroyableWallsActive();
                    while (activeList.Count <= localIdx)
                        activeList.Add(true);
                
                    var healthList = SaveSystemManager.GetDestroyableWallsHealth();
                    while (healthList.Count <= localIdx)
                        healthList.Add(Random.Range(1, 6));
                
                    if (activeList[localIdx])
                    {
                        var mid = (segStart + segEnd) * 0.5f;
                        var rot = Quaternion.FromToRotation(Vector3.right, segEnd - segStart);
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
        private void SetupBossDoor(GameObject doorObj, List<Room> roomsWithEdge)
        {
            // Ensure the door has an OpenDoor component, add one if missing
            var openDoor = doorObj.GetComponent<OpenDoor>() ??
                           doorObj.GetComponentInChildren<OpenDoor>(true) ??
                           doorObj.AddComponent<OpenDoor>();
            openDoor.isBossDoor = true;

            // Find the child object Wall_Entrance
            var entrance = doorObj.transform.Find("Wall_Entrance");
            if (entrance == null) return;

            // Find objects fog wall and door panel
            var fog = entrance.Find("Room_Door_Fog");
            var doorPanel = entrance.Find("Door");

            // Identify the boss room and the adjacent room (otherRoom)
            var roomList = roomsWithEdge.ToList();
            var bossRoom = roomList.FirstOrDefault(r => r.Type == RoomType.Boss);
            var otherRoom = roomList.FirstOrDefault(r => r != bossRoom);

            if (bossRoom != null && otherRoom != null)
            {
                // Determine the direction from boss room to the other room
                var dir = (otherRoom.Center.ToVector3() - bossRoom.Center.ToVector3()).normalized;
            
                // Check if the entrance is facing the wrong direction
                var alignment = Vector3.Dot(dir, entrance.forward);

                // Rotate 180° if the entrance is facing the wrong direction
                if (alignment > 0f)
                    entrance.Rotate(0, 180f, 0);
            }

            // Enable fog wall
            if (fog != null)
                fog.gameObject.SetActive(true);

            // Make the door visually narrower
            if (doorPanel != null)
            {
                var scale = doorPanel.localScale;
                scale.z = 0.75f;
                doorPanel.localScale = scale;
            }
        }
    
        /// <summary>
        /// Create a wall segment from the prefab and scale and rotate it between two points
        /// </summary>
        /// <param name="start">The beginning of the wall</param>
        /// <param name="end">The end of the wall</param>
        /// <param name="scale">Local X scale of the segment.</param>
        private void CreateWallSegment(Vector3 start, Vector3 end, float scale)
        {
            // Instantiate the wall as a child
            var cube = Instantiate(wall, transform);

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
            var startIndex = _rng.Next(_dungeonGraph.Rooms.Count);
            var startRoom = _dungeonGraph.Rooms[startIndex];
            startRoom.Type    = RoomType.Start;
            startRoom.Visited = true;

            // Boss room
            var bossRoom = _dungeonGraph.GetFarthestRoomFrom(startRoom);
            bossRoom.Type = RoomType.Boss;

            // Put all the remaining rooms that are still “Normal” into a list
            var normals = _dungeonGraph.Rooms
                .Where(r => r.Type == RoomType.Normal)
                .ToList();

            // Minigame rooms
            for (var i = 0; i < 2 && normals.Count > 0; i++)
            {
                var idx = _rng.Next(normals.Count);
                normals[idx].Type = RoomType.MiniGame;
                normals.RemoveAt(idx);
            }

            // Item rooms
            var itemCount = Mathf.Max(1, (int)(0.2f * _dungeonGraph.Rooms.Count));
            for (var i = 0; i < itemCount && normals.Count > 0; i++)
            {
                var idx = _rng.Next(normals.Count);
                normals[idx].Type = RoomType.Item;
                normals.RemoveAt(idx);
            }

            // Enemy rooms
            var enemyCount = Mathf.Max(1, (int)(0.2f * _dungeonGraph.Rooms.Count));
            for (var i = 0; i < enemyCount && normals.Count > 0; i++)
            {
                var idx = _rng.Next(normals.Count);
                normals[idx].Type = RoomType.Enemy;
                normals.RemoveAt(idx);
            }

            // Rest are normal rooms
        }
    
        /// <summary>
        /// Computes the door path that must be traversable (start → some item room),
        /// but avoids passing through the Boss room.
        /// Also stores the target item room id in _forcedItemRoomId.
        /// </summary>
        private void ComputeForcedDoorPairs()
        {
            _forcedDoorPairs.Clear();
            ForcedItemRoomId = -1;

            var start     = _dungeonGraph.GetStartRoom();
            var itemRooms = _dungeonGraph.GetAllItemRooms();
            if (start == null || itemRooms == null || itemRooms.Count == 0) return;
        
            List<Room> bestPath = null;
            Room bestTarget = null;

            foreach (var item in itemRooms)
            {
                var path = _dungeonGraph.FindShortestPathWithoutBossRoom(start, item);
                if (path == null) continue;

                if (bestPath == null || path.Count < bestPath.Count)
                {
                    bestPath = path;
                    bestTarget = item;
                }
            }

            // Fallback
            if (bestPath == null)
            {
                var firstItem = itemRooms.FirstOrDefault();
                if (firstItem == null) return;

                var fallback = _dungeonGraph.FindShortestPath(start, firstItem);
                if (fallback == null) return;

                for (int i = 0; i < fallback.Count - 1; i++)
                    _forcedDoorPairs.Add(GetPairKey(fallback[i].ID, fallback[i + 1].ID));

                ForcedItemRoomId = firstItem.ID;
                return;
            }

            for (var i = 0; i < bestPath.Count - 1; i++)
                _forcedDoorPairs.Add(GetPairKey(bestPath[i].ID, bestPath[i + 1].ID));
        
            ForcedItemRoomId = bestTarget?.ID ?? bestPath.Last().ID;
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
            if (start == null) return;
        
            Room item = null;
            if (ForcedItemRoomId >= 0)
                item = _dungeonGraph.GetRoomByID(ForcedItemRoomId);
            else
                item = _dungeonGraph.GetAllItemRooms().FirstOrDefault();

            if (item == null) return;

            var pathRooms = _dungeonGraph.FindShortestPathWithoutBossRoom(start, item)
                            ?? _dungeonGraph.FindShortestPath(start, item);
            if (pathRooms == null) return;

            _debugShortestItemPath = pathRooms.Select(r => r.Center).ToList();
        }
        #endregion
    
        #region Voronoi & Delaunay Computation
        /// <summary>
        /// Generates random points with a minimum spacing radius.
        /// </summary>
        /// <param name="count">Number of points to generate.</param>
        /// <param name="radius">Minimum distance between points.</param>
        /// <param name="dungeonSize">Area limit (0…size for X and Y).</param>
        /// <returns>List of generated points.</returns>
        private List<Point> GeneratePoints(int count, float radius, float dungeonSize)
        {
            var points = new List<Point>();
            var maxAttempts = 1000;

            while (points.Count < count && maxAttempts > 0)
            {
                var margin = 2f;
                var x = margin + (float)_rng.NextDouble() * (dungeonSize - 2 * margin);
                var y = margin + (float)_rng.NextDouble() * (dungeonSize - 2 * margin);
                var newPoint = new Point(x, y);
                var isValid = true;

                foreach (var existing in points)
                {
                    if (Point.GetDistance(existing, newPoint) < radius)
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
        private List<Triangle> BowyerWatson(List<Point> points)
        {
            // List of triangles forming the Delaunay-Triangulation
            var triangulation = new List<Triangle>();

            // A triangle that is big enough so all points fall within its bound
            var superTriangle = new Triangle(new Point(-size,-size/2), new Point(size/2,size*2.5f), new Point(2*size,-size/2));
            triangulation.Add(superTriangle);
        
            // ONLY FOR DEBUGGING
            _debugSuperTriangle = superTriangle;
            // ONLY FOR DEBUGGING

            // Add each point after point
            foreach (var point in points)
            {
                // List of invalid triangles in the triangulation
                var badTriangles = new List<Triangle>();
            
                // Find all invalid triangles in the triangulation
                foreach (var triangle in triangulation)
                {
                    if (triangle.IsInCircumcircle(point))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                var polygon = new List<Edge>();

                // Find boundary polygon for generation of new triangles
                foreach (var triangle in badTriangles)
                {
                    foreach (var edge in triangle.Edges)
                    {
                        var count = 0;
                        foreach (var other in badTriangles)
                        {
                            if (Edge.Equals(edge, other.Edges[0]))
                            {
                                count++;
                            }
                            else if (Edge.Equals(edge, other.Edges[1]))
                            {
                                count++;
                            }
                            else if (Edge.Equals(edge, other.Edges[2]))
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
                    var newTriangle = new Triangle(edge.A,edge.B,point);
                    triangulation.Add(newTriangle);
                }
            }

            // Remove all triangles which contain a point of the super triangle
            triangulation.RemoveAll(t => t.ContainsPoints(superTriangle.Points));

            return triangulation;
        }
    
        /// <summary>
        /// This Method generates the Voronoi-Diagram by drawing the bisectors of every triangle, joining matching edges and extending the border ones
        /// </summary>
        /// <param name="triangulation">A valid Delaunay-Triangulation</param>
        /// <returns>The List of Voronoi Edges</returns>
        private List<Edge> GenerateVoronoi(List<Triangle> triangulation)
        {
            var bisectors = new List<Edge>();
            var toRemove = new List<int>();
            var voronoi = new List<Edge>();
        
            // ONLY FOR DEBUGGING
            _debugBisectors = new List<Edge>();
            _debugVoronoiIntersections = new List<Point>();
            // ONLY FOR DEBUGGING

            // Generate three edges from every circumcenter to the edge of the triangle
            foreach (var triangle in triangulation)
            {
                var center = triangle.GetCircumcircle().Center;
            
                // Only Centers within the map
                if (center.X < 0 || center.X > size || center.Y < 0 || center.Y > size)
                    continue;
            
                // Add bisectors to a list for later use
                foreach (var edge in triangle.Edges)
                {
                    var eTmp = new Edge(center, new Point(((edge.A.X + edge.B.X) / 2), ((edge.A.Y + edge.B.Y) / 2)));
                    eTmp.Room1 = edge.A;
                    eTmp.Room2 = edge.B;
                    bisectors.Add(eTmp);
                
                    // ONLY FOR DEBUGGING
                    _debugBisectors.Add(new Edge(center, new Point(((edge.A.X + edge.B.X) / 2), ((edge.A.Y + edge.B.Y) / 2))));
                    // ONLY FOR DEBUGGING
                }
            }

            var edgeCount = bisectors.Count;

            // Connect two bisectors that meet to one big edge
            for (var i = 0; i < edgeCount - 1; i++)
            {
                for (var j = i + 1; j < edgeCount; j++)
                {
                    if (Point.Equals(bisectors[i].B, bisectors[j].B))
                    {
                        var longEdge = new Edge(bisectors[i].A, bisectors[j].A);
                        longEdge.Room1 = bisectors[i].Room1;
                        longEdge.Room2 = bisectors[i].Room2;
                        longEdge.GiveID();
                        voronoi.Add(longEdge);
                        toRemove.Add(i);
                        toRemove.Add(j);
                    }
                }
            }

            // Prevent out of bounds exception
            var offset = 0;
            toRemove.Sort();

            // Remove all bisectors that have been connected
            foreach (var count in toRemove)
            {
                bisectors.RemoveAt(count - offset);
                offset++;
            }

            // For each remaining bisector, so every bisector that goes to the outside of the dungeon
            foreach (var e in bisectors)
            {
                // Finde das eine Dreieck, dem dieser Bisektor entstammt
                Triangle owner = null;
                foreach (var tri in triangulation)
                {
                    if (Point.Equals(tri.GetCircumcircle().Center, e.A))
                    {
                        owner = tri;
                        break;
                    }
                }
                if (owner == null) continue;

                // direction bisector
                var dir = new Vector2(e.B.X - e.A.X, e.B.Y - e.A.Y);
                dir.Normalize();

                // Checkpoints slightly forward and backward
                var forwardTest = new Point(e.B.X + dir.x * 0.01f, e.B.Y + dir.y * 0.01f);
                var forwardInside = !PointOutTriangle(forwardTest, owner.Points[0], owner.Points[1], owner.Points[2]);

                // If the forward point is inside,
                // we have to extend backwards outwards, otherwise forwards.
                var rayDir = forwardInside ? new Vector2(-dir.x, -dir.y) : dir;

                // Build a ray from the circumcenter far outwards
                var far = new Point(e.A.X + rayDir.x * size * 2f, e.A.Y + rayDir.y * size * 2f);
                var ray = new Edge(e.A, far);

                // Intersection with the map boundary
                var inter = FindIntersectionWithMapBoundary(ray, size);
                _debugVoronoiIntersections.Add(inter);

                // New Voronoi-Edge
                var newEdge = new Edge(e.A, inter);
                newEdge.Room1 = e.Room1;
                newEdge.Room2 = e.Room2;
                newEdge.GiveID();
                voronoi.Add(newEdge);
            }
            // Return the voronoi diagram as a list of Edges
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
        /// <returns>Weather the point is outside(true) or inside (false)</returns>
        private bool PointOutTriangle(Point pt, Point v1, Point v2, Point v3)
        {
            var d1 = Sign(pt, v1, v2);
            var d2 = Sign(pt, v2, v3);
            var d3 = Sign(pt, v3, v1);

            var hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            var hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        
            return (hasNeg && hasPos);
        }
    
        /// <summary>
        /// Computes the sign of the area for three points (used in triangle check)
        /// </summary>
        /// <param name="p1">Point A</param>
        /// <param name="p2">Point B</param>
        /// <param name="p3">Point C</param>
        /// <returns>The sign of the area</returns>
        private float Sign(Point p1, Point p2, Point p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }
    
        /// <summary>
        /// Finds the intersection point of an edge with the map boundary
        /// </summary>
        /// <param name="edge">The edge</param>
        /// <param name="dungeonSize">The dungeon size</param>
        /// <returns>The intersection Point</returns>
        private Point FindIntersectionWithMapBoundary(Edge edge, float dungeonSize)
        {
            var dx = edge.B.X - edge.A.X;
            var dy = edge.B.Y - edge.A.Y;

            var vertical = Mathf.Abs(dx) < 1e-5f;
            var horizontal = Mathf.Abs(dy) < 1e-5f;

            var candidates = new List<Point>();

            if (vertical)
            {
                candidates.Add(new Point(edge.B.X, 0));
                candidates.Add(new Point(edge.B.X, dungeonSize));
            }
            else if (horizontal)
            {
                candidates.Add(new Point(0, edge.B.Y));
                candidates.Add(new Point(dungeonSize, edge.B.Y));
            }
            else
            {
                var m = dy / dx;
                var b = edge.B.Y - m * edge.B.X;

                candidates.Add(new Point(0, b));                                    // left
                candidates.Add(new Point(dungeonSize, m * dungeonSize + b));        // right
                candidates.Add(new Point((0 - b) / m, 0));                          // bottom
                candidates.Add(new Point((dungeonSize - b) / m, dungeonSize));      // top
            }

            // Only valid points in Range [0, size]
            candidates.RemoveAll(p => p.X < 0 || p.X > dungeonSize || p.Y < 0 || p.Y > dungeonSize);

            return CheckPoints(candidates, edge.B);
        }
    
        /// <summary>
        /// Find the closest Point from a list to another point
        /// </summary>
        /// <param name="points">The list of points we want to get the closest from</param>
        /// <param name="reference">The point we want to get closest to</param>
        /// <returns>the point from the list that is closest to reference</returns>
        private Point CheckPoints(List<Point> points, Point reference)
        {
            var result= new Point(0,0);
            float minDistance = 10000;
            foreach (var p in points)
            {
                var distx = reference.X - p.X;
                var disty = reference.Y - p.Y;
                var distance = Mathf.Sqrt((distx*distx)+(disty*disty));
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
            foreach (var room in _dungeonGraph.Rooms)
            {
                room.Neighbors.RemoveAll(other => !HasTraversableEdge(room, other));
            }
        }
    
        /// <summary>
        /// Checks if there is a traversable Voronoi edge between two rooms
        /// </summary>
        /// <param name="a">Room A</param>
        /// <param name="b">Room B</param>
        /// <returns>True if there is a Voronoi edge between rooms that is at least the door length.</returns>
        private bool HasTraversableEdge(Room a, Room b)
        {
            foreach (var e in _debugVoronoi)
            {
                var matches = (Point.Equals(e.Room1, a.Center) && Point.Equals(e.Room2, b.Center)) ||
                              (Point.Equals(e.Room1, b.Center) && Point.Equals(e.Room2, a.Center));

                if (!matches) continue;

                var len = Point.GetDistance(e.A, e.B);
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

            Debug.Log($"[BossRoom] ID: {bossRoom.ID} has {bossRoom.Doors.Count} door(s)");
            Debug.Log($"[BossRoom] ID: {bossRoom.ID} has {bossRoom.Neighbors.Count} neighbor(s)");

            var neighborIds = string.Join(", ", bossRoom.Neighbors.Select(n => n.ID));
            Debug.Log($"[BossRoom] Neighbor IDs: {neighborIds}");

            foreach (int edgeId in bossRoom.Doors)
            {
                var isInstantiated = _dungeonGraph.IDDoorDict.ContainsKey(edgeId) ? "✅ Door exists in dict" : "❌ Not in dict";
                Debug.Log($"[BossRoom] Door EdgeID: {edgeId} — {isInstantiated}");
            }

            Debug.Log("[BossRoom] All wall edges:");
            foreach (var wallId in bossRoom.Walls)
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
        
                for(var i = 0; i < _debugPoints.Count; i++)
                {
                    var p = _debugPoints[i];
                    var pos = new Vector3(p.X, 0.5f, p.Y);
                    Gizmos.DrawCube(pos, Vector3.one);
                
                    var style = new GUIStyle();
                    style.normal.textColor = Color.white;
                    var room = _dungeonGraph.GetRoomByID(i);
                    var labelColor = Color.white;

                    switch (room.Type)
                    {
                        case RoomType.Boss:     labelColor = Color.red;     break;
                        case RoomType.MiniGame: labelColor = Color.cyan;    break;
                        case RoomType.Item:     labelColor = Color.yellow;  break;
                        case RoomType.Enemy:    labelColor = Color.magenta; break;
                        case RoomType.Start:    labelColor = Color.blue;    break;
                        case RoomType.Normal:   labelColor = Color.white;   break;
                    };

                    //Set the color before drawing the label
                    style.normal.textColor = labelColor;
                    Handles.Label(pos + Vector3.up * 0.5f, $"\nPoint {i} : {room.Type}\nVisited: {room.Visited}", style);
                }
            }
        
            // CENTER (WHITE)
            if (showCenters && _debugCenters != null)
            {
                Gizmos.color = Color.white;
                foreach (var center in _debugCenters)
                {
                    var pos = new Vector3(center.X, 0.5f, center.Y);
                    Gizmos.DrawSphere(pos, 0.5f);
                }
            }
        
            // DELAUNAY TRIANGULATION (BLUE)
            if (showTriangles && _debugTriangles != null)
            {
                Gizmos.color = Color.blue;
                foreach (var triangle in _debugTriangles)
                {
                    DrawLine(triangle.Points[0], triangle.Points[1]);
                    DrawLine(triangle.Points[1], triangle.Points[2]);
                    DrawLine(triangle.Points[2], triangle.Points[0]);
                }
            }
        
            // SHOW SHORTEST ITEM PATH (ORANGE)
            if (showShortestItemPath && _debugShortestItemPath != null && _debugShortestItemPath.Count > 1)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                for (var i = 0; i < _debugShortestItemPath.Count - 1; i++)
                    DrawLine(_debugShortestItemPath[i], _debugShortestItemPath[i + 1]);
            }
        
            // DOOR + EDGE ID LABELS (WHITE)
            if (showDoorEdgeID && _dungeonGraph != null)
            {
                var doorStyle = new GUIStyle();
                doorStyle.normal.textColor = Color.white;
            
                foreach (var kvp in _dungeonGraph.IDDoorDict)
                {
                    var id = kvp.Key;
                    var doorObj = kvp.Value;
                    if (doorObj == null) continue;

                    var labelPos = doorObj.transform.position + Vector3.up * 0.5f;
                    Handles.Label(labelPos, $"Edge: {id}\nDoor: {id}", doorStyle);
                }
            }
        
            // ALL EDGE ID THAT DO NOT HAVE A DOOR (ORANGE) 
            if (showDoorEdgeID && _debugVoronoi != null && _dungeonGraph != null)
            {
                var wallStyle = new GUIStyle();
                wallStyle.normal.textColor = new Color(1f, 0.5f, 0f);
    
                foreach (var edge in _debugVoronoi)
                {
                    if (!_dungeonGraph.IDDoorDict.ContainsKey(edge.Id))
                    {
                        var labelPos = new Vector3((edge.A.X + edge.B.X) * 0.5f, 0.5f, (edge.A.Y + edge.B.Y) * 0.5f);
                        Handles.Label(labelPos, $"Edge: {edge.Id}\n(Wall)", wallStyle);
                    }
                }
            }
        
            // VORONOI TRIANGULATION (YELLOW)
            if (showVoronoi && _debugVoronoi != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var edge in _debugVoronoi)
                {
                    DrawLine(edge.A, edge.B);
                }
            }
        
            // BISEKTOREN (MAGENTA)
            if (showBisectors && _debugBisectors != null)
            {
                Gizmos.color = Color.magenta;
                foreach (var edge in _debugBisectors)
                {
                    DrawLine(edge.A, edge.B);
                }
            }

            // INTERSECTIONS (GRAY)
            if (showVoronoiIntersections && _debugVoronoiIntersections != null)
            {
                Gizmos.color = Color.gray;
                foreach (var point in _debugVoronoiIntersections)
                {
                    var pos = new Vector3(point.X, 0.5f, point.Y);
                    Gizmos.DrawSphere(pos, 0.5f);
                }
            }
        
            // SUPERTRIANGLE (BLACK)
            if (showSuperTriangle && _debugSuperTriangle != null)
            {
                Gizmos.color = Color.black;
                DrawLine(_debugSuperTriangle.Points[0], _debugSuperTriangle.Points[1]);
                DrawLine(_debugSuperTriangle.Points[1], _debugSuperTriangle.Points[2]);
                DrawLine(_debugSuperTriangle.Points[2], _debugSuperTriangle.Points[0]);
            }
#endif
        }
    
        /// <summary>
        /// Draws a line between two points in Gizmos
        /// </summary>
        /// <param name="a">Point A</param>
        /// <param name="b">Point B</param>
        private void DrawLine(Point a, Point b)
        {
            var start = new Vector3(a.X, 0.5f, a.Y);
            var end = new Vector3(b.X, 0.5f, b.Y);
            Gizmos.DrawLine(start, end);
        }
        #endregion
    }
}
