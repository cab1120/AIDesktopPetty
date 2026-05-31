using System;
using System.Collections.Generic;

[Serializable]
public class EmotionData
{
    // --- 预处理：为数据库新增字段 ---
    public string Id = Guid.NewGuid().ToString(); // 每一条情绪记录的唯一ID
    public string UserId = "DefaultUser";       // 预留多用户支持
    public string CharacterId { get; set; } = "DefaultCharacter";

    
    public EmotionType CurrentEmotion;

    // 0~1
    public float Intensity;

    // 持续时间
    public int RemainingMinutes;

    // 开始时间
    public long LastUpdateTicks;

    public DateTime GetLastUpdateTime()
    {
        return new DateTime(LastUpdateTicks);
    }

    public void SetLastUpdate(DateTime time)
    {
        LastUpdateTicks = time.Ticks;
    }
}

