using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    /// <summary>
    /// Simple triangle helper built from three points.
    /// Stores its edges/points and provides circle/intersection checks.
    /// </summary>
    public class Triangle
    {
        /// <summary>
        /// The three edges that form this triangle.
        /// </summary>
        public List<Edge> Edges;
        
        /// <summary>
        /// The three corner points of this triangle, in order A, B, C.
        /// </summary>
        public List<Point> Points;
        
        /// <summary>
        /// Creates a triangle from three points (A, B, C) and builds its three edges.
        /// </summary>
        /// <param name="a">Corner A.</param>
        /// <param name="b">Corner B.</param>
        /// <param name="c">Corner C.</param>
        public Triangle(Point a, Point b, Point c)
        {
            Edges = new List<Edge>();
            Points = new List<Point>();

            Points.Add(a);
            Points.Add(b);
            Points.Add(c);

            Edges.Add(new Edge(a,b));
            Edges.Add(new Edge(b,c));
            Edges.Add(new Edge(c,a));
        }

        /// <summary>
        /// Checks whether a given point lies inside this triangle's circumcircle.
        /// </summary>
        /// <param name="p">Point to test.</param>
        /// <returns>True if inside, otherwise false.</returns>
        public bool IsInCircumcircle(Point p) 
        {
            var circumcircle = GetCircumcircle();
            if((p.X - circumcircle.Center.X) * (p.X - circumcircle.Center.X) + (p.Y - circumcircle.Center.Y) * (p.Y - circumcircle.Center.Y) < (circumcircle.Radius * circumcircle.Radius))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether any point in the given list is one of this triangle's corners.
        /// </summary>
        /// <param name="points">The List of points to check</param>
        /// <returns>True if at least one point is a corner of the triangle</returns>
        public bool ContainsPoints(List<Point> points)
        {
            // Iterate through all points and return true if any is part of this triangle
            foreach (var point in points)
            {
                if (Point.Equals(point, Points[0]))
                {
                    return true;
                }
                else if (Point.Equals(point, Points[1]))
                {
                    return true;
                }
                else if (Point.Equals(point, Points[2]))
                {
                    return true;
                }
            }

            // If no point is found, return false
            return false; 
        }

        /// <summary>
        /// Checks whether the given edge is one of this triangle's edges.
        /// </summary>
        /// <param name="edge">Edge to test.</param>
        /// <returns>True if it matches any of the three edges.</returns>
        public bool ContainsEdge(Edge edge)
        {
            if (Edge.Equals(edge, Edges[0]))
            {
                return true;
            }
            else if (Edge.Equals(edge, Edges[1]))
            {
                return true;
            }
            else if (Edge.Equals(edge, Edges[2]))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Calculates this triangle's circumcircle (center and radius).
        /// </summary>
        /// <returns>The circumcircle.</returns>
        public Circle GetCircumcircle()
        {
            // Circum center
            var d = 2 * (Points[0].X * (Points[1].Y - Points[2].Y) + Points[1].X * (Points[2].Y - Points[0].Y) + Points[2].X * (Points[0].Y - Points[1].Y));
            var ux = (
                (Points[0].X * Points[0].X + Points[0].Y * Points[0].Y) * (Points[1].Y - Points[2].Y)
              + (Points[1].X * Points[1].X + Points[1].Y * Points[1].Y) * (Points[2].Y - Points[0].Y)
              + (Points[2].X * Points[2].X + Points[2].Y * Points[2].Y) * (Points[0].Y - Points[1].Y)
            ) / d;
            var uy = (
                (Points[0].X * Points[0].X + Points[0].Y * Points[0].Y) * (Points[2].X - Points[1].X)
              + (Points[1].X * Points[1].X + Points[1].Y * Points[1].Y) * (Points[0].X - Points[2].X)
              + (Points[2].X * Points[2].X + Points[2].Y * Points[2].Y) * (Points[1].X - Points[0].X)
            ) / d;
            var center = new Point(ux, uy);
            var radius = Mathf.Sqrt((Points[0].X-center.X)* (Points[0].X - center.X) + (Points[0].Y-center.Y)* (Points[0].Y - center.Y));
            return new Circle(center,radius);
        }

        /// <summary>
        /// Calculates the intersection point of two lines (if it exists), the method is mainly used to get the center of the circumcircle (and there should always be an intersection of two bisectors of the triangle edges)
        /// </summary>
        /// <param name="e1">First edge</param>
        /// <param name="e2">Second edge</param>
        /// <returns>The intersection point, or null if both are vertical.</returns>
        public static Point GetIntersection(Edge e1, Edge e2)
        {
            var isVert1 = false;
            var isVert2 = false;

            var m1 = 0f;
            var b1 = 0f;
            var m2 = 0f;
            var b2 = 0f;

            // Try to get function parameters of e_1
            try
            {
                m1 = (e1.B.Y - e1.A.Y) / (e1.B.X - e1.A.X);
                b1 = (e1.A.Y - m1 * e1.A.X);
            }
            catch (DivideByZeroException)
            {
                // Catch if edge is vertical (division by 0 for m)
                isVert1 = true;
            }

            // Try to get function parameters of e_2
            try
            {
                m2 = (e2.B.Y - e2.A.Y) / (e2.B.X - e2.A.X);
                b2 = (e2.A.Y - m2 * e2.A.X);
            }
            catch (DivideByZeroException)
            {
                // Catch if edge is vertical (division by 0 for m)
                isVert2 = true;
            }

            // This should be impossible
            if (isVert1 && isVert2)
            {
                return null;
            }

            if (isVert1)
            {
                // Return point with x of the vertical line and y with the x plugged in the equation of the none vertical line
                return new Point(e1.A.X, m2 * e1.A.X + b2);
            }
            else if (isVert2)
            {
                // Return point with x of the vertical line and y with the x plugged in the equation of the none vertical line
                return new Point(e2.A.X, m1 * e2.A.X + b1);
            }
            else
            {
                // Set both equations equal and rearranged to calculate x and use x to calculate y
                var x = (b2 - b1) / (m1 - m2);
                var y = m1 * x + b1;
                
                return new Point(x, y);
            }
        }

        /// <summary>
        /// Logs a readable string with the triangle's three corner coordinates.
        /// </summary>
        public void PrintTriangle()
        {

            var triString = "Triangle: (A:(" + Points[0].X + "," + Points[0].Y +
                            "),B:(" + Points[1].X + "," + Points[1].Y +
                            "),C:(" + Points[2].X + "," + Points[2].Y + "))\n";
            Debug.Log(triString);
        }
    }

    /// <summary>
    /// 2D point with X and Y values plus small helpers.
    /// </summary>
    public class Point
    {
        /// <summary>
        /// X coordinate.
        /// Y coordinate.
        /// </summary>
        public float X, Y;

        /// <summary>
        /// Creates a point with given X and Y.
        /// </summary>
        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Checks if two points have the same coordinates.
        /// </summary>
        public static bool Equals(Point p1, Point p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        /// <summary>
        /// Returns the distance between two points.
        /// </summary>
        public static float GetDistance(Point p1, Point p2)
        {
            return Mathf.Sqrt(((p1.X-p2.X) * (p1.X - p2.X)) + ((p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }
        
        /// <summary>
        /// Converts this point to a Vector3, using a given Y height (default 0).
        /// </summary>
        public Vector3 ToVector3(float y = 0f)
        {
            return new Vector3(X, y, Y);
        }
        
        /// <summary>
        /// Logs this point as "(X;Y)".
        /// </summary>
        public void PrintPoint()
        {
            Debug.Log("("+X+";"+Y+")");
        }
    }

    /// <summary>
    /// Line segment between two points, with helpers for ids and intersections.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Start point.
        /// </summary>
        public Point A;

        /// <summary>
        /// End point.
        /// </summary>
        public Point B;
        
        /// <summary>
        /// Static counter used to assign unique ids.
        /// </summary>
        private static int _nextId;
        
        /// <summary>
        /// Unique id for this edge (assigned via <see cref="GiveID"/>).
        /// </summary>
        public int Id;

        /// <summary>
        /// Optional reference to a room center on one side of the edge.
        /// </summary>
        public Point Room1;
        
        /// <summary>
        /// Optional reference to a room center on the other side of the edge.
        /// </summary>
        public Point Room2;

        /// <summary>
        /// Creates an edge from point A to point B.
        /// </summary>
        public Edge(Point a, Point b)
        {
            A = a;
            B = b;
        }

        /// <summary>
        /// Assigns a unique id to this edge.
        /// </summary>
        public void GiveID()
        {
            Id = _nextId++;
        }

        /// <summary>
        /// Checks if two edges are equal (same two points, any order).
        /// </summary>
        public static bool Equals(Edge e1, Edge e2)
        {
            if (Point.Equals(e1.A,e2.A) && Point.Equals(e1.B,e2.B))
            {
                return true;
            }
            else if (Point.Equals(e1.A, e2.B) && Point.Equals(e1.B, e2.A))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Checks if this edge intersects another edge.
        /// </summary>
        public bool Intersect(Edge e2)
        {
            return DoLinesIntersect(A, B, e2.A, e2.B);
        }
        
        /// <summary>
        /// Line intersection test for two segments p1–p2 and q1–q2.
        /// </summary>
        private static bool DoLinesIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            float Orientation(Point a, Point b, Point c)
            {
                return (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);
            }

            bool OnSegment(Point p, Point q, Point r)
            {
                return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                       q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
            }

            var o1 = Orientation(p1, p2, q1);
            var o2 = Orientation(p1, p2, q2);
            var o3 = Orientation(q1, q2, p1);
            var o4 = Orientation(q1, q2, p2);

            // General case
            if (o1 * o2 < 0 && o3 * o4 < 0)
                return true;

            // Special cases (colinear points)
            if (o1 == 0 && OnSegment(p1, q1, p2)) return true;
            if (o2 == 0 && OnSegment(p1, q2, p2)) return true;
            if (o3 == 0 && OnSegment(q1, p1, q2)) return true;
            if (o4 == 0 && OnSegment(q1, p2, q2)) return true;

            return false;
        }
    }

    /// <summary>
    /// Circle with a center point and radius.
    /// </summary>
    public class Circle
    {
        /// <summary>
        /// Center of the circle.
        /// </summary>
        public Point Center;
        
        /// <summary>
        /// Radius of the circle.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Creates a circle with the given center and radius.
        /// </summary>
        public Circle(Point center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
