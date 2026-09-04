using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OrdTarifManager.Core
{
    public static class DwmHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void EnableDarkMode(Window window)
        {
            if (window == null) return;

            window.SourceInitialized += (sender, e) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle == IntPtr.Zero) return;

                int darkMode = 1;
                // Try modern 20H1+ attribute first
                int hr = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                if (hr != 0)
                {
                    // Fallback for older Windows 10 versions
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                }
            };
        }
    }
}
