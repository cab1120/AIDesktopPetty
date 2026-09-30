public enum ChatReplyStatus
{
    Success,
    Failure,
    Cancelled
}

public enum ChatReplyFailureReason
{
    None,

    // 网络层
    Timeout,
    Network,
    HttpError,

    // 返回内容
    InvalidResponse,
    EmptyResponse,

    // 联网搜索链
    SearchDecisionFailed,
    SearchFailed,

    // 调用或配置问题
    InvalidRequest,
    Unknown
}

public sealed class ChatReplyResult
{
    public ChatReplyStatus Status { get; }

    public ChatReplyFailureReason FailureReason { get; }

    public string Text { get; }

    /// <summary>
    /// 面向日志/诊断的信息。
    /// UI 不应该直接把这里的技术错误显示给用户。
    /// </summary>
    public string Error { get; }

    public bool IsSuccess =>
        Status == ChatReplyStatus.Success;

    public bool IsFailure =>
        Status == ChatReplyStatus.Failure;

    public bool IsCancelled =>
        Status == ChatReplyStatus.Cancelled;

    private ChatReplyResult(
        ChatReplyStatus status,
        ChatReplyFailureReason failureReason,
        string text,
        string error)
    {
        Status = status;
        FailureReason = failureReason;
        Text = text;
        Error = error;
    }

    public static ChatReplyResult Success(string text)
    {
        return new ChatReplyResult(
            ChatReplyStatus.Success,
            ChatReplyFailureReason.None,
            text,
            null);
    }

    public static ChatReplyResult Failure(
        ChatReplyFailureReason reason,
        string error)
    {
        return new ChatReplyResult(
            ChatReplyStatus.Failure,
            reason,
            null,
            error);
    }

    public static ChatReplyResult Cancelled(string reason = null)
    {
        return new ChatReplyResult(
            ChatReplyStatus.Cancelled,
            ChatReplyFailureReason.None,
            null,
            reason);
    }
}