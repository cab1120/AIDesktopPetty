using System;
using System.Diagnostics;
using System.Text;
using Platform.Windows.Native;

namespace Platform.Windows
{
    public struct ForegroundContext
    {
        public string Title;
        public string ProcessName;
    }

    public static class WindowsForegroundContextService
    {
        public static bool TryGetCurrent(out ForegroundContext context)
        {
            context = default(ForegroundContext);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                IntPtr handle = WindowsForegroundNativeMethods.GetForegroundWindow();
                if (handle == IntPtr.Zero) return false;
                var text = new StringBuilder(256);
                if (WindowsForegroundNativeMethods.GetWindowText(handle, text, text.Capacity) <= 0)
                    return false;
                WindowsForegroundNativeMethods.GetWindowThreadProcessId(handle, out uint pid);
                string processName = "unknown";
                try
                {
                    using (Process process = Process.GetProcessById((int)pid))
                        processName = process.ProcessName;
                }
                catch (Exception) { }
                context = new ForegroundContext
                {
                    Title = text.ToString(),
                    ProcessName = processName
                };
                return true;
            }
            catch (Exception) { return false; }
#else
            return false;
#endif
        }
    }
}
