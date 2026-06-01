using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public static class ChatContextBuilder
{
    private const int ContextMessageCount = 8;

    public static JArray BuildMessages(
        string systemPrompt,
        string currentUserMessage,
        string userId,
        string characterId)
    {
        JArray messages = new JArray();

        messages.Add(new JObject
        {
            { "role", "system" },
            { "content", systemPrompt }
        });

        List<ChatMessageData> history =
            ChatMessageRepository.GetRecentMessages(
                userId,
                characterId,
                ContextMessageCount
            );

        history = history
            .OrderBy(m => m.CreatedAtTicks)
            .ToList();

        foreach (ChatMessageData msg in history)
        {
            string role = msg.Sender == "Assistant"
                ? "assistant"
                : "user";

            messages.Add(new JObject
            {
                { "role", role },
                { "content", msg.Content }
            });
        }

        messages.Add(new JObject
        {
            { "role", "user" },
            { "content", currentUserMessage }
        });

        return messages;
    }
}