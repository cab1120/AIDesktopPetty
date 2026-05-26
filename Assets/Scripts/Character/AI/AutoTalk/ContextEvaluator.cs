using System.Collections.Generic;
using UnityEngine;

public static class ContextEvaluator
{
    // 1. 进程名黑名单：这些进程是系统组件，绝对不聊
    private static readonly HashSet<string> ProcessBlacklist = new HashSet<string>
    {
        "explorer", "taskhostw", "shellexperiencehost", "searchhost", 
        "applicationframehost", "systemsettings", "textinputhost", 
        "taskmgr", "ctfmon", "lsass", "csrss", "startmenuexperiencehost"
    };

    // 2. 窗口标题黑名单：即使进程合法，这些标题也太无聊
    private static readonly string[] TitleBlacklist = { 
        "任务管理器", "设置", "Program Manager", "桌面", "新建文件夹" 
    };
    public static bool IsInteresting(string title, string processName)
    {
        if (string.IsNullOrEmpty(title) || title.Length < 2) return false;

        // 统一转小写进行比对
        string procLower = processName.ToLower();

        // A. 排除系统进程
        if (ProcessBlacklist.Contains(procLower)) return false;

        // B. 排除无意义标题
        foreach (var blackTitle in TitleBlacklist)
        {
            if (title.Contains(blackTitle)) return false;
        }

        // C. 剩下的默认全都要（无论是游戏、音乐、还是网页）
        return true;
    }
}