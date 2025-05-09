#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Geometry;

public class VoronoiGenerator : MonoBehaviour
{
    [Header("Prefabs for Dungeon")]
    [SerializeField]
    private GameObject pillar;
    [SerializeField]
    private GameObject wall;
    [SerializeField]
    private GameObject floor;
    [SerializeField]
    private GameObject door;
    [SerializeField]
    private GameObject light;
    
    [Header("Dungeon Settings")]
    [SerializeField]
    private float size = 40;
    [SerializeField]
    private int numPoints = 5;
    [SerializeField]
    private int seed;
    
    DungeonGraph dungonGraph;
    
    // ONLY FOR DEBUGGING
    [Header("Gizmos Debugging")]
    [SerializeField] private bool showPoints = true;
    [SerializeField] private bool showCenters = true;
    [SerializeField] private bool showTriangles = true;
    [SerializeField] private bool showVoronoi = true;
    [SerializeField] private bool showBisectors = true;
    [SerializeField] private bool showVoronoiIntersections = true;
    [SerializeField] private bool showSuperTriangle = true;
    
    private List<Point> _debugPoints;
    private List<Triangle> _debugTriangles;
    private List<Edge> _debugVoronoi;
    private List<Point> _debugCenters;
    private List<Edge> _debugBisectors;
    private List<Point> _debugVoronoiIntersections;
    private Triangle _debugSuperTriangle;
    // ONLY FOR DEBUGGING

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        #region DEBUG VERSION

        _debugPoints = generatePoints(numPoints, 6.0f, size);
        _debugTriangles = BowyerWatson(_debugPoints);
        _debugVoronoi = generateVoronoi(_debugTriangles);
        _debugCenters = new List<Point>();
        foreach (Triangle triangle in _debugTriangles)
        {
            Point center = triangle.getCircumcircle().center;

            // only add valid centers (inside the map)
            if (center.x >= 0 && center.x <= size && center.y >= 0 && center.y <= size)
            {
                _debugCenters.Add(center);
            }
        }
        
        // Dungon graph
        List<Room> rooms = new List<Room>();
        dungonGraph = new DungeonGraph();
        for (int id = 0; id < _debugPoints.Count; id++)
        {
            Room room = new Room(id, _debugPoints[id]);
            rooms.Add(room);
        }
        
        // Assign the type of the rooms
        rooms = roomTypeAssignment(rooms);
        
        foreach (Room room in rooms)
        {
            room.AddNeighbors(rooms, GetNeighbors(room.id, room));
            dungonGraph.AddRoom(room);
        }
        
        buildDungeon();
        buildVoronoi(_debugVoronoi);
        placePillars(_debugTriangles); 
        #endregion DEBUG VERSION
    }
    
    public List<Point> generatePoints(int count, float radius, float size)
    {
        List<Point> points = new List<Point>();
        System.Random random = new System.Random(seed);
        int maxAttempts = 1000;

        while (points.Count < count && maxAttempts > 0)
        {
            float margin = 2f;
            float x = margin + (float)random.NextDouble() * (size - 2 * margin);
            float y = margin + (float)random.NextDouble() * (size - 2 * margin);
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
    /// Builds the dungeon walls and the dungeon floor
    /// </summary>
    public void buildDungeon()
    {
        // Place the floor of the dungeon
        for (int j = 0; j < size / 2; j++)
        {
            for (int i = 0; i < size / 2; i++)
            {
                GameObject floorObj = Instantiate(floor, new Vector3((i * 2) + 1, 0, (j * 2) + 1), Quaternion.identity, transform);
            }
        }
        // Create outside walls of the dungeon
        CreateWall(new Vector3(0, 0, 0), new Vector3(size, 0, 0));
        CreateWall(new Vector3(size, 0, 0), new Vector3(size, 0, size));
        CreateWall(new Vector3(size, 0, size), new Vector3(0, 0, size));
        CreateWall(new Vector3(0, 0, size), new Vector3(0, 0, 0));
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
    /// Builds only the segmented Walls for the voronoi-edges
    /// </summary>
    /// <param name="voronoi">The list of voronoi edges</param>
    public void buildVoronoi(List<Edge> voronoi)
    {
        foreach (Edge e in voronoi)
        {
            CreateWall(new Vector3(e.A.x,0,e.A.y),new Vector3(e.B.x,0,e.B.y));
        }
    }

    /// <summary>
    /// Create a wall using wall scaled segments
    /// </summary>
    /// <param name="start">Beginning of the wall</param>
    /// <param name="end">End of the wall</param>
    void CreateWall(Vector3 start, Vector3 end)
    {
        // Size of the prefab (in our case its 2 units wide)
        int widthOfPrefab = 2;

        int numberOfSegments = (int)Vector3.Distance(start, end) / widthOfPrefab;
        
        // Set number of segments to 1 if the wall is to short
        if (numberOfSegments < 1)
        {
            numberOfSegments = 1;
        }

        // The factor by which the segment is scaled
        float scaleOfSegment = (Vector3.Distance(start, end) / widthOfPrefab) / numberOfSegments;

        // A Vector to keep track of how far we have to place the centers of each segment apart
        Vector3 segmentStep = (end - start) / numberOfSegments;

        for (int i = 0; i < numberOfSegments; i++)
        {
            CreateWallSegment(start + (i * segmentStep), start + ((i + 1) * segmentStep), scaleOfSegment);
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

    List<Room> roomTypeAssignment(List<Room> rooms)
    {
        int total = rooms.Count;
        System.Random rand = new System.Random();

        // Shuffle indices to randomly assign types
        List<int> indices = Enumerable.Range(0, total).OrderBy(x => rand.Next()).ToList();

        // Assign 1 Start Room
        rooms[indices[0]].type = RoomType.Start;
        
        // Assign 1 Boss Room
        rooms[indices[1]].type = RoomType.Boss;

        // Assign 2 Minigame Rooms
        rooms[indices[2]].type = RoomType.MiniGame;
        rooms[indices[3]].type = RoomType.MiniGame;

        int itemRoomCount = (int)(0.2f * total);
        int enemyRoomCount = (int)(0.2f * total);

        // Assign Item Rooms
        for (int i = 3; i < 3 + itemRoomCount; i++)
        {
            rooms[indices[i]].type = RoomType.Item;
        }

        // Assign Enemy Rooms
        for (int i = 3 + itemRoomCount; i < 3 + itemRoomCount + enemyRoomCount; i++)
        {
            rooms[indices[i]].type = RoomType.Enemy;
        }
        return rooms;
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
                bisectors.Add(new Edge(center, new Point(((edge.A.x + edge.B.x) / 2), ((edge.A.y + edge.B.y) / 2))));
                
                // ONLY FOR DEBUGGING
                _debugBisectors.Add(new Edge(center, new Point(((edge.A.x + edge.B.x) / 2), ((edge.A.y + edge.B.y) / 2))));
                // ONLY FOR DEBUGGING
            }
        }

        int edgeCount = bisectors.Count;

        // Connect two bisectors that meet to one big edge
        for (int i = 0; i < edgeCount-1; i++)
        {
            for (int j = i+1; j < edgeCount; j++)
            {
                if (Point.equals(bisectors[i].B, bisectors[j].B))
                {
                    Edge longEdge = new Edge(bisectors[i].A, bisectors[j].A);
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
            foreach (Triangle tri in triangulation)
            {
                // Get every bisector of the triangle
                Edge bisector_1 = new Edge(tri.getCircumcircle().center, new Point(((tri.edges[0].A.x + tri.edges[0].B.x) / 2), ((tri.edges[0].A.y + tri.edges[0].B.y) / 2)));
                Edge bisector_2 = new Edge(tri.getCircumcircle().center, new Point(((tri.edges[1].A.x + tri.edges[1].B.x) / 2), ((tri.edges[1].A.y + tri.edges[1].B.y) / 2)));
                Edge bisector_3 = new Edge(tri.getCircumcircle().center, new Point(((tri.edges[2].A.x + tri.edges[2].B.x) / 2), ((tri.edges[2].A.y + tri.edges[2].B.y) / 2)));

                // Check if the bisector we want is in the triangle
                if (Edge.equals(e, bisector_1) || Edge.equals(e, bisector_2) || Edge.equals(e, bisector_3))
                {
                    // Direction vector of the bisector
                    float diffX = e.B.x - e.A.x;
                    float diffY = e.B.y - e.A.y;
                    Vector2 dir = new Vector2(diffX, diffY);

                    // Normalize and shorten the direction vector
                    dir.Normalize();
                    Vector2 shortend = new Vector2(dir[0] * 0.01f, dir[1] * 0.01f);
                    
                    // Now check a point a little bit before and after the intersection of the bisector and the triangle edge
                    // Then build the edge AWAY from the triangle ( if one point is inside of the triangle use the other)
                    if (!PointOutTriangle(new Point(e.B.x + shortend[0], e.B.y + shortend[1]), tri.points[0], tri.points[1], tri.points[2]))
                    {
                        Point inter = FindIntersectionWithMapBoundary(e, size);
                        _debugVoronoiIntersections.Add(inter);
                        Edge newEdge = new Edge(e.A, inter);
                        newEdge.giveID();
                        voronoi.Add(newEdge);
                    }
                    else if (!PointOutTriangle(new Point(e.B.x - shortend[0], e.B.y - shortend[1]), tri.points[0], tri.points[1], tri.points[2]))
                    {
                        Point inter = FindIntersectionWithMapBoundary(e, size);
                        _debugVoronoiIntersections.Add(inter);
                        Edge newEdge = new Edge(e.A, inter);
                        newEdge.giveID();
                        voronoi.Add(newEdge);
                    }

                }
            }
        }
        // Return the voronoi diagramm as a list of Edges
        return voronoi;
    }
    
    // Helper Method
    public float sign(Point p1, Point p2, Point p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    /// <summary>
    /// Checks if a Point is outside of a triangle
    /// </summary>
    /// <param name="pt">The point we want to know</param>
    /// <param name="v1">Point A</param>
    /// <param name="v2">Point B</param>
    /// <param name="v3">Point C</param>
    /// <returns>Wether the point is outside(true) or inside (false)</returns>
    public bool PointOutTriangle(Point pt, Point v1, Point v2, Point v3)
    {
        float d1, d2, d3;
        bool has_neg, has_pos;

        d1 = sign(pt, v1, v2);
        d2 = sign(pt, v2, v3);
        d3 = sign(pt, v3, v1);

        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        
        return (has_neg && has_pos);
    }

    /// <summary>
    /// Find the closest Point from a list to another point
    /// </summary>
    /// <param name="points">The list of points we want to get the closest from</param>
    /// <param name="reference">The point we want to get closest to</param>
    /// <returns>the point from the list that is closest to reference</returns>
    public Point checkPoints(List<Point> points, Point reference)
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
    
    public Point FindIntersectionWithMapBoundary(Edge edge, float size)
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
                Point current = t.points[i];
                if (Mathf.Approximately(current.x, target.x) && Mathf.Approximately(current.y, target.y))
                {
                    int next1 = (i + 1) % 3;
                    int next2 = (i + 2) % 3;

                    Point neighbor1 = t.points[next1];
                    Point neighbor2 = t.points[next2];
                    int index1 = _debugPoints.FindIndex(p =>
                        Mathf.Approximately(p.x, neighbor1.x) && Mathf.Approximately(p.y, neighbor1.y));
                    int index2 = _debugPoints.FindIndex(p =>
                        Mathf.Approximately(p.x, neighbor2.x) && Mathf.Approximately(p.y, neighbor2.y));

                    if (index1 != -1) neighbors.Add(index1);
                    if (index2 != -1) neighbors.Add(index2);

                    Edge edge1 = new Edge(target, neighbor1);
                    foreach (Edge edge in _debugVoronoi){
                        if (edge1.Intersect(edge))
                        {
                            room.AddWallEdge(edge.Id);
                        }
                    }
                    
                    Edge edge2 = new Edge(target, neighbor2);
                    foreach (Edge edge in _debugVoronoi){
                        if (edge2.Intersect(edge))
                        {
                            room.AddWallEdge(edge.Id);
                        }
                    }
                    
                    break; // Found the point in this triangle; no need to keep looping i
                }
            }
        }

        return neighbors;
    }
    
    
    #region ONLY FOR DEBUGGING
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
                Room room = dungonGraph.GetRoomByID(i);
                Color labelColor = Color.white;

                switch (room.type)
                {
                    case RoomType.Boss:
                        labelColor = Color.red;
                        break;
                    case RoomType.MiniGame:
                        labelColor = Color.cyan;
                        break;
                    case RoomType.Item:
                        labelColor = Color.yellow;
                        break;
                    case RoomType.Enemy:
                        labelColor = Color.magenta;
                        break;
                    case RoomType.Normal:
                        labelColor = Color.white;
                        break;
                    case RoomType.Start:
                        labelColor = Color.blue;
                        break;
                }

                //Set the color before drawing the label
                style.normal.textColor = labelColor;
                Handles.Label(pos + Vector3.up * 0.5f, $"\nPoint {i} : {room.type}", style);
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

        // VORONOI TRIANGULATION (YELLOW)
        if (showVoronoi && _debugVoronoi != null)
        {
            foreach (Edge edge in _debugVoronoi)
            {
                DrawLine(edge.A, edge.B);
                Handles.Label(new Vector3((edge.A.x + edge.B.x) / 2, 0, (edge.A.y + edge.B.y) / 2), $"\nEdge {edge.Id}");
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
    
    private void DrawLine(Point a, Point b)
    {
        Vector3 start = new Vector3(a.x, 0.5f, a.y);
        Vector3 end = new Vector3(b.x, 0.5f, b.y);
        Gizmos.DrawLine(start, end);
    }
    #endregion ONLY FOR DEBUGGING
}
