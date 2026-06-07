using System;
using System.Collections.Generic;
using System.Linq;

public static class InteractionEventRepository
{
    private const int MaxEventsPerUserCharacter = 300;

    public static void AddEvent(
        string userId,
        string characterId,
        string eventType,
        string eventSource,
        string contextKey,
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
            EventSource = eventSource,
            ContextKey = contextKey,
            Description = description,
            EmotionImpact = emotionImpact,
            FavorabilityImpact = favorabilityImpact,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(data);

        TrimOldEvents(userId, characterId);
    }

    public static bool HasRecentEvent(
        string userId,
        string characterId,
        string eventType,
        string contextKey,
        TimeSpan timeWindow)
    {
        DatabaseManager.Initialize();

        long thresholdTicks = DateTime.Now.Subtract(timeWindow).Ticks;

        return DatabaseManager.Connection
            .Table<InteractionEventData>()
            .Any(e =>
                e.UserId == userId &&
                e.CharacterId == characterId &&
                e.EventType == eventType &&
                e.ContextKey == contextKey &&
                e.CreatedAtTicks >= thresholdTicks
            );
    }

    private static void TrimOldEvents(string userId, string characterId)
    {
        var allEvents = DatabaseManager.Connection
            .Table<InteractionEventData>()
            .Where(e =>
                e.UserId == userId &&
                e.CharacterId == characterId
            )
            .OrderByDescending(e => e.CreatedAtTicks)
            .ToList();

        if (allEvents.Count <= MaxEventsPerUserCharacter)
            return;

        List<InteractionEventData> oldEvents = allEvents
            .Skip(MaxEventsPerUserCharacter)
            .ToList();

        foreach (var oldEvent in oldEvents)
        {
            DatabaseManager.Connection.Delete(oldEvent);
        }
    }
}