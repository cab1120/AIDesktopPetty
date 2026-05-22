using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickThroughController : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    const int GWL_EXSTYLE = -20;
    const int WS_EX_LAYERED = 0x80000;
    const int WS_EX_TRANSPARENT = 0x20;
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;

    private IntPtr hwnd;
    private bool isClickThrough = false;

    // 关键：手动指定检测用的 Canvas
    public GraphicRaycaster raycaster;
    private PointerEventData pointerData;
    private List<RaycastResult> results;

    void Start()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        hwnd = GetActiveWindow();

        // 初始设置：置顶并开启分层样式
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_LAYERED);
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
#endif
        pointerData = new PointerEventData(EventSystem.current);
        results = new List<RaycastResult>();

        if (raycaster == null)
            raycaster = GetComponent<GraphicRaycaster>();
    }

    void Update()
    {
        // 核心检测逻辑：手动射线检测
        bool isOverUI = CheckIfMouseOverUI();

        if (isOverUI && isClickThrough)
        {
            DisableClickThrough();
        }
        else if (!isOverUI && !isClickThrough)
        {
            EnableClickThrough();
        }
    }

    // 不依赖窗口焦点的 UI 检测方法
    private bool CheckIfMouseOverUI()
    {
        if (raycaster == null) return false;

        // 设置当前鼠标位置
        pointerData.position = Input.mousePosition;
        results.Clear();
        
        // 强制对 UI 进行射线检测（即使窗口没有焦点也能工作）
        raycaster.Raycast(pointerData, results);
        
        return results.Count > 0;
    }

    void EnableClickThrough()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
#endif
        isClickThrough = true;
    }

    void DisableClickThrough()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
#endif
        isClickThrough = false;
    }
}