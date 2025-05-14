using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1.UI
{
    /// <summary>
    /// Interaction logic for AnnotationWindow.xaml
    /// </summary>
    // AnnotationWindow.xaml.cs
    public partial class AnnotationWindow : Window
    {
        private Point startPoint;
        private Shape tempShape;
        private string currentTool = "Rectangle";
        private BitmapImage backgroundImage;
        private readonly List<Shape> drawnShapes = new List<Shape>();
        private readonly List<UIElement> drawnElements = new List<UIElement>();

        public AnnotationWindow(BitmapImage image)
        {
            InitializeComponent();
            backgroundImage = image;

            drawingCanvas.Width = image.PixelWidth;
            drawingCanvas.Height = image.PixelHeight;

            this.Width = image.PixelWidth;
            this.Height = image.PixelHeight + 40; // for toolbar

            drawingCanvas.Background = new ImageBrush(image);
        }

        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                currentTool = button.Tag.ToString();  // Set the tool based on the button's Tag
            }
        }
        private void Tool_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                currentTool = rb.Tag.ToString();
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(drawingCanvas);

            if (currentTool == "Rectangle")
            {
                tempShape = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };
            }
            else if (currentTool == "Arrow")
            {
                tempShape = new Path
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Data = Geometry.Empty
                };
            }
            else if (currentTool == "Text")
            {
                var textBox = new TextBox
                {
                    ToolTip = "Your Text",
                    Foreground = Brushes.Red,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 16,
                    AcceptsReturn = false,
                    Width = 100
                };

                Canvas.SetLeft(textBox, startPoint.X);
                Canvas.SetTop(textBox, startPoint.Y);

                textBox.LostFocus += (s, ev) =>
                {
                    // Convert to TextBlock after editing
                    var finalText = new TextBlock
                    {
                        Text = textBox.Text,
                        Foreground = Brushes.Red,
                        FontSize = 16
                    };
                    Canvas.SetLeft(finalText, Canvas.GetLeft(textBox));
                    Canvas.SetTop(finalText, Canvas.GetTop(textBox));
                    drawingCanvas.Children.Remove(textBox);
                    drawingCanvas.Children.Add(finalText);
                    drawnElements.Add(finalText);
                };

                drawingCanvas.Children.Add(textBox);
                drawnElements.Add(textBox);
                textBox.Focus();
            }

            if (tempShape != null)
            {
                drawingCanvas.Children.Add(tempShape);
                drawnElements.Add(tempShape);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (tempShape == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point pos = e.GetPosition(drawingCanvas);

            if (tempShape is Rectangle rect)
            {
                double x = Math.Min(pos.X, startPoint.X);
                double y = Math.Min(pos.Y, startPoint.Y);
                double w = Math.Abs(pos.X - startPoint.X);
                double h = Math.Abs(pos.Y - startPoint.Y);

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                rect.Width = w;
                rect.Height = h;
            }
            else if (tempShape is Path path)
            {
                Point endPoint = pos;

                // Vector from start to end
                Vector direction = endPoint - startPoint;
                direction.Normalize();

                // Arrowhead size
                double arrowHeadLength = 12;
                double arrowHeadWidth = 6;

                // Main line
                var shaft = new PathFigure { StartPoint = startPoint };
                shaft.Segments.Add(new LineSegment(endPoint, true));

                // Calculate arrowhead base
                Point basePoint = endPoint - direction * arrowHeadLength;
                Vector normal = new Vector(-direction.Y, direction.X) * arrowHeadWidth;

                Point left = basePoint + normal;
                Point right = basePoint - normal;

                // Arrowhead triangle
                var arrowHead = new PathFigure { StartPoint = endPoint, IsFilled = true, IsClosed = true };
                arrowHead.Segments.Add(new LineSegment(left, true));
                arrowHead.Segments.Add(new LineSegment(right, true));

                var geometry = new PathGeometry();
                geometry.Figures.Add(shaft);
                geometry.Figures.Add(arrowHead);

                path.Data = geometry;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (tempShape != null)
            {
                drawnShapes.Add(tempShape);
                tempShape = null;
            }
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            // Hide the toolbar temporarily
            toolbarPanel.Visibility = Visibility.Collapsed;

            // Give UI time to update layout before rendering
            drawingCanvas.UpdateLayout();

            // Wait for layout pass to complete
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

            // Render the canvas to bitmap
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)drawingCanvas.Width,
                (int)drawingCanvas.Height,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingCanvas);

            // Restore toolbar
            toolbarPanel.Visibility = Visibility.Visible;

            // Copy image to clipboard
            Clipboard.SetImage(rtb);
            this.Close();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (drawnShapes.Count > 0)
            {
                var lastShape = drawnShapes.Last();
                drawingCanvas.Children.Remove(lastShape);
                drawnShapes.RemoveAt(drawnShapes.Count - 1);
            }
            if (drawnElements.Count > 0)
            {
                var lastElement = drawnElements.Last();
                drawingCanvas.Children.Remove(lastElement);
                drawnElements.RemoveAt(drawnElements.Count - 1);
            }
        }
        private void ToolbarPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            toolbarPanel.Opacity = 1.0;
        }

        private void ToolbarPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            toolbarPanel.Opacity = 0.3;
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}
