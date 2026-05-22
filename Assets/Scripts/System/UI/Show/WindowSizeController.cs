using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class WindowSizeController : MonoBehaviour
{
#if UNITY_STANDALONE_WIN

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

#endif

    public int expandedWidth = 432;
    public int expandedHeight = 768;

    public int collapsedWidth = 180;
    public int collapsedHeight = 250;
    
    public CanvasScaler canvasScaler;

    const uint SWP_NOMOVE = 0x0002;//位置不移动
    
    const uint SWP_NOZORDER = 0x0004;   // 忽略窗口层级顺序参数
    const uint SWP_SHOWWINDOW = 0x0040; // 窗口大小改变后强制显示窗口
    
    IntPtr hwnd;

    void Start()
    {
#if UNITY_STANDALONE_WIN

        hwnd = GetActiveWindow();
#endif

        CollapseFirst();
    }

    

    public void ToggleWindow(bool isExpanded)
    {
        if (isExpanded)
            Expand();
        else
            Collapse();
    }

    void Expand()
    {
        canvasScaler.enabled = true;
#if UNITY_STANDALONE_WIN

        SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            0,
            0,
            expandedWidth,
            expandedHeight,
            SWP_NOMOVE
        );

#endif
    }

    void Collapse()
    {
        canvasScaler.enabled = false;
#if UNITY_STANDALONE_WIN

        SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            0,
            0,
            collapsedWidth,
            collapsedHeight,
            SWP_NOMOVE
        );

#endif
    }
    private void CollapseFirst()
    {
        canvasScaler.enabled = false;
#if UNITY_STANDALONE_WIN
     
        SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            100,
            100,
            collapsedWidth,
            collapsedHeight,
            0
        );
     
#endif
    }
}