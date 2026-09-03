using Platform.Windows.Models;
using UnityEngine;

using Platform.Windows.Native;


namespace Platform.Windows
{
    internal sealed class WindowsWindowService
        : IWindowService
    {
        private const float BaseDpi =
            96f;
        
        private const uint
            ExpectedNativeMajorVersion =
                1;


        /// <summary>
        /// 防止窗口在极端小屏幕 /
        /// 高 DPI 环境下占满整个工作区。
        /// </summary>
        private const float
            MaximumWorkAreaRatio =
                0.92f;


        private bool _initialized;
        
        /// <summary>
        /// 缓存日志
        /// </summary>
        private NativeApiVersion
            _nativeVersion;


        private NativeCapability
            _nativeCapabilities;


        private WindowMonitorInfo
            _startupMonitorInfo;


        private bool
            _hasStartupMonitorInfo;

        // ======================================================
        // Native Self Check
        // ======================================================
        private bool ValidateNativeContract()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            int versionResult =
                WindowsNativeMethods
                    .DP_GetApiVersion(
                        out NativeApiVersion version
                    );


            if (versionResult == 0)
            {
                LogNativeFailure(
                    "DP_GetApiVersion"
                );


                return false;
            }


            _nativeVersion =
                version;


            if (version.Major !=
                ExpectedNativeMajorVersion)
            {
                Debug.LogError(
                    "[WindowsWindowService] " +
                    "Native API major version mismatch. " +
                    $"Expected=" +
                    $"{ExpectedNativeMajorVersion}.x.x, " +
                    $"Actual={version}"
                );


                return false;
            }


                _nativeCapabilities =
                    (NativeCapability)
                    WindowsNativeMethods
                        .DP_GetCapabilities();


            NativeCapability required =
                GetRequiredCapabilities();


            NativeCapability missing =
                required &
                ~_nativeCapabilities;


            if (missing !=
                NativeCapability.None)
            {
                Debug.LogError(
                    "[WindowsWindowService] " +
                    "Native DLL is missing " +
                    $"required capabilities: {missing}"
                );


                return false;
            }


            return true;

#else

            return false;

#endif
        }

        private NativeCapability GetRequiredCapabilities()
        {
            return                 
                NativeCapability.Borderless
                |
                NativeCapability.TransparentBackground
                |
                NativeCapability.WindowBounds
                |
                NativeCapability.TopMost
                |
                NativeCapability.MonitorInfo
                |
                NativeCapability.Dpi
                |
                NativeCapability.CursorPosition
                |
                NativeCapability.WindowDrag
                |
                NativeCapability.ClickThrough
                |
                NativeCapability.MultiMonitor;
        }
        
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

            if (!ValidateNativeContract())
            {
                _initialized =
                    false;


                return false;
            }

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


        internal void LogStartupDiagnostics(
            bool initialized)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

                if (!initialized)
                {
                    Debug.LogError(
                        "[WindowsPlatform] " +
                        "Initialization failed."
                    );

                    return;
                }


                Debug.Log(
                    "[WindowsPlatform] " +
                    $"Native API Version: " +
                    $"{_nativeVersion}"
                );


                Debug.Log(
                    "[WindowsPlatform] " +
                    $"Native Capabilities: " +
                    $"{_nativeCapabilities}"
                );


                if (_hasStartupMonitorInfo)
                {
                    Debug.Log(
                        "[WindowsPlatform] " +
                        "Startup Monitor: " +
                        $"{_startupMonitorInfo}"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        "[WindowsPlatform] " +
                        "Startup monitor information " +
                        "was unavailable."
                    );
                }

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
             if (TryGetCurrentMonitorInfo(
                    out WindowMonitorInfo monitorInfo))
            {
                _startupMonitorInfo =
                    monitorInfo;

                _hasStartupMonitorInfo =
                    true;
            }
            else
            {
                _hasStartupMonitorInfo =
                    false;
                return;
            }


            if (monitorInfo.Dpi == 96)
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
        
        
        public bool TryGetWindowRect(
            out WindowRect rect)
        {
            rect =
                default;


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(TryGetWindowRect)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_GetWindowRect(
                        out NativeRect native
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    "DP_GetWindowRect"
                );


                return false;
            }


            rect =
                ConvertRect(
                    native
                );


            return true;

#else

            return false;

#endif
        }
        
        
        public bool SetPhysicalBounds(
            int x,
            int y,
            int width,
            int height)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(SetPhysicalBounds)))
            {
                return false;
            }


            if (width <= 0 ||
                height <= 0)
            {
                Debug.LogError(
                    "[WindowsWindowService] " +
                    "Invalid physical bounds: " +
                    $"{x},{y},{width},{height}"
                );


                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_SetWindowBounds(
                        x,
                        y,
                        width,
                        height
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    "DP_SetWindowBounds"
                );


                return false;
            }


            return true;

#else

            return false;

#endif
        }
        
        
        public bool TryGetCursorPosition(
            out WindowPoint point)
        {
            point =
                default;


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(TryGetCursorPosition)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_GetCursorPosition(
                        out NativePoint native
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    "DP_GetCursorPosition"
                );


                return false;
            }


            point =
                new WindowPoint(
                    native.X,
                    native.Y
                );


            return true;

#else

            return false;

#endif
        }
        
        
        public bool IsPointOnAnyMonitor(
            int x,
            int y)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!IsInitialized)
            {
                return false;
            }


            return
                WindowsNativeMethods
                    .DP_IsPointOnAnyMonitor(
                        x,
                        y
                    )
                != 0;

#else

            return false;

#endif
        }
// ======================================================
// Window Drag
// ======================================================

        public bool BeginWindowDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(BeginWindowDrag)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_BeginWindowDrag();


            if (result == 0)
            {
                LogNativeFailure(
                    "DP_BeginWindowDrag"
                );


                return false;
            }


            return true;

#else

            return false;

#endif
        }
        // ======================================================
// Click Through
// ======================================================

        public bool SetClickThrough(
            bool enabled)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(SetClickThrough)))
            {
                return false;
            }


            int result =
                WindowsNativeMethods
                    .DP_SetClickThrough(
                        enabled ? 1 : 0
                    );


            if (result == 0)
            {
                LogNativeFailure(
                    $"DP_SetClickThrough({enabled})"
                );


                return false;
            }


            return true;

#else

            return false;

#endif
        }
        
    }
}