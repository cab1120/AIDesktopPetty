using UnityEngine;

using Platform.Windows;


public class BorderlessWindow : MonoBehaviour
{
    [SerializeField]
    private bool borderlessOnStartup = true;


    private void Awake()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        ApplyBorderless(
            borderlessOnStartup
        );

#endif
    }


    /// <summary>
    /// 设置桌宠窗口是否使用无边框模式。
    /// </summary>
    public void ApplyBorderless(bool enabled)
    {
        IWindowService windowService = WindowsPlatformBootstrap.WindowService;


        if (windowService == null)
        {
            Debug.LogError(
                "[BorderlessWindow] " +
                "WindowService is not available."
            );

            return;
        }


        if (!windowService.IsInitialized)
        {
            Debug.LogError(
                "[BorderlessWindow] " +
                "WindowService is not initialized."
            );

            return;
        }


        bool success = windowService.SetBorderless(enabled);


        if (!success)
        {
            Debug.LogError(
                "[BorderlessWindow] " +
                $"Failed to set borderless={enabled}."
            );
        }
    }
}