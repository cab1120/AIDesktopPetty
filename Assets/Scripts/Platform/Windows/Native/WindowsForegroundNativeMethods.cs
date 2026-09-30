using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Platform.Windows.Native
{
    internal static class WindowsForegroundNativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr handle, StringBuilder text, int count);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    }
}
