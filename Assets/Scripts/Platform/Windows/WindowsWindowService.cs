using UnityEngine;

using Platform.Windows.Native;


namespace Platform.Windows
{
    internal sealed class WindowsWindowService
        : IWindowService
    {
        private const float BaseDpi =
            96f;


        /// <summary>
        /// 防止窗口在极端小屏幕 /
        /// 高 DPI 环境下占满整个工作区。
        /// </summary>
        private const float
            MaximumWorkAreaRatio =
                0.92f;


        private bool _initialized;


        // ======================================================
        // Lifecycle
        // ======================================================

        public bool IsInitialized
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

                if (!_initialized)
                {
                    return false;
                }


                return
                    WindowsNativeMethods
                        .DP_IsInitialized()
                    != 0;

#else

                return false;

#endif
            }
        }


        internal bool Initialize()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            int result =
                WindowsNativeMethods
                    .DP_Initialize();


            _initialized =
                result != 0;


            if (!_initialized)
            {
                LogNativeFailure(
                    "DP_Initialize"
                );

                return false;
            }


            LogMonitorDiagnostics();


            return true;

#else

            _initialized =
                false;


            Debug.Log(
                "[WindowsWindowService] " +
                "Native Windows service disabled."
            );


            return false;

#endif
        }


        // ======================================================
        // Borderless
        // ======================================================

        public bool SetBorderless(
            bool enabled)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(SetBorderless)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_SetBorderless(
                        enabled ? 1 : 0
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    $"DP_SetBorderless({enabled})"
                );

                return false;
            }


            return true;

#else

            return false;

#endif
        }


        // ======================================================
        // Transparency
        // ======================================================

        public bool SetTransparentBackground(
            bool enabled)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(
                        SetTransparentBackground)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_SetTransparentBackground(
                        enabled ? 1 : 0
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    "DP_SetTransparentBackground" +
                    $"({enabled})"
                );

                return false;
            }


            return true;

#else

            return false;

#endif
        }


        // ======================================================
        // Monitor
        // ======================================================

        public bool TryGetCurrentMonitorInfo(
            out WindowMonitorInfo info)
        {
            info =
                default;


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(
                        TryGetCurrentMonitorInfo)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_GetCurrentMonitorInfo(
                        out NativeMonitorInfo native
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    "DP_GetCurrentMonitorInfo"
                );

                return false;
            }


            info =
                ConvertMonitorInfo(
                    native
                );


            return true;

#else

            return false;

#endif
        }


        // ======================================================
        // Logical Window Resize
        // ======================================================

        public bool SetLogicalSize(
            WindowLogicalSize size)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(SetLogicalSize)))
            {
                return false;
            }


            if (size.Width <= 0 ||
                size.Height <= 0)
            {
                Debug.LogError(
                    "[WindowsWindowService] " +
                    "Invalid logical window size: " +
                    size
                );


                return false;
            }


            if (!TryGetCurrentMonitorInfo(
                    out WindowMonitorInfo monitor))
            {
                return false;
            }


            // ----------------------------------------------
            // 1. Logical → Physical
            // ----------------------------------------------

            float dpiScale =
                monitor.DpiScale;


            int desiredWidth =
                Mathf.RoundToInt(
                    size.Width *
                    dpiScale
                );


            int desiredHeight =
                Mathf.RoundToInt(
                    size.Height *
                    dpiScale
                );


            // ----------------------------------------------
            // 2. WorkArea Safety Clamp
            // ----------------------------------------------

            int maximumWidth =
                Mathf.Max(
                    1,
                    Mathf.FloorToInt(
                        monitor.WorkArea.Width *
                        MaximumWorkAreaRatio
                    )
                );


            int maximumHeight =
                Mathf.Max(
                    1,
                    Mathf.FloorToInt(
                        monitor.WorkArea.Height *
                        MaximumWorkAreaRatio
                    )
                );


            float fitScale =
                Mathf.Min(
                    1f,
                    maximumWidth /
                        (float)desiredWidth,
                    maximumHeight /
                        (float)desiredHeight
                );


            int finalWidth =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        desiredWidth *
                        fitScale
                    )
                );


            int finalHeight =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        desiredHeight *
                        fitScale
                    )
                );


            // ----------------------------------------------
            // 3. Get Current Window Position
            // ----------------------------------------------

            int rectResult =
                WindowsNativeMethods
                    .DP_GetWindowRect(
                        out NativeRect currentRect
                    );


            if (rectResult == 0)
            {
                LogNativeFailure(
                    "DP_GetWindowRect"
                );

                return false;
            }


            // ----------------------------------------------
            // 4. Preserve Position, But Keep It Inside
            //    Current Monitor WorkArea
            // ----------------------------------------------

            int maxX =
                monitor.WorkArea.Right -
                finalWidth;


            int maxY =
                monitor.WorkArea.Bottom -
                finalHeight;


            int finalX =
                Mathf.Clamp(
                    currentRect.Left,
                    monitor.WorkArea.Left,
                    maxX
                );


            int finalY =
                Mathf.Clamp(
                    currentRect.Top,
                    monitor.WorkArea.Top,
                    maxY
                );


            // ----------------------------------------------
            // 5. Apply Native Bounds
            // ----------------------------------------------

            int setResult =
                WindowsNativeMethods
                    .DP_SetWindowBounds(
                        finalX,
                        finalY,
                        finalWidth,
                        finalHeight
                    );


            if (setResult == 0)
            {
                LogNativeFailure(
                    "DP_SetWindowBounds"
                );

                return false;
            }


            Debug.Log(
                "[WindowsWindowService] " +
                $"LogicalSize={size}, " +
                $"DPI={monitor.Dpi}, " +
                $"DpiScale={dpiScale:F2}, " +
                $"PhysicalSize=" +
                $"{finalWidth}x{finalHeight}, " +
                $"FitScale={fitScale:F2}"
            );


            return true;

#else

            return false;

#endif
        }


        // ======================================================
        // TopMost
        // ======================================================

        public bool SetTopMost(
            bool enabled)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(SetTopMost)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_SetTopMost(
                        enabled ? 1 : 0
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    $"DP_SetTopMost({enabled})"
                );

                return false;
            }


            return true;

#else

            return false;

#endif
        }


        // ======================================================
        // Conversion
        // ======================================================

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        private static
            WindowMonitorInfo ConvertMonitorInfo(
                NativeMonitorInfo native)
        {
            WindowMonitorInfo result =
                new WindowMonitorInfo
                {
                    Bounds =
                        ConvertRect(
                            native.MonitorBounds
                        ),

                    WorkArea =
                        ConvertRect(
                            native.WorkArea
                        ),

                    Dpi =
                        native.Dpi,

                    DpiScale =
                        native.Dpi /
                        BaseDpi,

                    IsPrimary =
                        native.IsPrimary != 0
                };


            return result;
        }


        private static WindowRect ConvertRect(
            NativeRect native)
        {
            return new WindowRect
            {
                Left =
                    native.Left,

                Top =
                    native.Top,

                Right =
                    native.Right,

                Bottom =
                    native.Bottom
            };
        }

#endif


        // ======================================================
        // Validation
        // ======================================================

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        private bool EnsureInitialized(
            string operation)
        {
            if (IsInitialized)
            {
                return true;
            }


            Debug.LogError(
                "[WindowsWindowService] " +
                $"{operation} failed: " +
                "window service is not initialized."
            );


            return false;
        }

#endif


        // ======================================================
        // Diagnostics
        // ======================================================

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        private void LogMonitorDiagnostics()
        {
            if (!TryGetCurrentMonitorInfo(
                    out WindowMonitorInfo info))
            {
                return;
            }


            Debug.Log(
                "[WindowsWindowService] " +
                $"Current monitor: {info}"
            );


            if (info.Dpi == 96)
            {
                Debug.LogWarning(
                    "[WindowsWindowService] " +
                    "Window DPI is 96. " +
                    "If Windows Display Scale is above 100%, " +
                    "the Player may not currently be " +
                    "Per-Monitor DPI aware."
                );
            }
        }


        private static void LogNativeFailure(
            string operation)
        {
            uint errorCode =
                WindowsNativeMethods
                    .DP_GetLastErrorCode();


            NativeErrorDomain domain =
                (NativeErrorDomain)
                WindowsNativeMethods
                    .DP_GetLastErrorDomain();


            string formattedCode =
                domain ==
                NativeErrorDomain.HResult

                    ? $"0x{errorCode:X8}"

                    : errorCode.ToString();


            Debug.LogError(
                "[WindowsWindowService] " +
                $"{operation} failed. " +
                $"Domain={domain}, " +
                $"Code={formattedCode}"
            );
        }

#endif
    }
}