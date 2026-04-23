using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class BorderlessWindow : MonoBehaviour
{
#if UNITY_STANDALONE_WIN

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(
        IntPtr hWnd,
        int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_FRAMECHANGED = 0x0020; // 核心：强制刷新框架样式
    const uint SWP_NOZORDER = 0x0004;
#endif

    void Awake()
    {
#if UNITY_STANDALONE_WIN

        IntPtr hwnd = GetActiveWindow();
        int style = GetWindowLong(hwnd, -16);
    
        style &= ~(0x00C00000 | 0x00800000 | 0x00400000); // 一次性去掉边框和标题栏
    
        SetWindowLong(hwnd, -16, style);
    
        // 关键修复：通知系统窗口样式已变，请立刻刷新
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

#endif
    }
}