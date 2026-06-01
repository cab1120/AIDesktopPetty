using System;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SearchDecisionService
{
    public static IEnumerator Decide(
        string userMessage,
        string recentContext,
        Func<string, string, Action<string>, IEnumerator> rawLLMCall,
        Action<SearchDecision> callback)
    {
        SearchDecisionMode mode =
            SearchRuleFilter.JudgeByRule(userMessage);

        if (mode == SearchDecisionMode.NoSearch)
        {
            callback?.Invoke(new SearchDecision
            {
                NeedSearch = false,
                Query = "",
                Reason = "用户输入更接近日常陪伴或情绪表达，不需要展开月读空间链路。"
            });
            yield break;
        }

        if (mode == SearchDecisionMode.DirectSearch)
        {
            callback?.Invoke(new SearchDecision
            {
                NeedSearch = true,
                Query = userMessage,
                Reason = "用户明确提出需要实时信息或主动要求查询。"
            });
            yield break;
        }
        
        string decisionPrompt = BuildDecisionPrompt(recentContext);

        string rawResult = null;

        yield return rawLLMCall(
            decisionPrompt,
            userMessage,
            result => rawResult = result
        );

        SearchDecision decision = ParseDecision(rawResult);

        callback?.Invoke(decision);
    }

    private static string BuildDecisionPrompt(string recentContext)
    {
        return $@"
你是一个联网搜索决策器，不负责回答用户，只判断是否需要搜索。

你的任务：
判断用户当前输入是否需要联网搜索来避免回答错误。

需要搜索的情况：
1. 用户提到近期作品、最近新番、最近新闻、热点事件、现实人物、比赛、价格、天气、版本更新。
2. 用户直接给出一个你可能不认识的作品名、角色名、梗、专有名词。
3. 用户的问题依赖当前时间，例如“最近”“今年”“现在”“新出的”“刚更新”。
4. 用户问的是具体事实，而不是普通聊天陪伴。
5. 用户追问上一轮搜索相关内容，但本地上下文不足以回答。

不需要搜索的情况：
1. 用户只是表达情绪、疲惫、日常生活，例如“今天课好多”“我好累”“不想起床”。
2. 用户只是闲聊、撒娇、吐槽，不需要事实核验。
3. 用户问的是你们刚刚聊过、已经在本地上下文里的内容。
4. 用户希望得到安慰、建议、陪伴，而不是外部事实。

最近聊天上下文：
{recentContext}

请只输出 JSON，不要输出其他文字。

格式：
{{
  ""needSearch"": true 或 false,
  ""query"": ""如果需要搜索，给出适合搜索引擎的关键词；否则为空字符串"",
  ""reason"": ""一句话说明原因""
}}
";
    }

    private static SearchDecision ParseDecision(string raw)
    {
        SearchDecision fallback = new SearchDecision
        {
            NeedSearch = false,
            Query = "",
            Reason = "决策解析失败，默认不搜索。"
        };

        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        try
        {
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');

            if (start < 0 || end < 0 || end <= start)
                return fallback;

            string json = raw.Substring(start, end - start + 1);

            JObject obj = JObject.Parse(json);

            return new SearchDecision
            {
                NeedSearch = obj["needSearch"]?.Value<bool>() ?? false,
                Query = obj["query"]?.ToString() ?? "",
                Reason = obj["reason"]?.ToString() ?? ""
            };
        }
        catch (Exception e)
        {
            Debug.LogWarning("搜索决策解析失败：" + e.Message);
            Debug.LogWarning("原始返回：" + raw);
            return fallback;
        }
    }
}