using System.Runtime.InteropServices;


namespace Platform.Windows.Native
{
    [StructLayout(
        LayoutKind.Sequential)]
    internal struct NativeMonitorInfo
    {
        public NativeRect MonitorBounds;

        public NativeRect WorkArea;

        public uint Dpi;

        public int IsPrimary;
    }
}