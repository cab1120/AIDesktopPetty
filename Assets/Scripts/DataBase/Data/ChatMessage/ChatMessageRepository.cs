using System;
using System.Collections.Generic;
using System.Linq;

public static class ChatMessageRepository
{
    public static void AddMessage(
        string userId,
        string characterId,
        string sender,
        string content,
        string emotionId = null)
    {
        DatabaseManager.Initialize();

        ChatMessageData message = new ChatMessageData
        {
            UserId = userId,
            CharacterId = characterId,
            EmotionId = emotionId,
            Sender = sender,
            Content = content,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(message);
    }

    public static List<ChatMessageData> GetRecentMessages(
        string userId,
        string characterId,
        int limit)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<ChatMessageData>()
            .Where(m => m.UserId == userId && m.CharacterId == characterId)
            .OrderByDescending(m => m.CreatedAtTicks)
            .Take(limit)
            .ToList();
    }
}