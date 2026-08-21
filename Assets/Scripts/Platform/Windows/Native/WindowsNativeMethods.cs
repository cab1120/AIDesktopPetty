using System;
using System.Runtime.InteropServices;


namespace Platform.Windows.Native
{
    internal static class WindowsNativeMethods
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        private const string LibraryName =
            "DesktopPet.Native.Windows";


        // ======================================================
        // Lifecycle
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_Initialize();


        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_IsInitialized();


        // ======================================================
        // Window Handle
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern IntPtr
            DP_GetMainWindow();


        // ======================================================
        // Style
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_SetBorderless(
                int enabled);


        // ======================================================
        // DWM
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_SetTransparentBackground(
                int enabled);


        // ======================================================
        // Geometry
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_GetWindowRect(
                out NativeRect rect);


        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_SetWindowBounds(
                int x,
                int y,
                int width,
                int height);


        // ======================================================
        // Monitor / DPI
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_GetCurrentMonitorInfo(
                out NativeMonitorInfo info);


        // ======================================================
        // Z Order
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_SetTopMost(
                int enabled);


        // ======================================================
        // Diagnostics
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern uint
            DP_GetLastErrorCode();


        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_GetLastErrorDomain();

#endif
    }
}