using System;
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

    // 新增：强制设置输入焦点
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

// 在 WindowDragHandler.cs 中修改 OnDrag 的修复部分

#if UNITY_STANDALONE_WIN
            IntPtr hwnd = GetActiveWindow();
            ReleaseCapture();

            // 发送消息，阻塞线程
            SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, 0);

            // --- 真正的强制重置修复 ---
            
            // 1. 恢复焦点
            SetForegroundWindow(hwnd);
            
            // 2. 核心：重置 EventSystem (这是解决“必须点一下”的终极手段)
            // 禁用再瞬间开启，会强迫 Unity 重新扫描所有鼠标状态，清除“悬挂”的按下事件
            EventSystem es = EventSystem.current;
            if (es != null)
            {
                es.enabled = false;
                es.enabled = true;
            }
            
            ExecuteEvents.ExecuteHierarchy(gameObject, eventData, ExecuteEvents.pointerUpHandler);
            
            // 3. 强制清空 EventData 的状态
            eventData.pointerDrag = null;
            eventData.dragging = false;
            
            // 4. 还原视觉
            if (customButton) customButton.OnPointerUpVisual();
#endif
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 只有在没拖拽的情况下才触发模拟点击
        if (!windowWasDragged)
        {
            if (customButton)
            {
                customButton.OnPointerUpVisual();
                customButton.PerformClick();
            }
        }
    }
}