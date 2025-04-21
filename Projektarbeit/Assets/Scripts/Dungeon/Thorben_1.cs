#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

public class Thorben_1 : MonoBehaviour
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
    
    [Header("Dungeon Settings")]
    [SerializeField]
    private float size = 40;
    [SerializeField]
    private int numPoints = 5;
    [SerializeField]
    private int seed;
    
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
            _debugCenters.Add(triangle.getCircumcircle().center);
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

    void CreateWall(Vector3 start, Vector3 end)
    {
        int widthOfPrefab = 2;
        int numberOfSegments = (int)Vector3.Distance(start, end) / widthOfPrefab;

        if (numberOfSegments < 1)
        {
            numberOfSegments = 1;
        }
        float scaleOfSegment = (Vector3.Distance(start, end) / widthOfPrefab) / numberOfSegments;
        Vector3 segmentStep = (end - start) / numberOfSegments;

        for (int i = 0; i < numberOfSegments; i++)
        {
            CreateWallSegment(start + (i * segmentStep), start + ((i + 1) * segmentStep), scaleOfSegment);
        }
    }

    /// <summary>
    /// Create a wall from the prefab and scale and rotate it between two points
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

        foreach (Triangle triangle in triangulation)
        {
            // Generate three edges from every circumcenter to the edge of the triangle
            Point center = triangle.getCircumcircle().center;
            foreach (Edge edge in triangle.edges)
            {
                bisectors.Add(new Edge(center, new Point(((edge.A.x + edge.B.x) / 2), ((edge.A.y + edge.B.y) / 2))));
                
                // ONLY FOR DEBUGGING
                _debugBisectors.Add(new Edge(center, new Point(((edge.A.x + edge.B.x) / 2), ((edge.A.y + edge.B.y) / 2))));
                // ONLY FOR DEBUGGING
            }
        }

        int edgeCount = bisectors.Count;

        for (int i = 0; i < edgeCount-1; i++)
        {
            for (int j = i+1; j < edgeCount; j++)
            {
                if (Point.equals(bisectors[i].B, bisectors[j].B))
                {
                    Edge longEdge = new Edge(bisectors[i].A, bisectors[j].A);
                    voronoi.Add(longEdge);
                    toRemove.Add(i);
                    toRemove.Add(j);
                }
            }
        }

        int offset = 0;
        toRemove.Sort();

        foreach (int count in toRemove)
        {

            bisectors.RemoveAt(count - offset);
            offset++;
        }

        foreach (Edge e in bisectors)
        {
            foreach (Triangle tri in triangulation)
            {
                Edge bisector_1 = new Edge(tri.getCircumcircle().center, new Point(((tri.edges[0].A.x + tri.edges[0].B.x) / 2), ((tri.edges[0].A.y + tri.edges[0].B.y) / 2)));
                Edge bisector_2 = new Edge(tri.getCircumcircle().center, new Point(((tri.edges[1].A.x + tri.edges[1].B.x) / 2), ((tri.edges[1].A.y + tri.edges[1].B.y) / 2)));
                Edge bisector_3 = new Edge(tri.getCircumcircle().center, new Point(((tri.edges[2].A.x + tri.edges[2].B.x) / 2), ((tri.edges[2].A.y + tri.edges[2].B.y) / 2)));

                if (Edge.equals(e, bisector_1) || Edge.equals(e, bisector_2) || Edge.equals(e, bisector_3))
                {
                    float diffX = e.B.x - e.A.x;
                    float diffY = e.B.y - e.A.y;

                    Vector2 dir = new Vector2(diffX, diffY);
                    dir.Normalize();
                    Vector2 shortend = new Vector2(dir[0] * 0.01f, dir[1] * 0.01f);
                    
                    if (!PointOutTriangle(new Point(e.B.x + shortend[0], e.B.y + shortend[1]), tri.points[0], tri.points[1], tri.points[2]))
                    {
                        Point inter = FindIntersectionWithMapBoundary(e, size);
                        _debugVoronoiIntersections.Add(inter);
                        Edge newEdge = new Edge(e.A, inter);
                        voronoi.Add(newEdge);
                    }
                    else if (!PointOutTriangle(new Point(e.B.x - shortend[0], e.B.y - shortend[1]), tri.points[0], tri.points[1], tri.points[2]))
                    {
                        Point inter = FindIntersectionWithMapBoundary(e, size);
                        _debugVoronoiIntersections.Add(inter);
                        Edge newEdge = new Edge(e.A, inter);
                        voronoi.Add(newEdge);
                    }

                }
            }
        }
        return voronoi;
    }
    
    public float sign(Point p1, Point p2, Point p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    public bool PointOutTriangle(Point pt, Point v1, Point v2, Point v3)
    {
        float d1, d2, d3;
        bool has_neg, has_pos;

        d1 = sign(pt, v1, v2);
        d2 = sign(pt, v2, v3);
        d3 = sign(pt, v3, v1);

        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        Debug.Log("Result: "+ !(has_neg && has_pos));
        return (has_neg && has_pos);
    }

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

    // Helper-Classes
    public class Triangle
    {
        public List<Edge> edges;
        public List<Point> points;

        public Triangle(Point A, Point B, Point C)
        {
            this.edges = new List<Edge>();
            this.points = new List<Point>();

            this.points.Add(A);
            this.points.Add(B);
            this.points.Add(C);

            this.edges.Add(new Edge(A,B));
            this.edges.Add(new Edge(B,C));
            this.edges.Add(new Edge(C,A));
        }

        /// <summary>
        /// This Method checks wether a given point is within the circumcircle of the triangle calling this method
        /// </summary>
        /// <param name="p">The point, which should be inside the circle</param>
        /// <returns>True if the point is inside, False else</returns>
        public bool isInCircumcircle(Point p) 
        {
            Circle circumcircle = getCircumcircle();
            if((p.x - circumcircle.center.x) * (p.x - circumcircle.center.x) + (p.y - circumcircle.center.y) * (p.y - circumcircle.center.y) < (circumcircle.radius * circumcircle.radius))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks wether any Point of a given set is a corner of the triangle calling this method
        /// </summary>
        /// <param name="points">The List of points to check</param>
        /// <returns>True if at least one point is a corner of the triangle</returns>
        public bool containsPoints(List<Point> points)
        {
            // Iterate through all points and return true if any is part of this triangle
            foreach (var point in points)
            {
                if (Point.equals(point, this.points[0]))
                {
                    return true;
                }
                else if (Point.equals(point, this.points[1]))
                {
                    return true;
                }
                else if (Point.equals(point, this.points[2]))
                {
                    return true;
                }
            }

            // If no point is found return false
            return false; 
        }

        /// <summary>
        /// Checks wether an edge is an edge of the calling triangle
        /// </summary>
        /// <param name="edge">the edge which should be checked</param>
        /// <returns>True if the edge is one of the triangle ones</returns>
        public bool containsEdge(Edge edge)
        {
            if (Edge.equals(edge, this.edges[0]))
            {
                return true;
            }
            else if (Edge.equals(edge, this.edges[1]))
            {
                return true;
            }
            else if (Edge.equals(edge, this.edges[2]))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Calculates the circumcircle of a triangle as represented as center and radius
        /// </summary>
        /// <returns>The circumcircle</returns>
        public Circle getCircumcircle()
        {
            // Circum center
            float d = 2 * (points[0].x * (points[1].y - points[2].y) + points[1].x * (points[2].y - points[0].y) + points[2].x * (points[0].y - points[1].y));
            float ux = (
                (points[0].x * points[0].x + points[0].y * points[0].y) * (points[1].y - points[2].y)
              + (points[1].x * points[1].x + points[1].y * points[1].y) * (points[2].y - points[0].y)
              + (points[2].x * points[2].x + points[2].y * points[2].y) * (points[0].y - points[1].y)
            ) / d;
            float uy = (
                (points[0].x * points[0].x + points[0].y * points[0].y) * (points[2].x - points[1].x)
              + (points[1].x * points[1].x + points[1].y * points[1].y) * (points[0].x - points[2].x)
              + (points[2].x * points[2].x + points[2].y * points[2].y) * (points[1].x - points[0].x)
            ) / d;
            Point center = new Point(ux, uy);
            float radius = Mathf.Sqrt((points[0].x-center.x)* (points[0].x - center.x) + (points[0].y-center.y)* (points[0].y - center.y));
            return new Circle(center,radius);
        }

        /// <summary>
        /// Calculates the intersection point of two lines (if it exists), the method is mainly used to get the center of the circumcircle (and there should always be an intersection of two bisectors of the triangle edges)
        /// </summary>
        /// <param name="e_1">First edge</param>
        /// <param name="e_2">Second edge</param>
        /// <returns>The intersection point of e_1 and e_2</returns>
        public static Point getIntersection(Edge e_1, Edge e_2)
        {
            bool isVert_1 = false;
            bool isVert_2 = false;

            float m_1 = 0f;
            float b_1 = 0f;
            float m_2 = 0f;
            float b_2 = 0f;

            // Try to get function parameters of e_1
            try
            {
                m_1 = (e_1.B.y - e_1.A.y) / (e_1.B.x - e_1.A.x);
                b_1 = (e_1.A.y - m_1 * e_1.A.x);
            }
            catch (DivideByZeroException)
            {
                // Catch if edge is vertikal (division by 0 for m)
                isVert_1 = true;
            }

            // Try to get function parameters of e_2
            try
            {
                m_2 = (e_2.B.y - e_2.A.y) / (e_2.B.x - e_2.A.x);
                b_2 = (e_2.A.y - m_2 * e_2.A.x);
            }
            catch (DivideByZeroException)
            {
                // Catch if edge is vertikal (division by 0 for m)
                isVert_2 = true;
            }

            // This should be impossible
            if (isVert_1 && isVert_2)
            {
                return null;
            }

            if (isVert_1)
            {
                // Return point with x of the vertikal line and y with the x plugged in the equation of the none vertikal line
                return new Point(e_1.A.x, m_2 * e_1.A.x + b_2);
            }
            else if (isVert_2)
            {
                // Return point with x of the vertikal line and y with the x plugged in the equation of the none vertikal line
                return new Point(e_2.A.x, m_1 * e_2.A.x + b_1);
            }
            else
            {
                // Set both equations equal and rearranged to calculate x and use x to calculate y
                float x = (b_2 - b_1) / (m_1 - m_2);
                float y = m_1 * x + b_1;
                
                return new Point(x, y);
            }
        }

        public void printTriangle()
        {

            String triString = "Triangle: (A:(" + this.points[0].x + "," + this.points[0].y +
                                       "),B:(" + this.points[1].x + "," + this.points[1].y +
                                       "),C:(" + this.points[2].x + "," + this.points[2].y + "))\n";
            Debug.Log(triString);
        }
    }

    public class Point
    {
        public float x, y;

        public Point(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool equals(Point p_1, Point p_2)
        {
            if (p_1.x == p_2.x && p_1.y == p_2.y)
            {
                return true;
            }
            else
            {
                return false; 
            }
        }

        public static float getDistance(Point p_1, Point p_2)
        {
            return Mathf.Sqrt(((p_1.x-p_2.x) * (p_1.x - p_2.x)) + ((p_1.y - p_2.y) * (p_1.y - p_2.y)));
        }
        public void printPoint()
        {
            Debug.Log("("+this.x+";"+this.y+")");
        }
    }

    public class Edge
    {
        public Point A, B;

        public Edge(Point A, Point B)
        {
            this.A = A;
            this.B = B;
        }

        public static bool equals(Edge e_1, Edge e_2)
        {
            if (Point.equals(e_1.A,e_2.A) && Point.equals(e_1.B,e_2.B))
            {
                return true;
            }
            else if (Point.equals(e_1.A, e_2.B) && Point.equals(e_1.B, e_2.A))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class Circle
    {
        public Point center;
        public float radius;

        public Circle(Point center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
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
                Handles.Label(pos + Vector3.up * 0.5f, $"\nPoint {i + 1}", style);
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
    
    private void DrawLine(Point a, Point b)
    {
        Vector3 start = new Vector3(a.x, 0.5f, a.y);
        Vector3 end = new Vector3(b.x, 0.5f, b.y);
        Gizmos.DrawLine(start, end);
    }
    #endregion ONLY FOR DEBUGGING
}
