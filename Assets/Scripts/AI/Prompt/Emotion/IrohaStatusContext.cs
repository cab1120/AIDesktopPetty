using System;

public static class IrohaStatusContext
{
    // 长期心境：决定一段时间内的情绪基调
    public static string LongTermMood => GetLongTermMoodByDay();

    // 自动计算体力值 (0~1)：早晨8点最高，凌晨4点最低
    public static float GetPhysicalStrength()
    {
        int hour = DateTime.Now.Hour;
        // 简单的三角函数模拟体力曲线：白天高，深夜低
        float strength = (float)Math.Cos((hour - 14) * Math.PI / 12); 
        return UnityEngine.Mathf.Clamp((strength + 1) / 2, 0.2f, 1.0f);
    }

    private static string GetLongTermMoodByDay()
    {
        DayOfWeek day = DateTime.Now.DayOfWeek;
        if (day == DayOfWeek.Monday) return "略显压抑（周一总是有些沉重）";
        if (day == DayOfWeek.Friday) return "隐约的期待（周末将至）";
        if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday) return "松弛且平静";
        return "平稳的日常工作节奏";
    }
}