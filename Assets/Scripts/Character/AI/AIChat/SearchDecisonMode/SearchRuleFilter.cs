using System.Text.RegularExpressions;
/// <summary>
/// 搜索决策优化
/// </summary>
public static class SearchRuleFilter
{
    public static SearchDecisionMode JudgeByRule(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return SearchDecisionMode.NoSearch;

        string text = message.Trim();

        if (IsPureDailyChat(text))
            return SearchDecisionMode.NoSearch;

        if (IsExplicitSearchRequest(text))
            return SearchDecisionMode.DirectSearch;

        if (LooksLikeRealtimeOrUnknownTopic(text))
            return SearchDecisionMode.NeedLLMJudge;

        return SearchDecisionMode.NoSearch;
    }

    private static bool IsPureDailyChat(string text)
    {
        string[] dailyKeywords =
        {
            "好累", "累死", "困", "想睡", "课好多", "作业好多",
            "不想上课", "心情不好", "难受", "烦", "无聊",
            "饿了", "困死", "今天好忙", "压力好大"
        };

        foreach (string keyword in dailyKeywords)
        {
            if (text.Contains(keyword))
                return true;
        }

        if (text.Length <= 8 &&
            (text.Contains("累") ||
             text.Contains("困") ||
             text.Contains("烦") ||
             text.Contains("饿")))
        {
            return true;
        }

        return false;
    }

    private static bool IsExplicitSearchRequest(string text)
    {
        string[] searchKeywords =
        {
            "查一下", "搜一下", "搜索", "帮我查", "帮我搜",
            "最新", "最近新出的", "刚更新", "现在怎么样",
            "新闻", "天气", "价格", "版本更新", "赛程"
        };

        foreach (string keyword in searchKeywords)
        {
            if (text.Contains(keyword))
                return true;
        }

        return false;
    }

    private static bool LooksLikeRealtimeOrUnknownTopic(string text)
    {
        if (text.Contains("新番") ||
            text.Contains("动画") ||
            text.Contains("漫画") ||
            text.Contains("角色") ||
            text.Contains("游戏") ||
            text.Contains("电影") ||
            text.Contains("剧场版") ||
            text.Contains("声优") ||
            text.Contains("番剧"))
        {
            return true;
        }

        // 英文/日文片假名/特殊作品名混杂时，可能是作品名、角色名、游戏名
        if (Regex.IsMatch(text, @"[A-Za-z]{3,}") ||
            Regex.IsMatch(text, @"[\u30A0-\u30FF]{2,}"))
        {
            return true;
        }

        // 较短但像专有名词的问题，比如“你知道xxx吗”
        if ((text.StartsWith("你知道") || text.Contains("是什么")) &&
            text.Length <= 30)
        {
            return true;
        }

        return false;
    }
}