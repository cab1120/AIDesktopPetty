using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public static class ChatContextBuilder
{
    private const int ContextMessageCount = 8;

    public static JArray BuildMessages(
        string systemPrompt,
        ChatTurn turn)
    {
        if (turn == null)
        {
            throw new ArgumentNullException(
                nameof(turn)
            );
        }

        JArray messages =
            new JArray();

        messages.Add(
            new JObject
            {
                { "role", "system" },
                { "content", systemPrompt }
            }
        );

        /*
         * 当前用户消息已经在真正执行 Turn 时落库。
         *
         * 因此读取历史时必须明确排除
         * turn.UserMessageId。
         *
         * 不能按 Content 去重，
         * 因为用户可以真的连续发送相同文本。
         */
        List<ChatMessageData> history =
            ChatMessageRepository
                .GetRecentMessagesExcluding(
                    turn.Session.UserId,
                    turn.Session.CharacterId,
                    turn.UserMessageId,
                    ContextMessageCount
                );

        history = history
            .OrderBy(m => m.CreatedAtTicks)
            .ToList();

        foreach (ChatMessageData msg in history)
        {
            string role =
                msg.Sender == "Assistant"
                    ? "assistant"
                    : "user";

            messages.Add(
                new JObject
                {
                    { "role", role },
                    { "content", msg.Content }
                }
            );
        }

        /*
         * 当前输入只在这里明确加入一次。
         */
        messages.Add(
            new JObject
            {
                { "role", "user" },
                { "content", turn.Input }
            }
        );

        return messages;
    }


    /*
     * 暂时保留旧接口，
     * 避免其他尚未迁移的代码编译失败。
     *
     * 新 Conversation 链禁止再使用它。
     */
    public static JArray BuildMessages(
        string systemPrompt,
        string currentUserMessage,
        string userId,
        string characterId)
    {
        JArray messages =
            new JArray();

        messages.Add(
            new JObject
            {
                { "role", "system" },
                { "content", systemPrompt }
            }
        );

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
            string role =
                msg.Sender == "Assistant"
                    ? "assistant"
                    : "user";

            messages.Add(
                new JObject
                {
                    { "role", role },
                    { "content", msg.Content }
                }
            );
        }

        messages.Add(
            new JObject
            {
                { "role", "user" },
                { "content", currentUserMessage }
            }
        );

        return messages;
    }
}