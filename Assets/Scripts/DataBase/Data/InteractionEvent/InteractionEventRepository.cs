using System;

public static class InteractionEventRepository
{
    public static void AddEvent(
        string userId,
        string characterId,
        string eventType,
        string eventSource,
        string description,
        float emotionImpact,
        int favorabilityImpact)
    {
        DatabaseManager.Initialize();

        InteractionEventData data = new InteractionEventData
        {
            UserId = userId,
            CharacterId = characterId,
            EventType = eventType,
            Description = description,
            EmotionImpact = emotionImpact,
            FavorabilityImpact = favorabilityImpact,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(data);
    }
}