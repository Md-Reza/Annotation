using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1.UI
{
    /// <summary>
    /// Interaction logic for SnippingOverlay.xaml
    /// </summary>
    public partial class SnippingOverlay : Window
    {
        private Point startPoint;
        private Rectangle selectionRect;
        public Rect SelectedRegion { get; private set; }

        public SnippingOverlay()
        {
            InitializeComponent();
            selectionRect = new Rectangle
            {
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2
            };
            SelectionCanvas.Children.Add(selectionRect);
            Cursor = Cursors.Cross;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(this);
            Canvas.SetLeft(selectionRect, startPoint.X);
            Canvas.SetTop(selectionRect, startPoint.Y);
            selectionRect.Width = 0;
            selectionRect.Height = 0;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(this);
                var x = Math.Min(pos.X, startPoint.X);
                var y = Math.Min(pos.Y, startPoint.Y);
                var w = Math.Abs(pos.X - startPoint.X);
                var h = Math.Abs(pos.Y - startPoint.Y);
                Canvas.SetLeft(selectionRect, x);
                Canvas.SetTop(selectionRect, y);
                selectionRect.Width = w;
                selectionRect.Height = h;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var x = Canvas.GetLeft(selectionRect);
            var y = Canvas.GetTop(selectionRect);
            var w = selectionRect.Width;
            var h = selectionRect.Height;
            SelectedRegion = new Rect(x, y, w, h);
            DialogResult = true;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
