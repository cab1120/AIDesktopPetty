namespace Platform.Windows.Models
{
    public struct WindowRect
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;


        public int Width => Right - Left;


        public int Height => Bottom - Top;
    }
}