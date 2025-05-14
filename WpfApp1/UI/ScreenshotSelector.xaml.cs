using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1.UI
{
    /// <summary>
    /// Interaction logic for ScreenshotSelector.xaml
    /// </summary>
    public partial class ScreenshotSelector : Window
    {
        private System.Windows.Point startPoint;
        private System.Windows.Shapes.Rectangle selectionRectangle;
        private System.Windows.Shapes.Rectangle visualRectangle;

        public ScreenshotSelector()
        {
            InitializeComponent();
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(this);

            visualRectangle = new System.Windows.Shapes.Rectangle
            {
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4 }
            };

            OverlayCanvas.Children.Add(visualRectangle);
            Canvas.SetLeft(visualRectangle, startPoint.X);
            Canvas.SetTop(visualRectangle, startPoint.Y);
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && visualRectangle != null)
            {
                var pos = e.GetPosition(this);
                var x = Math.Min(pos.X, startPoint.X);
                var y = Math.Min(pos.Y, startPoint.Y);
                var width = Math.Abs(pos.X - startPoint.X);
                var height = Math.Abs(pos.Y - startPoint.Y);

                Canvas.SetLeft(visualRectangle, x);
                Canvas.SetTop(visualRectangle, y);
                visualRectangle.Width = width;
                visualRectangle.Height = height;
            }
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (visualRectangle != null)
            {
                int x = (int)Canvas.GetLeft(visualRectangle);
                int y = (int)Canvas.GetTop(visualRectangle);
                int width = (int)visualRectangle.Width;
                int height = (int)visualRectangle.Height;

                TakeScreenshot(x, y, width, height);
                this.Close();
            }
        }

        private void TakeScreenshot(int x, int y, int width, int height)
        {
            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
                }

                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "screenshot.png");
                bmp.Save(path, ImageFormat.Png);
                MessageBox.Show($"Screenshot saved: {path}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
