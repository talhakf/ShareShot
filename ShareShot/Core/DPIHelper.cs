using System;
using System.Runtime.InteropServices;

namespace ShareShot.Core
{
    public static class DPIHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public static void SetDPIAwareness()
        {
            try
            {
                SetProcessDPIAware();
            }
            catch (Exception)
            {
                // Silently fail if DPI awareness cannot be set
                // This might happen on older Windows versions
            }
        }
    }
} 