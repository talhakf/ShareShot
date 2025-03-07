using System.Drawing;

namespace ShareShot.Interfaces
{
    public interface IScreenshotService
    {
        void CaptureScreen();
        void SaveScreenshot(Bitmap screenshot);
    }
} 