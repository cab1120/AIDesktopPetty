using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DesktopContextManager : MonoBehaviour
{
#if UNITY_STANDALONE_WIN

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);  //获取窗口名
    
    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);   //获取进程名

#endif
    
    private string lastConfirmedTitle = ""; // 真正触发了事件的窗口
    private string currentTrackingTitle = ""; // 正在“考察”中的窗口
    private float trackingStartTime = 0f;
    
    public Action<string, string> OnWindowChanged; // 传递 Title 和 ProcessName
    
    [Header("设置")]
    public float stayThreshold = 5f; // 停留多时长（秒）
    public float checkInterval = 1f; // 检查频率
    
    //测试用
    public BubbleUIManager bubbleUI;

    void Start()
    {
        InvokeRepeating(nameof(CheckWindow), 1f, checkInterval); 
    }

    const int nChars = 256;

    StringBuilder buffer = new StringBuilder(nChars);
    void CheckWindow()
    {
#if UNITY_STANDALONE_WIN

        buffer.Clear();

        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return;
        
        string procName = GetProcessName(handle);
        
        if (GetWindowText(handle, buffer, nChars) > 0)
        {
            string activeTitle = buffer.ToString();

            // 如果当前窗口和正在考察的窗口不一样，重置计时器
            if (activeTitle != currentTrackingTitle)
            {
                currentTrackingTitle = activeTitle;
                trackingStartTime = Time.time;
            }
            else
            {
                // 如果当前窗口和正在考察的一样，检查停留时间
                float stayTime = Time.time - trackingStartTime;
                
                // 停留时间超过阈值，且和上次真正触发的窗口不同
                if (stayTime >= stayThreshold && activeTitle != lastConfirmedTitle)
                {
                    if (ContextEvaluator.IsInteresting(activeTitle, procName))
                    {
                        lastConfirmedTitle = activeTitle;
                        Debug.Log($"[确认切换] 用户已进入窗口: {activeTitle}");
                        OnWindowChanged?.Invoke(activeTitle,procName);
                    }
                    
                }
            }
        }

#endif
    }
    
    string GetProcessName(IntPtr handle)
    {
        try {
            GetWindowThreadProcessId(handle, out uint pid);
            using (Process p = Process.GetProcessById((int)pid))
            {
                return p.ProcessName;
            }
        } catch { return "unknown"; }
    }
    //测试用
    
}