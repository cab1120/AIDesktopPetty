using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindowDragHandler :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    const int WM_NCLBUTTONDOWN = 0xA1;
    const int HTCAPTION = 0x2;
#endif

    public float dragThreshold = 10f;
    private Vector2 startMousePos;
    private bool windowWasDragged = false;
    private CustomButtonClicker customButton;

    void Awake()
    {
        customButton = GetComponent<CustomButtonClicker>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        startMousePos = eventData.position;
        windowWasDragged = false;
        if (customButton) customButton.OnPointerDownVisual();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowWasDragged) return;

        if (Vector2.Distance(startMousePos, eventData.position) > dragThreshold)
        {
            windowWasDragged = true;

            // 先还原视觉，再进入阻塞
            if (customButton) customButton.OnPointerUpVisual();

#if UNITY_STANDALONE_WIN
            IntPtr hwnd = GetActiveWindow();

            // 提前清理 EventSystem 拖拽状态，避免 Unity 侧的悬挂引用
            eventData.pointerDrag = null;
            eventData.dragging = false;

            // ReleaseCapture 让 Windows 接管鼠标捕获
            ReleaseCapture();

            // ⚠️ 这里会同步阻塞，直到用户松开鼠标拖拽结束才返回
            // 在此期间 Unity 收不到任何鼠标消息，导致输入状态不同步
            SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, 0);

            // ---- SendMessage 返回，拖拽已结束 ----

            // 重新激活窗口焦点
            SetForegroundWindow(hwnd);
            SetFocus(hwnd);

            // 关键：延迟到下一帧重置输入系统
            // 必须在 SendMessage 返回后才有意义
            StartCoroutine(ResetInputNextFrame());
#endif
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!windowWasDragged)
        {
            if (customButton)
            {
                customButton.OnPointerUpVisual();
                customButton.PerformClick();
            }
        }
    }

#if UNITY_STANDALONE_WIN
    private IEnumerator ResetInputNextFrame()
    {
        // 等一帧，让 Unity 的消息泵先跑一次
        yield return null;

        // 1. 重置所有轴输入（包括鼠标按键的内部状态）
        Input.ResetInputAxes();

        // 2. 重启 EventSystem，强迫它重新扫描当前鼠标状态
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            es.enabled = false;
            yield return null; // 禁用状态保持一帧，确保清空完成
            es.enabled = true;
        }
    }
#endif
}