using System;
using System.Collections.Generic;
using System.Windows;

namespace CGLab4.Models
{
    public enum PointPolygonLocation
    {
        Outside,
        Boundary,
        Inside
    }

    public enum PointEdgeSide
    {
        Left,
        Right,
        OnLine
    }

    /// <summary>
    /// Геометрические проверки для отрезков и простых полигонов.
    /// Координаты WPF имеют направление Y вниз, поэтому положительный
    /// векторный поворот соответствует правой стороне нарисованного ребра.
    /// </summary>
    public static class Geometry2D
    {
        private const double Epsilon = 1e-8;

        public static PointPolygonLocation ClassifyPointInPolygon(
            IReadOnlyList<Point> vertices,
            Point point)
        {
            if (vertices.Count == 0)
            {
                return PointPolygonLocation.Outside;
            }

            bool inside = false;
            for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
            {
                Point a = vertices[j];
                Point b = vertices[i];

                if (IsPointOnSegment(point, a, b))
                {
                    return PointPolygonLocation.Boundary;
                }

                if ((a.Y > point.Y) != (b.Y > point.Y))
                {
                    double crossingX = a.X +
                        (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                    if (point.X < crossingX)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside ? PointPolygonLocation.Inside : PointPolygonLocation.Outside;
        }

        public static bool IsConvex(IReadOnlyList<Point> vertices)
        {
            if (vertices.Count < 4)
            {
                return true;
            }

            int turnDirection = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                Point a = vertices[i];
                Point b = vertices[(i + 1) % vertices.Count];
                Point c = vertices[(i + 2) % vertices.Count];
                double cross = Cross(b.X - a.X, b.Y - a.Y, c.X - b.X, c.Y - b.Y);

                if (Math.Abs(cross) <= Epsilon)
                {
                    continue;
                }

                int direction = Math.Sign(cross);
                if (turnDirection != 0 && direction != turnDirection)
                {
                    return false;
                }

                turnDirection = direction;
            }

            return true;
        }

        public static PointEdgeSide ClassifyPointRelativeToEdge(Point edgeStart, Point edgeEnd, Point point)
        {
            double cross = Cross(
                edgeEnd.X - edgeStart.X,
                edgeEnd.Y - edgeStart.Y,
                point.X - edgeStart.X,
                point.Y - edgeStart.Y);

            if (Math.Abs(cross) <= Epsilon)
            {
                return PointEdgeSide.OnLine;
            }

            // На WPF Canvas ось Y направлена вниз: положительный cross виден справа.
            return cross > 0 ? PointEdgeSide.Right : PointEdgeSide.Left;
        }

        public static bool TryGetSegmentIntersection(
            Point firstStart,
            Point firstEnd,
            Point secondStart,
            Point secondEnd,
            out Point intersection)
        {
            intersection = new Point();

            double rx = firstEnd.X - firstStart.X;
            double ry = firstEnd.Y - firstStart.Y;
            double sx = secondEnd.X - secondStart.X;
            double sy = secondEnd.Y - secondStart.Y;
            double firstLengthSquared = rx * rx + ry * ry;
            double secondLengthSquared = sx * sx + sy * sy;

            bool firstIsPoint = firstLengthSquared <= Epsilon * Epsilon;
            bool secondIsPoint = secondLengthSquared <= Epsilon * Epsilon;

            if (firstIsPoint && secondIsPoint)
            {
                if (DistanceSquared(firstStart, secondStart) <= Epsilon * Epsilon)
                {
                    intersection = firstStart;
                    return true;
                }

                return false;
            }

            if (firstIsPoint)
            {
                if (IsPointOnSegment(firstStart, secondStart, secondEnd))
                {
                    intersection = firstStart;
                    return true;
                }

                return false;
            }

            if (secondIsPoint)
            {
                if (IsPointOnSegment(secondStart, firstStart, firstEnd))
                {
                    intersection = secondStart;
                    return true;
                }

                return false;
            }

            double qpx = secondStart.X - firstStart.X;
            double qpy = secondStart.Y - firstStart.Y;
            double denominator = Cross(rx, ry, sx, sy);
            double parallelTolerance = Epsilon * Math.Sqrt(firstLengthSquared * secondLengthSquared);

            if (Math.Abs(denominator) <= parallelTolerance)
            {
                double collinearCross = Cross(qpx, qpy, rx, ry);
                double collinearTolerance = Epsilon * Math.Sqrt(firstLengthSquared);
                if (Math.Abs(collinearCross) > collinearTolerance)
                {
                    return false;
                }

                // Для наложившихся коллинеарных отрезков возвращаем одну общую точку.
                if (IsPointOnSegment(firstStart, secondStart, secondEnd))
                {
                    intersection = firstStart;
                    return true;
                }
                if (IsPointOnSegment(firstEnd, secondStart, secondEnd))
                {
                    intersection = firstEnd;
                    return true;
                }
                if (IsPointOnSegment(secondStart, firstStart, firstEnd))
                {
                    intersection = secondStart;
                    return true;
                }
                if (IsPointOnSegment(secondEnd, firstStart, firstEnd))
                {
                    intersection = secondEnd;
                    return true;
                }

                return false;
            }

            double t = Cross(qpx, qpy, sx, sy) / denominator;
            double u = Cross(qpx, qpy, rx, ry) / denominator;
            if (t < -Epsilon || t > 1 + Epsilon || u < -Epsilon || u > 1 + Epsilon)
            {
                return false;
            }

            intersection = new Point(firstStart.X + t * rx, firstStart.Y + t * ry);
            return true;
        }

        private static bool IsPointOnSegment(Point point, Point start, Point end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double lengthSquared = dx * dx + dy * dy;

            if (lengthSquared <= Epsilon * Epsilon)
            {
                return DistanceSquared(point, start) <= Epsilon * Epsilon;
            }

            double cross = Cross(dx, dy, point.X - start.X, point.Y - start.Y);
            double distanceTolerance = Epsilon * Math.Sqrt(lengthSquared);
            if (Math.Abs(cross) > distanceTolerance)
            {
                return false;
            }

            double dot = (point.X - start.X) * dx + (point.Y - start.Y) * dy;
            return dot >= -Epsilon && dot <= lengthSquared + Epsilon;
        }

        private static double Cross(double ax, double ay, double bx, double by)
        {
            return ax * by - ay * bx;
        }

        private static double DistanceSquared(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }
}
