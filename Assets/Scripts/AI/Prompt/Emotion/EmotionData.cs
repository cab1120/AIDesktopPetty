using System;

[Serializable]
public class EmotionData
{
    public EmotionType CurrentEmotion;

    // 0~1
    public float Intensity;

    // 持续时间
    public int RemainingMinutes;

    // 时间戳
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