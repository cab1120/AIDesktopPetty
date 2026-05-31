using System;
using System.Collections.Generic;
using System.Linq;

public static class ChatMessageRepository
{
    public static void AddMessage(ChatMessageData message)
    {
        if (message == null)
            return;

        DatabaseManager.Initialize();
        DatabaseManager.Connection.Insert(message);
    }

    public static void AddMessage(
        string userId,
        string characterId,
        string sender,
        string content,
        string emotionId,
        string userName,
        string characterName)
    {
        DatabaseManager.Initialize();

        ChatMessageData message = new ChatMessageData
        {
            MessageId = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName,
            CharacterId = characterId,
            CharacterName = characterName,
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
            .Where(m =>
                m.UserId == userId &&
                m.CharacterId == characterId)
            .OrderByDescending(m => m.CreatedAtTicks)
            .Take(limit)
            .ToList();
    }

    public static List<ChatMessageData> SearchMessages(ChatMessageSearchCondition condition)
    {
        DatabaseManager.Initialize();

        IEnumerable<ChatMessageData> query =
            DatabaseManager.Connection.Table<ChatMessageData>();

        if (!string.IsNullOrEmpty(condition.UserId))
        {
            query = query.Where(m => m.UserId == condition.UserId);
        }

        if (!string.IsNullOrEmpty(condition.CharacterId))
        {
            query = query.Where(m => m.CharacterId == condition.CharacterId);
        }

        if (!string.IsNullOrEmpty(condition.UserNameKeyword))
        {
            query = query.Where(m =>
                !string.IsNullOrEmpty(m.UserName) &&
                m.UserName.Contains(condition.UserNameKeyword));
        }

        if (!string.IsNullOrEmpty(condition.CharacterNameKeyword))
        {
            query = query.Where(m =>
                !string.IsNullOrEmpty(m.CharacterName) &&
                m.CharacterName.Contains(condition.CharacterNameKeyword));
        }

        if (!string.IsNullOrEmpty(condition.ContentKeyword))
        {
            query = query.Where(m =>
                !string.IsNullOrEmpty(m.Content) &&
                m.Content.Contains(condition.ContentKeyword));
        }

        if (!string.IsNullOrEmpty(condition.Sender))
        {
            query = query.Where(m => m.Sender == condition.Sender);
        }

        if (condition.StartTicks > 0)
        {
            query = query.Where(m => m.CreatedAtTicks >= condition.StartTicks);
        }

        if (condition.EndTicks > 0)
        {
            query = query.Where(m => m.CreatedAtTicks <= condition.EndTicks);
        }

        return query
            .OrderByDescending(m => m.CreatedAtTicks)
            .ToList();
    }

    public static void DeleteMessages(List<string> messageIds)
    {
        if (messageIds == null || messageIds.Count == 0)
            return;

        DatabaseManager.Initialize();

        foreach (string id in messageIds)
        {
            DatabaseManager.Connection.Execute(
                "DELETE FROM ChatMessage WHERE MessageId = ?",
                id
            );
        }
    }

    public static void TrimOldMessages(
        string userId,
        string characterId,
        int keepCount)
    {
        DatabaseManager.Initialize();

        List<ChatMessageData> oldMessages =
            DatabaseManager.Connection
                .Table<ChatMessageData>()
                .Where(m =>
                    m.UserId == userId &&
                    m.CharacterId == characterId)
                .OrderByDescending(m => m.CreatedAtTicks)
                .Skip(keepCount)
                .ToList();

        foreach (ChatMessageData message in oldMessages)
        {
            DatabaseManager.Connection.Delete(message);
        }
    }

    public static int Count(
        string userId,
        string characterId)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<ChatMessageData>()
            .Where(m =>
                m.UserId == userId &&
                m.CharacterId == characterId)
            .Count();
    }
}