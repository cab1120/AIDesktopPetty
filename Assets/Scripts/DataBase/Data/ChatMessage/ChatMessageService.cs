using System;
using System.Collections.Generic;
using UnityEngine;

public static class ChatMessageService
{
    private const int MaxMessageCount = 100;

    // ============================
    // 新聊天链使用的接口
    // ============================

    public static ChatMessageData SaveUserMessage(
        ChatTurn turn)
    {
        if (turn == null)
            return null;

        return SaveMessage(
            turn.Session,
            "User",
            turn.Input,
            turn.UserMessageId
        );
    }

    public static ChatMessageData SaveAssistantMessage(
        SessionSnapshot session,
        string content)
    {
        return SaveMessage(
            session,
            "Assistant",
            content,
            null
        );
    }

    private static ChatMessageData SaveMessage(
        SessionSnapshot session,
        string sender,
        string content,
        string messageId)
    {
        if (!GlobalSession.IsCurrent(session))
            return null;

        if (string.IsNullOrWhiteSpace(content))
            return null;

        ChatMessageData message =
            new ChatMessageData
            {
                MessageId =
                    string.IsNullOrEmpty(messageId)
                        ? Guid.NewGuid().ToString()
                        : messageId,

                UserId = session.UserId,
                UserName = session.UserName,

                CharacterId = session.CharacterId,
                CharacterName = session.CharacterName,

                EmotionId = null,

                Sender = sender,
                Content = content,

                // 注意：
                // 使用真正“执行落库”的时间。
                // 不使用 ChatTurn.CreatedAtTicks。
                CreatedAtTicks = DateTime.Now.Ticks
            };

        ChatMessageRepository.AddMessage(message);

        ChatMessageRepository.TrimOldMessages(
            session.UserId,
            session.CharacterId,
            MaxMessageCount
        );

        Debug.Log($"聊天记录已保存：{sender} / {message.MessageId}");

        return message;
    }


    // ============================
    // 旧接口暂时保留
    // 避免项目其他地方突然编译失败
    // ============================

    public static void SaveUserMessage(string content)
    {
        SaveMessage(
            GlobalSession.CaptureSnapshot(),
            "User",
            content,
            null
        );
    }

    public static void SaveAssistantMessage(string content)
    {
        SaveMessage(
            GlobalSession.CaptureSnapshot(),
            "Assistant",
            content,
            null
        );
    }


    // ============================
    // 原有查询功能
    // ============================

    public static List<ChatMessageData> Search(
        ChatMessageSearchCondition condition)
    {
        if (condition == null)
        {
            condition =
                new ChatMessageSearchCondition();
        }

        return ChatMessageRepository
            .SearchMessages(condition);
    }

    public static List<ChatMessageData> GetRecent(
        int limit)
    {
        SessionSnapshot session = GlobalSession.CaptureSnapshot();
        if (!GlobalSession.IsCurrent(session))
            return new List<ChatMessageData>();
        return ChatMessageRepository
            .GetRecentMessages(
                session.UserId,
                session.CharacterId,
                limit
            );
    }

    public static void DeleteSelected(
        List<string> messageIds)
    {
        ChatMessageRepository
            .DeleteMessages(messageIds);
    }

    public static int Count()
    {
        SessionSnapshot session = GlobalSession.CaptureSnapshot();
        if (!GlobalSession.IsCurrent(session))
            return 0;
        return ChatMessageRepository.Count(
            session.UserId,
            session.CharacterId
        );
    }
}
