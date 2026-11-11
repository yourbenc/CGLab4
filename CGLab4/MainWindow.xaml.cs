using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CGLab4.Models;

namespace CGLab4
{
    public partial class MainWindow : Window
    {
        private enum AnalysisMode
        {
            None,
            SegmentIntersection,
            PointContainment,
            EdgeSide
        }

        private readonly PolygonManager _manager = new PolygonManager();
        private AnalysisMode _analysisMode;
        private int _intersectionStep;
        private Point _firstEdgeStart;
        private Point _firstEdgeEnd;
        private Point _secondEdgeStart;
        private Line? _intersectionPreview;
        private int _sideEdgeStep;
        private Point _sideEdgeStart;
        private Point _sideEdgeEnd;

        public MainWindow()
        {
            InitializeComponent();
            UpdateStatus();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(DrawingCanvas);

            if (_analysisMode != AnalysisMode.None)
            {
                HandleAnalysisClick(clickPoint);
                return;
            }

            if (ChkSelectionMode.IsChecked == true)
            {
                _manager.SelectPolygon(clickPoint);
                _manager.Render(DrawingCanvas);
                UpdateStatus();
                return;
            }

            if (_manager.IsDrawingMode)
            {
                _manager.AddPoint(clickPoint);
                _manager.Render(DrawingCanvas);
            }
            else
            {
                MessageBox.Show("Сначала нажмите 'Новый полигон' или включите 'Режим выбора'.", "Инфо");
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_analysisMode == AnalysisMode.SegmentIntersection && _intersectionStep == 3)
            {
                UpdateIntersectionPreview(e.GetPosition(DrawingCanvas));
            }
        }

        private void HandleAnalysisClick(Point point)
        {
            switch (_analysisMode)
            {
                case AnalysisMode.SegmentIntersection:
                    HandleIntersectionClick(point);
                    break;
                case AnalysisMode.PointContainment:
                    HandlePointContainmentClick(point);
                    break;
                case AnalysisMode.EdgeSide:
                    HandleEdgeSideClick(point);
                    break;
            }
        }

        private void HandleIntersectionClick(Point point)
        {
            switch (_intersectionStep)
            {
                case 0:
                    _firstEdgeStart = point;
                    _intersectionStep = 1;
                    AddMarker(point, Brushes.RoyalBlue);
                    break;
                case 1:
                    _firstEdgeEnd = point;
                    AddOverlayLine(_firstEdgeStart, _firstEdgeEnd, Brushes.RoyalBlue, 2.5);
                    AddMarker(point, Brushes.RoyalBlue);
                    _intersectionStep = 2;
                    break;
                case 2:
                    _secondEdgeStart = point;
                    _intersectionStep = 3;
                    AddMarker(point, Brushes.DarkOrange);
                    UpdateIntersectionPreview(point);
                    break;
                case 3:
                    RemoveIntersectionPreview();
                    AddOverlayLine(_secondEdgeStart, point, Brushes.DarkOrange, 2.5);
                    AddMarker(point, Brushes.DarkOrange);

                    if (Geometry2D.TryGetSegmentIntersection(
                        _firstEdgeStart,
                        _firstEdgeEnd,
                        _secondEdgeStart,
                        point,
                        out Point intersection))
                    {
                        AddIntersectionMarker(intersection);
                        AppendAnalysisResult($"Рёбра пересекаются в точке {FormatPoint(intersection)}.");
                    }
                    else
                    {
                        AppendAnalysisResult("Рёбра не пересекаются.");
                    }

                    // Оставляем режим включённым: следующий клик начинает новую пару рёбер.
                    _intersectionStep = 0;
                    break;
            }

            UpdateStatus();
        }

        private void HandlePointContainmentClick(Point point)
        {
            LabPolygon? polygon = _manager.SelectedPolygon;
            if (polygon == null)
            {
                StatusText.Text = "Сначала включите режим выбора и выберите полигон, затем вернитесь к проверке точки.";
                return;
            }

            PointPolygonLocation location = Geometry2D.ClassifyPointInPolygon(polygon.Vertices, point);
            string shapeKind = Geometry2D.IsConvex(polygon.Vertices) ? "выпуклый" : "невыпуклый";
            string relation = location switch
            {
                PointPolygonLocation.Inside => "внутри",
                PointPolygonLocation.Boundary => "на границе",
                _ => "снаружи"
            };
            Brush markerBrush = location switch
            {
                PointPolygonLocation.Inside => Brushes.LimeGreen,
                PointPolygonLocation.Boundary => Brushes.DarkOrange,
                _ => Brushes.Crimson
            };

            AddMarker(point, markerBrush);
            string result = $"Точка {FormatPoint(point)} {relation} {shapeKind} полигона.";
            AppendAnalysisResult(result);
            UpdateStatus();
        }

        private void HandleEdgeSideClick(Point point)
        {
            if (_sideEdgeStep == 0)
            {
                _sideEdgeStart = point;
                _sideEdgeStep = 1;
                AddMarker(point, Brushes.MediumPurple);
                StatusText.Text = "Кликните конец ребра.";
                return;
            }

            if (_sideEdgeStep == 1)
            {
                if (point.Equals(_sideEdgeStart))
                {
                    StatusText.Text = "Начало и конец ребра должны отличаться. Кликните другую точку.";
                    return;
                }

                _sideEdgeEnd = point;
                _sideEdgeStep = 2;
                AddMarker(point, Brushes.MediumPurple);
                AddOverlayLine(_sideEdgeStart, _sideEdgeEnd, Brushes.MediumPurple, 2.5);
                StatusText.Text = "Ребро задано. Кликайте точки для классификации; кнопка «Задать новое ребро» сохраняет режим.";
                return;
            }

            PointEdgeSide side = Geometry2D.ClassifyPointRelativeToEdge(_sideEdgeStart, _sideEdgeEnd, point);
            string relation = side switch
            {
                PointEdgeSide.Left => "слева",
                PointEdgeSide.Right => "справа",
                _ => "на линии ребра"
            };
            Brush markerBrush = side switch
            {
                PointEdgeSide.Left => Brushes.DodgerBlue,
                PointEdgeSide.Right => Brushes.OrangeRed,
                _ => Brushes.DarkOrange
            };

            AddMarker(point, markerBrush);
            string result = side == PointEdgeSide.OnLine
                ? $"Точка {FormatPoint(point)} лежит на прямой ребра {FormatPoint(_sideEdgeStart)} → {FormatPoint(_sideEdgeEnd)}."
                : $"Точка {FormatPoint(point)} — {relation} от ребра {FormatPoint(_sideEdgeStart)} → {FormatPoint(_sideEdgeEnd)}.";
            AppendAnalysisResult(result);
            StatusText.Text = "Кликайте следующую точку или задайте новое ребро.";
        }

        private void BtnNewPolygon_Click(object sender, RoutedEventArgs e)
        {
            StopAnalysisMode();
            ChkSelectionMode.IsChecked = false;
            _manager.StartDrawing();
            UpdateStatus();
        }

        private void BtnFinishPolygon_Click(object sender, RoutedEventArgs e)
        {
            _manager.FinishDrawing();
            _manager.Render(DrawingCanvas);
            UpdateStatus();
        }

        private void BtnClearScene_Click(object sender, RoutedEventArgs e)
        {
            StopAnalysisMode();
            _manager.ClearScene();
            _manager.Render(DrawingCanvas);
            InteractionOverlay.Children.Clear();
            AnalysisResultsText.Clear();
            UpdateStatus();
        }

        private void BtnIntersectionMode_Click(object sender, RoutedEventArgs e)
        {
            StartAnalysisMode(AnalysisMode.SegmentIntersection);
        }

        private void BtnPointContainmentMode_Click(object sender, RoutedEventArgs e)
        {
            StartAnalysisMode(AnalysisMode.PointContainment);
        }

        private void BtnEdgeSideMode_Click(object sender, RoutedEventArgs e)
        {
            StartAnalysisMode(AnalysisMode.EdgeSide);
        }

        private void BtnNewSideEdge_Click(object sender, RoutedEventArgs e)
        {
            if (_analysisMode != AnalysisMode.EdgeSide)
            {
                StartAnalysisMode(AnalysisMode.EdgeSide);
            }
            else
            {
                _sideEdgeStep = 0;
                StatusText.Text = "Кликните начало нового ребра.";
            }
        }

        private void BtnStopAnalysisMode_Click(object sender, RoutedEventArgs e)
        {
            StopAnalysisMode();
            UpdateStatus();
        }

        private void StartAnalysisMode(AnalysisMode mode)
        {
            RemoveIntersectionPreview();
            _analysisMode = mode;
            _intersectionStep = 0;
            _sideEdgeStep = 0;
            ChkSelectionMode.IsChecked = false;
            UpdateStatus();
        }

        private void StopAnalysisMode()
        {
            _analysisMode = AnalysisMode.None;
            _intersectionStep = 0;
            _sideEdgeStep = 0;
            RemoveIntersectionPreview();
        }

        /// <summary>
        /// Смещение выбранного полигона матрицей 3x3.
        /// </summary>
        private void BtnTranslate_Click(object sender, RoutedEventArgs e)
        {
            LabPolygon? selectedPolygon = _manager.SelectedPolygon;
            if (selectedPolygon == null)
            {
                MessageBox.Show("Сначала включите 'Режим выбора' и кликните по полигону на поле!", "Внимание");
                return;
            }

            if (double.TryParse(TxtDx.Text, out double dx) && double.TryParse(TxtDy.Text, out double dy))
            {
                Matrix3x3 translationMatrix = new Matrix3x3(
                    1, 0, dx,
                    0, 1, dy,
                    0, 0, 1);

                selectedPolygon.ApplyTransformation(translationMatrix);
                _manager.Render(DrawingCanvas);
            }
            else
            {
                MessageBox.Show("Введите корректные числа для dx и dy", "Ошибка ввода");
            }
        }

        private void BtnRotatePoint_Click(object sender, RoutedEventArgs e)
        {
            LabPolygon? selectedPolygon = _manager.SelectedPolygon;
            if (selectedPolygon == null)
            {
                MessageBox.Show("Сначала выберите полигон!", "Внимание");
                return;
            }

            if (double.TryParse(TxtRotationAngle.Text, out double angle) &&
                double.TryParse(TxtRotationX.Text, out double x) &&
                double.TryParse(TxtRotationY.Text, out double y))
            {
                selectedPolygon.ApplyRotation(angle, new Point(x, y));
                _manager.Render(DrawingCanvas);
            }
            else
            {
                MessageBox.Show("Введите корректные значения угла и координат точки.", "Ошибка ввода");
            }
        }

        private void BtnRotateCenter_Click(object sender, RoutedEventArgs e)
        {
            LabPolygon? selectedPolygon = _manager.SelectedPolygon;
            if (selectedPolygon == null)
            {
                MessageBox.Show("Сначала выберите полигон!", "Внимание");
                return;
            }

            if (double.TryParse(TxtRotationAngle.Text, out double angle))
            {
                selectedPolygon.ApplyRotationAroundCenter(angle);
                _manager.Render(DrawingCanvas);
            }
            else
            {
                MessageBox.Show("Введите корректное значение угла.", "Ошибка ввода");
            }
        }

        private void BtnScalePoint_Click(object sender, RoutedEventArgs e)
        {
            LabPolygon? selectedPolygon = _manager.SelectedPolygon;
            if (selectedPolygon == null)
            {
                MessageBox.Show("Сначала выберите полигон!", "Внимание");
                return;
            }

            if (double.TryParse(TxtScaleX.Text, out double scaleX) &&
                double.TryParse(TxtScaleY.Text, out double scaleY) &&
                double.TryParse(TxtScalePointX.Text, out double x) &&
                double.TryParse(TxtScalePointY.Text, out double y))
            {
                selectedPolygon.ApplyScale(scaleX, scaleY, new Point(x, y));
                _manager.Render(DrawingCanvas);
            }
            else
            {
                MessageBox.Show("Введите корректные коэффициенты и координаты точки.", "Ошибка ввода");
            }
        }

        private void BtnScaleCenter_Click(object sender, RoutedEventArgs e)
        {
            LabPolygon? selectedPolygon = _manager.SelectedPolygon;
            if (selectedPolygon == null)
            {
                MessageBox.Show("Сначала выберите полигон!", "Внимание");
                return;
            }

            if (double.TryParse(TxtScaleX.Text, out double scaleX) &&
                double.TryParse(TxtScaleY.Text, out double scaleY))
            {
                selectedPolygon.ApplyScaleAroundCenter(scaleX, scaleY);
                _manager.Render(DrawingCanvas);
            }
            else
            {
                MessageBox.Show("Введите корректные коэффициенты масштабирования.", "Ошибка ввода");
            }
        }

        private void ChkSelectionMode_Checked(object sender, RoutedEventArgs e)
        {
            if (ChkSelectionMode.IsChecked == true)
            {
                StopAnalysisMode();
            }

            UpdateStatus();
        }

        private void ChkSelectionMode_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_analysisMode == AnalysisMode.SegmentIntersection)
            {
                StatusText.Text = _intersectionStep switch
                {
                    0 => "Пересечение: кликните начало первого ребра.",
                    1 => "Пересечение: кликните конец первого ребра.",
                    2 => "Пересечение: кликните начало второго ребра.",
                    _ => "Пересечение: ведите мышью для предпросмотра второго ребра и кликните его конец."
                };
            }
            else if (_analysisMode == AnalysisMode.PointContainment)
            {
                StatusText.Text = _manager.SelectedPolygon == null
                    ? "Проверка точки: сначала выберите полигон в режиме выбора."
                    : "Кликайте точки на поле; проверка работает для выпуклых и невыпуклых полигонов.";
            }
            else if (_analysisMode == AnalysisMode.EdgeSide)
            {
                StatusText.Text = _sideEdgeStep switch
                {
                    0 => "Сторона ребра: кликните начало ребра.",
                    1 => "Сторона ребра: кликните конец ребра.",
                    _ => "Ребро задано. Кликайте точки для проверки справа или слева."
                };
            }
            else if (_manager.IsDrawingMode)
            {
                StatusText.Text = "РЕЖИМ РИСОВАНИЯ: Кликайте по полю, чтобы добавить вершины.";
            }
            else if (ChkSelectionMode.IsChecked == true)
            {
                StatusText.Text = _manager.SelectedPolygon != null
                    ? "ВЫБРАН ПОЛИГОН. Можно применять преобразования."
                    : "РЕЖИМ ВЫБОРА: Кликните по полигону на поле.";
            }
            else
            {
                StatusText.Text = $"Готово. Полигонов на сцене: {_manager.Polygons.Count}";
            }
        }

        private void AppendAnalysisResult(string result)
        {
            AnalysisResultsText.AppendText(result + Environment.NewLine);
            AnalysisResultsText.ScrollToEnd();
        }

        private void AddOverlayLine(Point start, Point end, Brush brush, double thickness)
        {
            InteractionOverlay.Children.Add(new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = brush,
                StrokeThickness = thickness,
                IsHitTestVisible = false
            });
        }

        private void AddMarker(Point point, Brush brush)
        {
            const double diameter = 9;
            var marker = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                Fill = brush,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(marker, point.X - diameter / 2);
            Canvas.SetTop(marker, point.Y - diameter / 2);
            InteractionOverlay.Children.Add(marker);
        }

        private void AddIntersectionMarker(Point point)
        {
            const double radius = 7;
            AddOverlayLine(
                new Point(point.X - radius, point.Y - radius),
                new Point(point.X + radius, point.Y + radius),
                Brushes.LimeGreen,
                3);
            AddOverlayLine(
                new Point(point.X - radius, point.Y + radius),
                new Point(point.X + radius, point.Y - radius),
                Brushes.LimeGreen,
                3);
        }

        private void UpdateIntersectionPreview(Point end)
        {
            if (_intersectionPreview == null)
            {
                _intersectionPreview = new Line
                {
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 4, 3 },
                    IsHitTestVisible = false
                };
                InteractionOverlay.Children.Add(_intersectionPreview);
            }

            _intersectionPreview.X1 = _secondEdgeStart.X;
            _intersectionPreview.Y1 = _secondEdgeStart.Y;
            _intersectionPreview.X2 = end.X;
            _intersectionPreview.Y2 = end.Y;
        }

        private void RemoveIntersectionPreview()
        {
            if (_intersectionPreview != null)
            {
                InteractionOverlay.Children.Remove(_intersectionPreview);
                _intersectionPreview = null;
            }
        }

        private static string FormatPoint(Point point)
        {
            string x = point.X.ToString("0.##", CultureInfo.CurrentCulture);
            string y = point.Y.ToString("0.##", CultureInfo.CurrentCulture);
            return $"({x}; {y})";
        }
    }
}
