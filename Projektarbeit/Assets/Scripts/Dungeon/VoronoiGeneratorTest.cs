using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VoronoiGeneratorTest : MonoBehaviour
{
    // Prefab used for the walls of the dungeon
    [SerializeField]
    public GameObject wallsPrefab;
    // Prefab used for the pillars on the vertexes
    [SerializeField]
    public GameObject pillar;
    // Prefab used for things placed on the Voronoi-Sites (currently unused)
    [SerializeField]
    public GameObject pointsPrefab;
    // Prefab for the floor of the dungeon
    [SerializeField]
    public GameObject floorPrefab;
    //Width and height of the dungeon
    [SerializeField]
    public int width = 10;
    [SerializeField]
    public int height = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        List<Point> points = GenerateRandomPoints(5, width, height);

        // Calculate the Delaunay-Triangulation
        List<Triangle> triangles = BowyerWatson(points);

        // Add circumcenters of every triangle, which are the voronoi points, to a list
        List<Point> circlePoints = new List<Point>();
        foreach (var triangle in triangles)
        {
            circlePoints.Add(triangle.GetCircumcenter());
        }

        // Place Pillars on the edges to hide wall-clipping
        Instantiate(pillar, new Vector3(0, 0, 0), Quaternion.identity, transform);
        Instantiate(pillar, new Vector3(width, 0, 0), Quaternion.identity, transform);
        Instantiate(pillar, new Vector3(width, 0, height), Quaternion.identity, transform);
        Instantiate(pillar, new Vector3(0, 0, height), Quaternion.identity, transform);

        // Set green markers to indicate the given voronoi sites
        foreach (var point in points)
        {
            Instantiate(pointsPrefab, new Vector3(point.X, 0, point.Y), Quaternion.identity, transform);
        }

        // Create edges of the voronoi diagramm using the wallPrefab
        List<Edge> voronoiEdges = ComputeVoronoiDiagram(triangles);
        foreach (var edge in voronoiEdges)
        {
            Point start = edge.A;
            Point end = edge.B;

            // Create Pillars on the start and end of each wall (results in double placement)
            Instantiate(pillar, new Vector3(start.X, 0, start.Y), Quaternion.identity, transform);
            Instantiate(pillar, new Vector3(end.X, 0, end.Y), Quaternion.identity, transform);

            // Create Walls
            CreateWall(new Vector3(start.X, 0, start.Y), new Vector3(end.X, 0, end.Y));
        }

        // Place the floor of the dungeon
        for (int j = 0; j < height/2; j++)
        {
            for (int i = 0; i < width / 2; i++)
            {
                GameObject floor = Instantiate(floorPrefab, new Vector3((i * 2) + 1, 0, (j * 2) + 1), Quaternion.identity, transform);
            }
        }
        
        // Create outside walls of the dungeon
        CreateWall(new Vector3(0, 0, 0), new Vector3(width, 0, 0));
        CreateWall(new Vector3(width, 0, 0), new Vector3(width, 0, height));
        CreateWall(new Vector3(width, 0, height), new Vector3(0, 0, height));
        CreateWall(new Vector3(0, 0, height), new Vector3(0, 0, 0));

    }

    /// <summary>
    /// Checks wether a point is within a rectangle bounded by (0,0) and (width,height)
    /// </summary>
    /// <param name="p">The point which should get checked</param>
    /// <returns>True if point is outside/False if inside</returns>
    bool isOut(Point p)
    {
        if ((p.X < 0 || p.X > width) || (p.Y < 0 || p.Y > height))
        {
            return true;
        }
        else return false;
    }

    void CreateWall(Vector3 start, Vector3 end)
    {
        int widthOfPrefab = 2;
        int numberOfSegments = (int)Vector3.Distance(start, end)/widthOfPrefab;
        
        if (numberOfSegments < 1)
        {
            numberOfSegments = 1;
        }
        float scaleOfSegment = (Vector3.Distance(start, end) / widthOfPrefab) / numberOfSegments;
        Vector3 segmentStep = (end - start) / numberOfSegments;

        for (int i = 0; i < numberOfSegments; i++)
        {
            CreateWallSegment(start + (i * segmentStep), start + ((i + 1)* segmentStep),scaleOfSegment);
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
        GameObject cube = Instantiate(wallsPrefab,transform);

        // Position the location of the prefab to the middle between start and end
        cube.transform.position = (start + end) / 2;

        // Scale the wall accordingly
        cube.transform.localScale = new Vector3(scale, 1f, 1f);

        // Rotate the wall correctly
        cube.transform.rotation = Quaternion.FromToRotation(Vector3.right, end - start);
    }

    static List<Point> GenerateRandomPoints(int count, int width, int height)
    {
        System.Random rand = new System.Random();
        List<Point> points = new List<Point>();
        for (int i = 0; i < count; i++)
        {
            points.Add(new Point(rand.Next(width), rand.Next(height)));
        }
        return points;
    }

    static List<Triangle> BowyerWatson(List<Point> points)
    {
        List<Triangle> triangles = new List<Triangle>();

        // Super-Triangle erstellen, das alle Punkte enth�lt
        int max = 1000;
        Triangle superTriangle = new Triangle(
            new Point(-max, -max),
            new Point(max, -max),
            new Point(0, max));
        triangles.Add(superTriangle);

        foreach (var point in points)
        {
            List<Triangle> badTriangles = triangles.Where(t => t.IsPointInsideCircumcircle(point)).ToList();
            List<Edge> polygon = new List<Edge>();

            foreach (var triangle in badTriangles)
            {
                foreach (var edge in triangle.GetEdges())
                {
                    if (!badTriangles.Any(t => t != triangle && t.HasEdge(edge)))
                    {
                        polygon.Add(edge);
                    }
                }
            }

            triangles.RemoveAll(t => badTriangles.Contains(t));
            foreach (var edge in polygon)
            {
                triangles.Add(new Triangle(edge.A, edge.B, point));
            }
        }

        triangles.RemoveAll(t => t.ContainsVertex(superTriangle.A) || t.ContainsVertex(superTriangle.B) || t.ContainsVertex(superTriangle.C));
        return triangles;
    }

    static List<Edge> ComputeVoronoiDiagram(List<Triangle> triangles)
    {
        List<Edge> voronoiEdges = new List<Edge>();
        Dictionary<Edge, List<Point>> edgeToCircumcenters = new Dictionary<Edge, List<Point>>();

        foreach (var triangle in triangles)
        {
            Point circumcenter = triangle.GetCircumcenter();
            foreach (var edge in triangle.GetEdges())
            {
                if (!edgeToCircumcenters.ContainsKey(edge))
                {
                    edgeToCircumcenters[edge] = new List<Point>();
                }
                edgeToCircumcenters[edge].Add(circumcenter);
            }
        }

        foreach (var entry in edgeToCircumcenters)
        {
            List<Point> centers = entry.Value;
            Edge delaunayEdge = entry.Key;
            if (centers.Count == 2)
            {
                // Normale Kante zwischen zwei Voronoi-Zellen
                voronoiEdges.Add(new Edge(centers[0], centers[1]));
            }
            else if (centers.Count == 1)
            {
                // Unendliche Kante: erweitern entlang des Normalenvektors
                Point circumcenter = centers[0];
                Vector2 a = new Vector2(delaunayEdge.A.X, delaunayEdge.A.Y);
                Vector2 b = new Vector2(delaunayEdge.B.X, delaunayEdge.B.Y);
                Vector2 edgeDir = (b - a).normalized;
                Vector2 normal = new Vector2(-edgeDir.y, edgeDir.x); // 90� Rotation

                Vector2 cc = new Vector2(circumcenter.X, circumcenter.Y);
                Vector2 far = cc + normal * 1000f; // "Unendlich weit" hinaus (oder ggf. Dungeon-Grenze)

                voronoiEdges.Add(new Edge(circumcenter, new Point(far.x, far.y)));
            }
        }

        return voronoiEdges;
    }

    public class Point
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Point(float x, float y) { X = x; Y = y; }
    }

    public class Triangle
    {
        public Point A { get; }
        public Point B { get; }
        public Point C { get; }
        public Triangle(Point a, Point b, Point c) { A = a; B = b; C = c; }

        public bool IsPointInsideCircumcircle(Point p)
        {
            double ax = A.X - p.X, ay = A.Y - p.Y;
            double bx = B.X - p.X, by = B.Y - p.Y;
            double cx = C.X - p.X, cy = C.Y - p.Y;
            double det = (ax * ax + ay * ay) * (bx * cy - by * cx)
                       - (bx * bx + by * by) * (ax * cy - ay * cx)
                       + (cx * cx + cy * cy) * (ax * by - ay * bx);
            return det > 0;
        }

        public List<Edge> GetEdges() => new List<Edge> { new Edge(A, B), new Edge(B, C), new Edge(C, A) };
        public bool HasEdge(Edge e) => GetEdges().Any(edge => edge.Equals(e));
        public bool ContainsVertex(Point p) => A == p || B == p || C == p;

        public Point GetCircumcenter()
        {
            double d = 2 * (A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y));
            double ux = ((A.X * A.X + A.Y * A.Y) * (B.Y - C.Y) + (B.X * B.X + B.Y * B.Y) * (C.Y - A.Y) + (C.X * C.X + C.Y * C.Y) * (A.Y - B.Y)) / d;
            double uy = ((A.X * A.X + A.Y * A.Y) * (C.X - B.X) + (B.X * B.X + B.Y * B.Y) * (A.X - C.X) + (C.X * C.X + C.Y * C.Y) * (B.X - A.X)) / d;
            return new Point((int)ux, (int)uy);
        }
    }

    public class Edge
    {
        public Point A { get; }
        public Point B { get; }
        public Edge(Point a, Point b) { A = a; B = b; }
        public override bool Equals(object obj) => obj is Edge e && ((A == e.A && B == e.B) || (A == e.B && B == e.A));
        public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
    }
}
