using System.Collections.Generic;
using UnityEngine;

public static class ChatMessageService
{
    public const string UserId = "DefaultUser";
    public const string UserName = "默认用户";

    public const string CharacterId = "DefaultCharacter";
    public const string CharacterName = "酒寄彩叶";

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
            UserId,
            CharacterId,
            sender,
            content,
            null,
            UserName,
            CharacterName
        );

        ChatMessageRepository.TrimOldMessages(
            UserId,
            CharacterId,
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
            condition.UserId = UserId;
        }

        if (string.IsNullOrEmpty(condition.CharacterId))
        {
            condition.CharacterId = CharacterId;
        }

        return ChatMessageRepository.SearchMessages(condition);
    }

    public static List<ChatMessageData> GetRecent(int limit)
    {
        return ChatMessageRepository.GetRecentMessages(
            UserId,
            CharacterId,
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
            UserId,
            CharacterId
        );
    }
}