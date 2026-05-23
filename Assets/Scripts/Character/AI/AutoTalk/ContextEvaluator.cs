using UnityEngine;

public static class ContextEvaluator
{
    public static bool IsInteresting(string title)
    {
        if (string.IsNullOrEmpty(title))
            return false;

        if (string.IsNullOrEmpty(title) || title.Length < 2) return false;

        // 过滤掉完全没意义的系统词
        string[] blackList = { "Task Switching", "Settings", "Microsoft Text Input", "Default IME","log"};
        foreach (var item in blackList) {
            if (title.Contains(item)) return false;
        }
        
        string[] usefulList = { "-", "Bilibili", "YouTube", "Steam","QQ","wechat"};
        foreach (var item in usefulList) {
            if (title.Contains(item)) return true;
        }

        return false;
    }
}