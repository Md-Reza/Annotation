using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp1.Utilitys;

namespace WpfApp1.UI
{
    /// <summary>
    /// Interaction logic for CanvasWindow.xaml
    /// </summary>
    public partial class CanvasWindow : Window
    {
        public ToolType CurrentTool { get; set; } = ToolType.None;

        private Point startPoint;
        private Shape currentShape;

        public CanvasWindow()
        {
            InitializeComponent();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(DrawCanvas);

            switch (CurrentTool)
            {
                case ToolType.Line:
                    currentShape = new Line
                    {
                        Stroke = Brushes.Blue,
                        StrokeThickness = 2,
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = startPoint.X,
                        Y2 = startPoint.Y
                    };
                    break;

                case ToolType.Rectangle:
                    currentShape = new Rectangle
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(currentShape, startPoint.X);
                    Canvas.SetTop(currentShape, startPoint.Y);
                    break;

                case ToolType.Freehand:
                    currentShape = new Polyline
                    {
                        Stroke = Brushes.Green,
                        StrokeThickness = 2
                    };
                    ((Polyline)currentShape).Points.Add(startPoint);
                    break;
            }

            if (currentShape != null)
            {
                DrawCanvas.Children.Add(currentShape);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentShape == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point currentPoint = e.GetPosition(DrawCanvas);

            switch (CurrentTool)
            {
                case ToolType.Line:
                    ((Line)currentShape).X2 = currentPoint.X;
                    ((Line)currentShape).Y2 = currentPoint.Y;
                    break;

                case ToolType.Rectangle:
                    double x = Math.Min(currentPoint.X, startPoint.X);
                    double y = Math.Min(currentPoint.Y, startPoint.Y);
                    double w = Math.Abs(currentPoint.X - startPoint.X);
                    double h = Math.Abs(currentPoint.Y - startPoint.Y);

                    Canvas.SetLeft(currentShape, x);
                    Canvas.SetTop(currentShape, y);
                    ((Rectangle)currentShape).Width = w;
                    ((Rectangle)currentShape).Height = h;
                    break;

                case ToolType.Freehand:
                    ((Polyline)currentShape).Points.Add(currentPoint);
                    break;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            currentShape = null;
        }

        public void ClearCanvas()
        {
            DrawCanvas.Children.Clear();
        }

        public void SaveCanvas()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Annotation";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Image (.png)|*.png";

            if (dlg.ShowDialog() == true)
            {
                string filename = dlg.FileName;

                // Create a bitmap of the canvas
                double width = DrawCanvas.ActualWidth;
                double height = DrawCanvas.ActualHeight;

                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int)width, (int)height, 96d, 96d, PixelFormats.Pbgra32);
                rtb.Render(DrawCanvas);

                // Encode as PNG
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                // Save to file
                using (var fs = System.IO.File.OpenWrite(filename))
                {
                    encoder.Save(fs);
                }

                System.Windows.MessageBox.Show("Image saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
