using System;


namespace Platform.Windows.Native
{
    [Flags]
    internal enum NativeCapability
        : ulong
    {
        None =
            0,

        Borderless =
            1UL << 0,

        TransparentBackground =
            1UL << 1,

        WindowBounds =
            1UL << 2,

        TopMost =
            1UL << 3,

        MonitorInfo =
            1UL << 4,

        Dpi =
            1UL << 5,

        CursorPosition =
            1UL << 6,

        WindowDrag =
            1UL << 7,

        ClickThrough =
            1UL << 8,

        MultiMonitor =
            1UL << 9
    }
}