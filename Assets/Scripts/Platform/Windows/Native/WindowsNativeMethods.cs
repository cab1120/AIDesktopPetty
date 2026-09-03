using System;
using System.Runtime.InteropServices;


namespace Platform.Windows.Native
{
    /// <summary>
    /// 唯一允许直接 P/Invoke
    /// DesktopPet.Native.Windows 的 C# 类型。
    ///
    /// 除 Platform/Windows/Native 外，
    /// 项目其他代码不得直接引用 Native ABI。
    /// </summary>
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
        
        // ======================================================
        // Desktop Pointer
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_GetCursorPosition(
                out NativePoint point);



        // ======================================================
        // Monitor Query
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_IsPointOnAnyMonitor(
                int x,
                int y);


        // ======================================================
        // Window Interaction
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_BeginWindowDrag();
        
        // ======================================================
        // Click Through
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_SetClickThrough(
                int enabled);
        
        // ======================================================
        // Version / Capabilities
        // ======================================================

        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern int
            DP_GetApiVersion(
                out NativeApiVersion version);


        [DllImport(
            LibraryName,
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        internal static extern ulong
            DP_GetCapabilities();
#endif
    }
}