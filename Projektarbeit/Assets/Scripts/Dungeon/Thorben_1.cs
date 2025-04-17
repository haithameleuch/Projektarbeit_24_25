using System;
using System.Collections.Generic;
using UnityEngine;

public class Thorben_1 : MonoBehaviour
{
    [SerializeField]
    float size = 40;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        List<Point> points = new List<Point>
        {
            new Point(10f, 10f),
            new Point(30f, 5f),
            new Point(10f, 30f),
            new Point(30f, 30f)
        };
        
        List<Triangle> triangulation = BowyerWatson(points);
        Debug.Log("Anzahl der Dreiecke: " + triangulation.Count);
        foreach (Triangle triangle in triangulation)
        {
            triangle.printTriangle();
        }
    }


    public void generateVoronoi(List<Triangle> triangulation)
    {

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

        // Add each point after point
        foreach (var point in points)
        {
            foreach (Triangle triangle in triangulation)
            {
                triangle.printTriangle();
            }
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
        static Point getIntersection(Edge e_1, Edge e_2)
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

    // Helper-Classes (maybe structs useful)

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
}
