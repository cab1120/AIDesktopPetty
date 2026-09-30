using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>应用级单一日志订阅。仅记录类型与指纹，避免写入对话、窗口标题或密钥。</summary>
public static class AppLogService
{
    private static bool subscribed;
    private const long MaxBytes = 2 * 1024 * 1024;

    public static void Start()
    {
        if (subscribed) return;
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
            string path = Path.Combine(Application.persistentDataPath, "m0_diagnostics.log");
            if (File.Exists(path) && new FileInfo(path).Length >= MaxBytes)
            {
                string oldPath = path + ".old";
                if (File.Exists(oldPath)) File.Delete(oldPath);
                File.Move(path, oldPath);
            }
            byte[] digest;
            using (var sha = SHA256.Create())
                digest = sha.ComputeHash(Encoding.UTF8.GetBytes(condition ?? ""));
            string fingerprint = BitConverter.ToString(digest, 0, 6).Replace("-", "");
            File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + type +
                " " + fingerprint + Environment.NewLine);
        }
        catch (Exception)
        {
            // 日志写入失败不能递归触发 Unity 日志回调。
        }
    }
}
