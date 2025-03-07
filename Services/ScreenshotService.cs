using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ShareShot.Core;
using ShareShot.Interfaces;
using ShareShot.Forms;

namespace ShareShot.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly NotifyIcon? trayIcon;
        private SelectionForm? selectionForm;
        private bool isCapturing;

        public ScreenshotService(NotifyIcon? trayIcon)
        {
            this.trayIcon = trayIcon;
        }

        public void CaptureScreen()
        {
            if (isCapturing) return;
            isCapturing = true;

            try
            {
                var bounds = GetTotalScreenBounds();
                var screenCapture = CaptureAllScreens(bounds);
                ShowSelectionForm(screenCapture, bounds);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screen: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                isCapturing = false;
            }
        }

        private Rectangle GetTotalScreenBounds()
        {
            var bounds = Rectangle.Empty;
            foreach (var screen in Screen.AllScreens)
            {
                bounds = Rectangle.Union(bounds, screen.Bounds);
            }
            return bounds;
        }

        private Bitmap CaptureAllScreens(Rectangle bounds)
        {
            var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                foreach (var screen in Screen.AllScreens)
                {
                    CaptureScreen(g, screen, bounds);
                }
            }
            return bitmap;
        }

        private void CaptureScreen(Graphics g, Screen screen, Rectangle totalBounds)
        {
            using var screenBitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
            using var screenGraphics = Graphics.FromImage(screenBitmap);
            screenGraphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
            
            g.DrawImage(screenBitmap, 
                screen.Bounds.X - totalBounds.X, 
                screen.Bounds.Y - totalBounds.Y);
        }

        private void ShowSelectionForm(Bitmap screenCapture, Rectangle bounds)
        {
            selectionForm = new SelectionForm(screenCapture, bounds);
            selectionForm.ScreenshotTaken += SelectionForm_ScreenshotTaken;
            selectionForm.FormClosed += (_, _) => isCapturing = false;
            selectionForm.Show();
        }

        private void SelectionForm_ScreenshotTaken(Rectangle selectionRect)
        {
            var bounds = GetTotalScreenBounds();
            var screenCapture = CaptureAllScreens(bounds);
            
            var result = new Bitmap(selectionRect.Width, selectionRect.Height);
            using var g = Graphics.FromImage(result);
            g.DrawImage(screenCapture,
                new Rectangle(0, 0, selectionRect.Width, selectionRect.Height),
                selectionRect,
                GraphicsUnit.Pixel);

            SaveScreenshot(result);
            isCapturing = false;
        }

        public void SaveScreenshot(Bitmap screenshot)
        {
            var fileName = string.Format(Constants.Screenshot.FileNameFormat, DateTime.Now);
            var screenshotsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), 
                Constants.Screenshot.FolderName);
            
            Directory.CreateDirectory(screenshotsFolder);
            var path = Path.Combine(screenshotsFolder, fileName);
            screenshot.Save(path, ImageFormat.Png);
            
            trayIcon?.ShowBalloonTip(Constants.UI.BalloonTipDuration, "Screenshot Saved", 
                $"Screenshot saved to {path}", ToolTipIcon.Info);
        }
    }
} 