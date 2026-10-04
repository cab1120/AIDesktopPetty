using System;
using System.IO;
using UnityEngine;

/// <summary>应用级单一日志订阅，沿用原 AIChat.RunLog 的可读格式和输出位置。</summary>
public static class AppLogService
{
    private static bool subscribed;
    private static string logPath;

    public static void Start()
    {
        if (subscribed) return;
        logPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../run_log.txt"));
        Application.logMessageReceived += OnLog;
        subscribed = true;
    }

    public static void Stop()
    {
        if (!subscribed) return;
        Application.logMessageReceived -= OnLog;
        subscribed = false;
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        try
        {
            string entry = $"[{DateTime.Now}] [{type}] {condition}\n";
            if (type == LogType.Exception || type == LogType.Error)
                entry += stackTrace + "\n";
            File.AppendAllText(logPath, entry);
        }
        catch (Exception)
        {
            // 日志写入失败不能递归触发 Unity 日志回调。
        }
    }
}
