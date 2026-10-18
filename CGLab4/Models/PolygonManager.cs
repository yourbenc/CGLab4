using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CGLab4.Models
{
    public class PolygonManager
    {
        public List<LabPolygon> Polygons { get; private set; } = new List<LabPolygon>();
        public LabPolygon SelectedPolygon { get; private set; }

        private LabPolygon _currentPolygon;
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

        /// <summary>
        /// Выбор полигона по точке клика.
        /// TODO (Участник 3): Заменить проверку Bounding Box на точный алгоритм "Point in Polygon"
        /// </summary>
        public void SelectPolygon(Point p)
        {
            DeselectAll();

            foreach (var poly in Polygons)
            {
                if (poly.Vertices.Count == 0) continue;

                double minX = poly.Vertices.Min(v => v.X);
                double maxX = poly.Vertices.Max(v => v.X);
                double minY = poly.Vertices.Min(v => v.Y);
                double maxY = poly.Vertices.Max(v => v.Y);

                if (p.X >= minX - 5 && p.X <= maxX + 5 && p.Y >= minY - 5 && p.Y <= maxY + 5)
                {
                    SelectedPolygon = poly;
                    poly.IsSelected = true;
                    break; // Выбираем первый попавшийся (верхний)
                }
            }
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