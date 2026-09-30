using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ChatContextTextBuilder
{
    public static string BuildRecentContextText(
        string userId,
        string characterId,
        int limit,
        string excludedMessageId = null)
    {
        List<ChatMessageData> history = string.IsNullOrEmpty(excludedMessageId)
            ? ChatMessageRepository.GetRecentMessages(userId, characterId, limit)
            : ChatMessageRepository.GetRecentMessagesExcluding(
                userId, characterId, excludedMessageId, limit);

        history = history
            .OrderBy(m => m.CreatedAtTicks)
            .ToList();

        StringBuilder sb = new StringBuilder();

        foreach (ChatMessageData msg in history)
        {
            string speaker = msg.Sender == "Assistant"
                ? "AI"
                : "User";

            sb.AppendLine($"{speaker}: {msg.Content}");
        }

        return sb.ToString();
    }
}
