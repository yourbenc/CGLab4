using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CGLab4.Models;

namespace CGLab4
{
    public partial class MainWindow : Window
    {
        private readonly PolygonManager _manager = new PolygonManager();

        public MainWindow()
        {
            InitializeComponent();
            UpdateStatus();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(DrawingCanvas);

            // 1. Если включен режим выбора, обрабатываем выбор (Участник 3 улучшит точность)
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
            // TODO (Участник 3): Здесь можно реализовать динамическое добавление второго ребра 
            // или предварительный просмотр пересечений без перерисовки всего Canvas.
        }

        private void BtnNewPolygon_Click(object sender, RoutedEventArgs e)
        {
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
            _manager.ClearScene();
            _manager.Render(DrawingCanvas);
            UpdateStatus();
        }

        /// <summary>
        /// Обработчик кнопки смещения. Демонстрирует работу с матрицами.
        /// </summary>
        private void BtnTranslate_Click(object sender, RoutedEventArgs e)
        {
            if (_manager.SelectedPolygon == null)
            {
                MessageBox.Show("Сначала включите 'Режим выбора' и кликните по полигону на поле!", "Внимание");
                return;
            }

            if (double.TryParse(TxtDx.Text, out double dx) && double.TryParse(TxtDy.Text, out double dy))
            {
                // Создаем матрицу смещения (Translation Matrix)
                // | 1  0 dx |
                // | 0  1 dy |
                // | 0  0  1 |
                Matrix3x3 translationMatrix = new Matrix3x3(
                    1, 0, dx,
                    0, 1, dy,
                    0, 0, 1
                );

                _manager.SelectedPolygon.ApplyTransformation(translationMatrix);

                _manager.Render(DrawingCanvas);
            }
            else
            {
                MessageBox.Show("Введите корректные числа для dx и dy", "Ошибка ввода");
            }
        }

        private void UpdateStatus()
        {
            if (_manager.IsDrawingMode)
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

        // Обновляем статус при переключении чекбокса
        private void ChkSelectionMode_Checked(object sender, RoutedEventArgs e) => UpdateStatus();
        private void ChkSelectionMode_Unchecked(object sender, RoutedEventArgs e) => UpdateStatus();
    }
}