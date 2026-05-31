using System;

public static class EmotionDataMapper
{
    public static EmotionRecord ToRecord(EmotionData data)
    {
        if (data == null)
            return null;
        
        long nowTicks = DateTime.Now.Ticks;

        if (data.LastUpdateTicks <= 0)
            data.LastUpdateTicks = nowTicks;

        return new EmotionRecord
        {
            EmotionId = data.Id,

            UserId = data.UserId,

            CharacterId = data.CharacterId,

            EmotionType = data.CurrentEmotion.ToString(),
            Intensity = data.Intensity,
            RemainingMinutes = data.RemainingMinutes,
            CreatedAtTicks = data.LastUpdateTicks
        };
    }

    public static EmotionData ToData(EmotionRecord record)
    {
        if (record == null)
            return null;

        EmotionType parsedEmotion;

        if (!Enum.TryParse(record.EmotionType, out parsedEmotion))
        {
            parsedEmotion = default;
        }

        return new EmotionData
        {
            Id = record.EmotionId,
            UserId = record.UserId,
            CharacterId = record.CharacterId,
            CurrentEmotion = parsedEmotion,
            Intensity = record.Intensity,
            RemainingMinutes = record.RemainingMinutes,
            LastUpdateTicks = record.CreatedAtTicks
        };
    }
}