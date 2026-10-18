using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CGLab4.Models
{
    public class LabPolygon
    {
        // Список вершин. Порядок важен для Участника 3 (геометрия)
        public List<Point> Vertices { get; set; } = new List<Point>();

        public bool IsSelected { get; set; } = false; // Для будущих преобразований


        public Point Center
        {
            get
            {
                if (Vertices.Count == 0) return new Point(0, 0);
                double sumX = Vertices.Sum(v => v.X);
                double sumY = Vertices.Sum(v => v.Y);
                return new Point(sumX / Vertices.Count, sumY / Vertices.Count);
            }
        }

        // Метод отрисовки, адаптирующийся под 1, 2 или >=3 вершин
        public IEnumerable<UIElement> CreateVisuals()
        {
            var visuals = new List<UIElement>();
            var brush = IsSelected ? Brushes.LightBlue : Brushes.Transparent;
            var pen = new Pen(IsSelected ? Brushes.Red : Brushes.Black, 2);

            if (Vertices.Count == 1)
            {
                visuals.Add(new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Red,
                    Margin = new Thickness(Vertices[0].X - 3, Vertices[0].Y - 3, 0, 0)
                });
            }
            else if (Vertices.Count == 2)
            {
                visuals.Add(new Line
                {
                    X1 = Vertices[0].X,
                    Y1 = Vertices[0].Y,
                    X2 = Vertices[1].X,
                    Y2 = Vertices[1].Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                });
                visuals.Add(new Ellipse { Width = 4, Height = 4, Fill = Brushes.Red, Margin = new Thickness(Vertices[0].X - 2, Vertices[0].Y - 2, 0, 0) });
                visuals.Add(new Ellipse { Width = 4, Height = 4, Fill = Brushes.Red, Margin = new Thickness(Vertices[1].X - 2, Vertices[1].Y - 2, 0, 0) });
            }
            else
            {
                var shape = new System.Windows.Shapes.Polygon
                {
                    Points = new PointCollection(Vertices),
                    Fill = brush,
                    Stroke = pen.Brush,
                    StrokeThickness = 2
                };
                visuals.Add(shape);
            }

            return visuals;
        }
        public void ApplyTransformation(Matrix3x3 matrix)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] = matrix.Transform(Vertices[i]);
            }
        }

        // TODO (Участник 2): Добавить сюда методы ApplyRotation, ApplyScale с использованием матриц 3x3
        // TODO (Участник 3): Добавить сюда или в расширения методы CheckPointInside, GetEdgeIntersection
    }
}