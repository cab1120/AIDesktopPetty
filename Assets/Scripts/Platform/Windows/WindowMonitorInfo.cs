namespace Platform.Windows
{
    public struct WindowMonitorInfo
    {
        public WindowRect Bounds;

        public WindowRect WorkArea;

        public uint Dpi;

        public float DpiScale;

        public bool IsPrimary;


        public override string ToString()
        {
            return
                $"DPI={Dpi}, " +
                $"Scale={DpiScale:F2}, " +
                $"WorkArea=" +
                $"{WorkArea.Left}," +
                $"{WorkArea.Top}," +
                $"{WorkArea.Width}x" +
                $"{WorkArea.Height}, " +
                $"Primary={IsPrimary}";
        }
    }
}