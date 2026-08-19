using UnityEngine;

using Platform.Windows;


public sealed class TransparentBackground
    : MonoBehaviour
{
    [SerializeField]
    private bool transparentOnStartup =
        true;


    private void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        ApplyTransparency(
            transparentOnStartup
        );

#endif
    }


    /// <summary>
    /// 设置 Windows 桌宠窗口是否使用
    /// DWM 透明背景支持。
    ///
    /// 这里只表达 Unity 侧需求，
    /// 不包含任何 Win32 / DWM 实现。
    /// </summary>
    public void ApplyTransparency(
        bool enabled)
    {
        IWindowService windowService =
            WindowsPlatformBootstrap
                .WindowService;


        if (windowService == null)
        {
            Debug.LogError(
                "[TransparentBackground] " +
                "WindowService is not available."
            );

            return;
        }


        if (!windowService.IsInitialized)
        {
            Debug.LogError(
                "[TransparentBackground] " +
                "WindowService is not initialized."
            );

            return;
        }


        bool success =
            windowService
                .SetTransparentBackground(
                    enabled
                );


        if (!success)
        {
            Debug.LogError(
                "[TransparentBackground] " +
                "Failed to set transparent " +
                $"background={enabled}."
            );
        }
    }
}