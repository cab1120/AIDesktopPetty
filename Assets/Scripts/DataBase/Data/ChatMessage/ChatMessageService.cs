using System.Collections.Generic;
using UnityEngine;

public static class ChatMessageService
{

    private const int MaxMessageCount = 100; 
    // 调试阶段 5，后期改成 100

    public static void SaveUserMessage(string content)
    {
        SaveMessage("User", content);
    }

    public static void SaveAssistantMessage(string content)
    {
        SaveMessage("Assistant", content);
    }

    private static void SaveMessage(string sender, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        ChatMessageRepository.AddMessage(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            sender,
            content,
            null,
            GlobalSession.CurrentUserName,
            GlobalSession.CurrentCharacterName
        );

        ChatMessageRepository.TrimOldMessages(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            MaxMessageCount
        );

        Debug.Log($"聊天记录已保存：{sender} / {content}");
    }

    public static List<ChatMessageData> Search(ChatMessageSearchCondition condition)
    {
        if (condition == null)
        {
            condition = new ChatMessageSearchCondition();
        }

        /*if (string.IsNullOrEmpty(condition.UserId))
        {
            condition.UserId =GlobalSession.CurrentUserId;
        }

        if (string.IsNullOrEmpty(condition.CharacterId))
        {
            condition.CharacterId = GlobalSession.CurrentCharacterId;
        }*/

        return ChatMessageRepository.SearchMessages(condition);
    }

    public static List<ChatMessageData> GetRecent(int limit)
    {
        return ChatMessageRepository.GetRecentMessages(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            limit
        );
    }

    public static void DeleteSelected(List<string> messageIds)
    {
        ChatMessageRepository.DeleteMessages(messageIds);
    }

    public static int Count()
    {
        return ChatMessageRepository.Count(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId
        );
    }
}