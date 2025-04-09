using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;

public class Test : MonoBehaviour
{
    [SerializeField]
    public GameObject prefab;
    [SerializeField]
    public GameObject corner;
    [SerializeField]
    public GameObject pointsPrefab;
    //Width and height of the dungeon
    [SerializeField]
    public int width = 10;
    [SerializeField]
    public int height = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        List<Point> points = GenerateRandomPoints(5, width, height);

        // Add three points to make sure the voronoi diagramm reaches to the edges of the rectangle
        points.Add(new Point(-width, -height));
        points.Add(new Point(width * 2, -height));
        points.Add(new Point(width / 2, height * 2));

        // Berechne Delaunay-Triangulation
        List<Triangle> triangles = BowyerWatson(points);

        // Add circumcenters of every triangle to a list
        List<Point> circlePoints = new List<Point>();
        foreach (var triangle in triangles) 
        {
            circlePoints.Add(triangle.GetCircumcenter());    
        }

        // Set red markers to indicate the given rectangle
        Instantiate(corner, new Vector3(0, 0, 0), Quaternion.identity, transform);
        Instantiate(corner, new Vector3(width, 0, 0), Quaternion.identity, transform);
        Instantiate(corner, new Vector3(width, 0, height), Quaternion.identity, transform);
        Instantiate(corner, new Vector3(0, 0, height), Quaternion.identity, transform);

        // Set green markers to indicate the given voronoi sites
        foreach (var point in points)
        {
            Instantiate(pointsPrefab, new Vector3(point.X, 0, point.Y), Quaternion.identity, transform);
        }

        // create edges of the voronoi diagramm using a gray cube that gets rescaled
        List<Edge> voronoiEdges = ComputeVoronoiDiagram(triangles);
        foreach (var edge in voronoiEdges)
        {
            Point start = edge.A;
            Point end = edge.B;

            bool isSwitched = false;

            if (start.X==end.X)
            {
                isSwitched = true;
                float tmp = start.X;
                start.X = start.Y;
                start.Y = tmp;
                tmp = end.X;
                end.X = end.Y;
                end.Y = tmp;
            }
            // Calculate line equation
            float m = (end.Y-start.Y)/(end.X-start.X);
            float b = start.Y - m * start.X;

            // Intersections
            Point p0_Y = new Point(0,b);
            Point pW_Y = new Point(width, (m * width) + b);

            Point pX_0 = new Point((-b/m),0);
            Point pX_H = new Point((height - b) / m, height);

            // Sort out everything that does not intersect the rectangle
            if (isOut(p0_Y) && isOut(pW_Y) && isOut(pX_0) && isOut(pX_H))
            {
                continue;
            }

            if (isOut(start))
            {
                if (start.X<0) 
                {
                    start = p0_Y;
                }
                if (start.X > width)
                {
                    start = pW_Y;
                }
                if (start.Y<0)
                {
                    start = pX_0;
                }
                if (start.Y>height)
                {
                    start = pX_H;
                }
            }

            if (isOut(end))
            {
                if (end.X < 0)
                {
                    end = p0_Y;
                }
                if (end.X > width)
                {
                    end = pW_Y;
                }
                if (end.Y < 0)
                {
                    end = pX_0;
                }
                if (end.Y > height)
                {
                    end = pX_H;
                }
            }
            // Remove not meaningful intersecting edges
            if (start.X==end.X)
            {
                continue;
            }

            if (isSwitched)
            {
                float tmp = start.X;
                start.X = start.Y;
                start.Y = tmp;
                tmp = end.X;
                end.X = end.Y;
                end.Y = tmp;
            }
            // Create cube
            CreateScaledCube(new Vector3(start.X,0,start.Y), new Vector3(end.X,0,end.Y), 0.2f, 0.2f);
        }

        CreateScaledCube(new Vector3(0, 0, 0), new Vector3(width, 0, 0), 0.2f, 2f);
        CreateScaledCube(new Vector3(width, 0, 0), new Vector3(width, 0, height), 0.2f, 2f);
        CreateScaledCube(new Vector3(width, 0, height), new Vector3(0, 0, height), 0.2f, 2f);
        CreateScaledCube(new Vector3(0, 0, height), new Vector3(0, 0, 0), 0.2f, 2f);

    }

    bool isOut(Point p)
    {
        if ((p.X < 0 || p.X > width) || (p.Y < 0 || p.Y > height))
        {
            return true;
        }
        else return false;
    }

    void CreateScaledCube(Vector3 start, Vector3 end,float width,float height)
    {
        // Erstelle den Würfel
        GameObject cube = Instantiate(prefab, transform);

        // Setze die Position in die Mitte zwischen den Punkten
        cube.transform.position = (start + end) / 2;

        // Berechne die Distanz zwischen den Punkten
        float distance = Vector3.Distance(start, end);

        // Setze die Skalierung des Würfels
        cube.transform.localScale = new Vector3(distance, height, width); // Passt die Skalierung nur auf der X-Achse an

        // Rotiert den Würfel, sodass er sich zwischen den beiden Punkten ausrichtet
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

        // Super-Triangle erstellen, das alle Punkte enthält
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

        foreach (var entry in edgeToCircumcenters.Values)
        {
            if (entry.Count == 2)
            {
                voronoiEdges.Add(new Edge(entry[0], entry[1]));
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
