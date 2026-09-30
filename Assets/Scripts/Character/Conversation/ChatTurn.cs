using System;

/// <summary>
/// 表示一次完整的用户对话轮次。
///
/// ChatTurn 在创建之后保持不可变：
/// 它固定了本轮请求的 TurnId、用户消息 MessageId、
/// Session 身份以及用户输入。
///
/// 后续异步流程必须围绕同一个 ChatTurn 执行，
/// 而不是在执行过程中重新读取 GlobalSession。
/// </summary>
public sealed class ChatTurn
{
    /// <summary>
    /// 整个对话轮次的唯一 ID。
    ///
    /// 用于关联搜索、LLM 请求、日志、取消和最终结果。
    /// 它不是数据库 ChatMessage 的 MessageId。
    /// </summary>
    public string TurnId { get; }

    /// <summary>
    /// 本轮用户输入对应的 ChatMessage.MessageId。
    ///
    /// 提前创建这个 ID，
    /// 之后 ChatContextBuilder 可以准确排除
    /// “本轮已经落库的用户消息”，避免重复加入 Prompt。
    /// </summary>
    public string UserMessageId { get; }

    /// <summary>
    /// 本轮请求开始时捕获的会话快照。
    ///
    /// 请求执行过程中，即使 GlobalSession 改变，
    /// 这里的身份也不会改变。
    /// </summary>
    public SessionSnapshot Session { get; }

    /// <summary>
    /// 用户本轮输入。
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Turn 创建时间。
    ///
    /// 当前主要用于调试、日志和后续性能分析，
    /// 不用它决定队列顺序。
    /// </summary>
    public long CreatedAtTicks { get; }

    private ChatTurn(
        string turnId,
        string userMessageId,
        SessionSnapshot session,
        string input,
        long createdAtTicks)
    {
        TurnId = turnId;
        UserMessageId = userMessageId;
        Session = session;
        Input = input;
        CreatedAtTicks = createdAtTicks;
    }

    /// <summary>
    /// 创建一轮新的用户对话。
    ///
    /// SessionSnapshot 由调用方传入，
    /// ChatTurn 本身不主动读取 GlobalSession。
    /// </summary>
    public static ChatTurn Create(
        SessionSnapshot session,
        string input)
    {
        if (!session.IsLoggedIn)
        {
            throw new InvalidOperationException(
                "无法创建 ChatTurn：当前 SessionSnapshot 未登录。"
            );
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                "无法创建 ChatTurn：用户输入不能为空。",
                nameof(input)
            );
        }

        return new ChatTurn(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            session,
            input,
            DateTime.Now.Ticks
        );
    }
}