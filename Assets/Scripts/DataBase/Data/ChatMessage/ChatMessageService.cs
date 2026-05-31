using System.Collections.Generic;
using UnityEngine;

public static class ChatMessageService
{
    public const string DefaultUserId = "DefaultUser";
    public const string DefaultUserName = "默认用户";

    public const string DefaultCharacterId = "DefaultCharacter";
    public const string DefaultCharacterName = "酒寄彩叶";

    private const int MaxMessageCount = 5; 
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
            DefaultUserId,
            DefaultCharacterId,
            sender,
            content,
            null,
            DefaultUserName,
            DefaultCharacterName
        );

        ChatMessageRepository.TrimOldMessages(
            DefaultUserId,
            DefaultCharacterId,
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

        if (string.IsNullOrEmpty(condition.UserId))
        {
            condition.UserId = DefaultUserId;
        }

        if (string.IsNullOrEmpty(condition.CharacterId))
        {
            condition.CharacterId = DefaultCharacterId;
        }

        return ChatMessageRepository.SearchMessages(condition);
    }

    public static List<ChatMessageData> GetRecent(int limit = 5)
    {
        return ChatMessageRepository.GetRecentMessages(
            DefaultUserId,
            DefaultCharacterId,
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
            DefaultUserId,
            DefaultCharacterId
        );
    }
}