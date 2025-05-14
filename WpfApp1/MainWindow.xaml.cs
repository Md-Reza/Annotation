using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp1.UI;
using WpfApp1.Utilitys;
using Image = System.Windows.Controls.Image;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string keyPath = @"Software\LKey";
        string KeyName = "Key";
        string licenseKey;
        private CanvasWindow canvasWindow;
        private NotifyIcon trayIcon;
        private bool magnifierActive = false;
        private Window magnifierWindow;
        private const int magnifierSize = 100;
        private const double zoomFactor = 1.5;
        public MainWindow()
        {
            InitializeComponent();
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.AllowsTransparency = true;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            trayIcon = new NotifyIcon
            {
                Icon = new Icon(SystemIcons.Application, 40, 40),
                Visible = true,
                Text = "Annotation Tool"
            };

            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Open", null, (s, ea) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            });
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, ea) =>
            {
                trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });

            trayIcon.DoubleClick += (s, ea) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            licenseKey = ReadDecryptedValue(keyPath, KeyName);
        }
        private static string ReadDecryptedValue(string path, string name)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path))
            {
                if (key == null)
                    throw new Exception("Registry key not found.");

                string encryptedBase64 = key.GetValue(name)?.ToString();

                if (string.IsNullOrEmpty(encryptedBase64))
                    throw new Exception("Registry value not found or empty.");

                byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

                // Decrypt
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(plainBytes);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FRMLicenseValidator validateLicense = new FRMLicenseValidator();
            validateLicense.Show();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove(); // allows dragging the window
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide(); // or this.WindowState = WindowState.Minimized;
            base.OnClosing(e);
        }
        private void EnsureCanvasWindow()
        {
            if (canvasWindow == null || !canvasWindow.IsLoaded)
            {
                canvasWindow = new CanvasWindow();
                canvasWindow.Show();
            }
            else
            {
                canvasWindow.Activate();
            }
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureCanvasWindow();
            canvasWindow.CurrentTool = ToolType.Line;
        }

        private void RectButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureCanvasWindow();
            canvasWindow.CurrentTool = ToolType.Rectangle;
        }

        private void FreehandButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureCanvasWindow();
            canvasWindow.CurrentTool = ToolType.Freehand;
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            // Add text tool logic later
            EnsureCanvasWindow();
            canvasWindow.CurrentTool = ToolType.Text;
        }
        private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            // Get DPI info before hiding
            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX;
            double dpiY = dpi.DpiScaleY;

            this.Hide();
            await Task.Delay(300); // Ensure the window is fully hidden

            var overlay = new SnippingOverlay();
            if (overlay.ShowDialog() == true)
            {
                var regionDip = overlay.SelectedRegion;

                // Convert logical (WPF) to physical (screen) pixels
                var region = new System.Drawing.Rectangle(
                    (int)(regionDip.X * dpiX),
                    (int)(regionDip.Y * dpiY),
                    (int)(regionDip.Width * dpiX),
                    (int)(regionDip.Height * dpiY));

                if (region.Width > 0 && region.Height > 0)
                {
                    using (var bmp = new Bitmap(region.Width, region.Height))
                    {
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(region.X, region.Y, 0, 0, bmp.Size);
                        }

                        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "snip.png");
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);

                        //System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        //{
                        //    FileName = path,
                        //    UseShellExecute = true
                        //});

                        BitmapImage image = ConvertBitmapToBitmapImage(bmp);
                        var annotationWindow = new AnnotationWindow(image);
                        annotationWindow.ShowDialog();
                    }
                }
            }

            this.Show();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureCanvasWindow();
            canvasWindow.ClearCanvas();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureCanvasWindow();
            canvasWindow.SaveCanvas();
        }
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        private void MagnifierButton_Click(object sender, RoutedEventArgs e)
        {
            magnifierActive = !magnifierActive;

            if (magnifierActive)
            {
                // Activate magnifier
                CompositionTarget.Rendering += UpdateMagnifier;
            }
            else
            {
                // Deactivate magnifier
                CompositionTarget.Rendering -= UpdateMagnifier;
                HideMagnifier();
            }
            
        }


        private void UpdateMagnifier(object sender, EventArgs e)
        {
            var mousePos = System.Windows.Forms.Cursor.Position;

            if (magnifierWindow == null)
            {
                // Create magnifier window
                magnifierWindow = new Window
                {
                    Width = magnifierSize,
                    Height = magnifierSize,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    IsHitTestVisible = false,
                    Topmost = true,
                    ShowInTaskbar = false
                };

                var border = new Border
                {
                    Width = magnifierSize,
                    Height = magnifierSize,
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Clip = new EllipseGeometry(new System.Windows.Point(magnifierSize / 2, magnifierSize / 2), magnifierSize / 2, magnifierSize / 2)
                };

                var image = new System.Windows.Controls.Image
                {
                    Width = magnifierSize,
                    Height = magnifierSize,
                    RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
                };

                border.Child = image;
                magnifierWindow.Content = border;
                magnifierWindow.Show();
            }

            // Update magnifier window position
            magnifierWindow.Left = mousePos.X - magnifierSize / 2;
            magnifierWindow.Top = mousePos.Y - magnifierSize / 2;

            int captureSize = (int)(magnifierSize / zoomFactor);
            int halfCapture = captureSize / 2;

            using (var bmp = new Bitmap(captureSize, captureSize))
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(mousePos.X - halfCapture, mousePos.Y - halfCapture, 0, 0, bmp.Size);

                var hBitmap = bmp.GetHbitmap();
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(captureSize, captureSize));

                var image = ((magnifierWindow.Content as Border)?.Child as System.Windows.Controls.Image);
                image.Source = bitmapSource;

                // Scale up to fill the magnifier window
                image.Width = magnifierSize;
                image.Height = magnifierSize;
                image.RenderTransform = new ScaleTransform(zoomFactor, zoomFactor); // apply zoom

                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);


        private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var position = System.Windows.Forms.Cursor.Position; // screen coordinates

            if (magnifierWindow == null)
            {
                magnifierWindow = new Window
                {
                    Width = 100,
                    Height = 100,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    IsHitTestVisible = false,
                    Topmost = true,
                    ShowInTaskbar = false
                };

                var border = new Border
                {
                    Width = 100,
                    Height = 100,
                    CornerRadius = new CornerRadius(50),
                    BorderThickness = new Thickness(2),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    ClipToBounds = true
                };

                var image = new Image
                {
                    Width = 200, // zoom factor 2x
                    Height = 200,
                    RenderTransform = new TranslateTransform()
                };

                border.Child = image;
                magnifierWindow.Content = border;
                magnifierWindow.Show();
            }

            magnifierWindow.Left = position.X - 50;
            magnifierWindow.Top = position.Y - 50;

            // Capture screen
            using (var screenBmp = new Bitmap(200, 200))
            {
                using (var g = Graphics.FromImage(screenBmp))
                {
                    g.CopyFromScreen(position.X - 50, position.Y - 50, 0, 0, screenBmp.Size);
                }

                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    screenBmp.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(200, 200));

                var image = (magnifierWindow.Content as Border)?.Child as Image;
                if (image != null)
                {
                    image.Source = bitmapSource;
                    image.RenderTransform = new TranslateTransform(0, 0); // no translation needed
                }
            }
        }

        private void HideMagnifier()
        {
            magnifierWindow?.Close();
            magnifierWindow = null;
        }
    }
}
