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
        // Window Style
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