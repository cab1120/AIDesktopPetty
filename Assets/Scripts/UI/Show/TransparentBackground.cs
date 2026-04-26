using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class TransparentBackground : MonoBehaviour
{
#if UNITY_STANDALONE_WIN

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(
        IntPtr hWnd,
        int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(
        IntPtr hwnd,
        uint crKey,
        byte bAlpha,
        uint dwFlags);

    const int GWL_EXSTYLE = -20;

    const uint WS_EX_LAYERED = 0x80000;

    const uint LWA_COLORKEY = 0x1;

#endif

    void Start()
    {
#if UNITY_STANDALONE_WIN

        IntPtr hwnd = GetActiveWindow();

        uint style = GetWindowLong(hwnd, GWL_EXSTYLE);

        style |= WS_EX_LAYERED;

        SetWindowLong(hwnd, GWL_EXSTYLE, style);

        // 黑色变透明
        SetLayeredWindowAttributes(
            hwnd,
            0x00010101,
            0,
            LWA_COLORKEY
        );

#endif
    }
}