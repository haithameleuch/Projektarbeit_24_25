using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Geometry
{
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
        private static int _nextId = 0;
        public int Id;

        public Point Room1;
        public Point Room2;

        public Edge(Point A, Point B)
        {
            this.A = A;
            this.B = B;
        }

        public void giveID()
        {
            Id = _nextId++;
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
        public bool Intersect(Edge e2)
        {
            return DoLinesIntersect(this.A, this.B, e2.A, e2.B);
        }
        
        private static bool DoLinesIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            float orientation(Point a, Point b, Point c)
            {
                return (b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y);
            }

            bool OnSegment(Point p, Point q, Point r)
            {
                return q.x <= Math.Max(p.x, r.x) && q.x >= Math.Min(p.x, r.x) &&
                       q.y <= Math.Max(p.y, r.y) && q.y >= Math.Min(p.y, r.y);
            }

            float o1 = orientation(p1, p2, q1);
            float o2 = orientation(p1, p2, q2);
            float o3 = orientation(q1, q2, p1);
            float o4 = orientation(q1, q2, p2);

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
