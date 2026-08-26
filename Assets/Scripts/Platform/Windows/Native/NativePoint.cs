using System.Runtime.InteropServices;


namespace Platform.Windows.Native
{
    [StructLayout(
        LayoutKind.Sequential)]
    internal struct NativePoint
    {
        public int X;

        public int Y;
    }
}