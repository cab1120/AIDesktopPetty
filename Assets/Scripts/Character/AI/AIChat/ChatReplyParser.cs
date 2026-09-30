using System;
using Newtonsoft.Json.Linq;

/// <summary>模型协议只在这里转成对话终态，错误体不能成为正常 Assistant 消息。</summary>
public static class ChatReplyParser
{
    public static ChatReplyResult Parse(string responseBody)
    {
        try
        {
            JObject body = JObject.Parse(responseBody);
            string content = body["choices"]?[0]?["message"]?["content"]?.ToString();
            return string.IsNullOrWhiteSpace(content)
                ? ChatReplyResult.Failure(ChatReplyFailureReason.EmptyResponse, "模型返回了空回复")
                : ChatReplyResult.Success(content);
        }
        catch (Exception)
        {
            return ChatReplyResult.Failure(ChatReplyFailureReason.InvalidResponse, "回复解析失败");
        }
    }
}
