using System.Runtime.InteropServices;


namespace Platform.Windows.Native
{
    [StructLayout(
        LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;


        public int Width =>
            Right - Left;


        public int Height =>
            Bottom - Top;
    }
}