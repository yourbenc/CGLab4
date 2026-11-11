using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CGLab4.Models
{
    public class PolygonManager
    {
        public List<LabPolygon> Polygons { get; private set; } = new List<LabPolygon>();
        public LabPolygon? SelectedPolygon { get; private set; }

        private LabPolygon? _currentPolygon;
        private bool _isDrawingMode;

        public bool IsDrawingMode => _isDrawingMode;

        public void StartDrawing()
        {
            _isDrawingMode = true;
            _currentPolygon = new LabPolygon();
            DeselectAll();
        }

        public void AddPoint(Point p)
        {
            if (_isDrawingMode && _currentPolygon != null)
            {
                _currentPolygon.Vertices.Add(p);
            }
        }

        public void FinishDrawing()
        {
            if (_isDrawingMode && _currentPolygon != null && _currentPolygon.Vertices.Count > 0)
            {
                Polygons.Add(_currentPolygon);
                _currentPolygon = null;
                _isDrawingMode = false;
            }
        }

        public void ClearScene()
        {
            Polygons.Clear();
            _currentPolygon = null;
            _isDrawingMode = false;
            SelectedPolygon = null;
        }

        /// <summary>Выбор фигуры по фактическому контуру или близости к точке/отрезку.</summary>
        public void SelectPolygon(Point p)
        {
            DeselectAll();

            for (int i = Polygons.Count - 1; i >= 0; i--)
            {
                LabPolygon poly = Polygons[i];
                if (IsHit(poly, p))
                {
                    SelectedPolygon = poly;
                    poly.IsSelected = true;
                    break;
                }
            }
        }

        private static bool IsHit(LabPolygon polygon, Point point)
        {
            const double tolerance = 5;
            IReadOnlyList<Point> vertices = polygon.Vertices;

            if (vertices.Count == 0)
            {
                return false;
            }

            if (vertices.Count >= 3)
            {
                return Geometry2D.ClassifyPointInPolygon(vertices, point) != PointPolygonLocation.Outside;
            }

            if (vertices.Count == 1)
            {
                return DistanceSquared(point, vertices[0]) <= tolerance * tolerance;
            }

            Point start = vertices[0];
            Point end = vertices[1];
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double lengthSquared = dx * dx + dy * dy;
            double t = lengthSquared == 0
                ? 0
                : ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            Point closest = new Point(start.X + t * dx, start.Y + t * dy);
            return DistanceSquared(point, closest) <= tolerance * tolerance;
        }

        private static double DistanceSquared(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private void DeselectAll()
        {
            SelectedPolygon = null;
            foreach (var poly in Polygons)
            {
                poly.IsSelected = false;
            }
        }

        public void Render(Canvas canvas)
        {
            canvas.Children.Clear();

            foreach (var poly in Polygons)
            {
                foreach (var visual in poly.CreateVisuals())
                {
                    canvas.Children.Add(visual);
                }
            }

            if (_isDrawingMode && _currentPolygon != null)
            {
                foreach (var visual in _currentPolygon.CreateVisuals())
                {
                    canvas.Children.Add(visual);
                }
            }
        }
    }
}
