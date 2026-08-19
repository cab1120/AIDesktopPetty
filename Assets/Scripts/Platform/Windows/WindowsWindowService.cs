using UnityEngine;

using Platform.Windows.Native;


namespace Platform.Windows
{
    /// <summary>
    /// Windows 平台窗口服务。
    ///
    /// 负责把 Unity 业务语义转换为
    /// Native Plugin 调用。
    /// </summary>
    internal sealed class WindowsWindowService
        : IWindowService
    {
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
            }


            return _initialized;

#else

            Debug.Log(
                "[WindowsWindowService] " +
                "Native Windows service is disabled " +
                "inside Unity Editor or on " +
                "non-Windows platforms."
            );


            _initialized =
                false;


            return false;

#endif
        }


        // ======================================================
        // Window Style
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
        // Transparency / DWM
        // ======================================================

        public bool SetTransparentBackground(
            bool enabled)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!EnsureInitialized(
                    nameof(SetTransparentBackground)))
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
                    $"DP_SetTransparentBackground({enabled})"
                );

                return false;
            }


            return true;

#else

            return false;

#endif
        }


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


        // ======================================================
        // Diagnostics
        // ======================================================

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