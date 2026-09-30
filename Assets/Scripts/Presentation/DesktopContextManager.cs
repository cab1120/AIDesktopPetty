using System;
using Platform.Windows;
using UnityEngine;

public class DesktopContextManager : MonoBehaviour
{
    
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

    void CheckWindow()
    {
        if (WindowsForegroundContextService.TryGetCurrent(out ForegroundContext context))
        {
            string activeTitle = context.Title;
            string procName = context.ProcessName;

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
                        OnWindowChanged?.Invoke(activeTitle,procName);
                    }
                    
                }
            }
        }

    }
    //测试用
    
}
