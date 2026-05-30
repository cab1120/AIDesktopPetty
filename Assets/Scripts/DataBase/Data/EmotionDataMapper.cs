using System;

public static class EmotionDataMapper
{
    public static EmotionRecord ToRecord(EmotionData data, string userId = "DefaultUser", string characterId = "DefaultCharacter")
    {
        if (data == null)
            return null;
        
        long nowTicks = DateTime.Now.Ticks;

        if (data.LastUpdateTicks <= 0)
            data.LastUpdateTicks = nowTicks;

        return new EmotionRecord
        {
            EmotionId = Guid.NewGuid().ToString(),

            UserId = userId,

            CharacterId = characterId,

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
            CurrentEmotion = parsedEmotion,
            Intensity = record.Intensity,
            RemainingMinutes = record.RemainingMinutes,
            LastUpdateTicks = record.CreatedAtTicks
        };
    }
}