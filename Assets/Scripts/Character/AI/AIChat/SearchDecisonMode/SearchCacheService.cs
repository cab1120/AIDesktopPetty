using System;
using System.Collections.Generic;
using System.Linq;

public static class SearchCacheService
{
    private static readonly List<SearchCacheEntry> cache =
        new List<SearchCacheEntry>();

    private const int MaxCacheCount = 5;
    private const int CacheMinutes = 15;

    public static bool TryGetRecent(
        string userMessage,
        out SearchCacheEntry entry)
    {
        entry = null;

        RemoveExpired();

        if (string.IsNullOrWhiteSpace(userMessage))
            return false;

        string normalizedMessage = Normalize(userMessage);

        entry = cache
            .OrderByDescending(c => c.CreatedAtTicks)
            .FirstOrDefault(c =>
                IsRelated(normalizedMessage, Normalize(c.Query)));

        return entry != null;
    }

    public static void Add(
        string query,
        string results,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(query) ||
            string.IsNullOrWhiteSpace(results))
        {
            return;
        }

        RemoveExpired();

        SearchCacheEntry old = cache.FirstOrDefault(c =>
            Normalize(c.Query) == Normalize(query));

        if (old != null)
        {
            cache.Remove(old);
        }

        cache.Add(new SearchCacheEntry
        {
            Query = query,
            Results = results,
            Reason = reason,
            CreatedAtTicks = DateTime.Now.Ticks
        });

        Trim();
    }

    private static void Trim()
    {
        while (cache.Count > MaxCacheCount)
        {
            SearchCacheEntry oldest =
                cache.OrderBy(c => c.CreatedAtTicks).First();

            cache.Remove(oldest);
        }
    }

    private static void RemoveExpired()
    {
        DateTime now = DateTime.Now;

        cache.RemoveAll(c =>
            (now - c.CreatedAt).TotalMinutes > CacheMinutes);
    }

    private static bool IsRelated(string message, string query)
    {
        if (string.IsNullOrWhiteSpace(message) ||
            string.IsNullOrWhiteSpace(query))
            return false;

        if (message.Contains(query) || query.Contains(message))
            return true;

        string[] queryParts = query
            .Split(' ', '　', ',', '，', '、')
            .Where(p => p.Length >= 2)
            .ToArray();

        foreach (string part in queryParts)
        {
            if (message.Contains(part))
                return true;
        }

        // 追问短句，默认认为可能延续上一轮搜索话题
        string[] followUpWords =
        {
            "那", "这个", "那个", "里面", "角色", "他", "她",
            "为什么", "怎么", "后来", "结局", "设定"
        };

        if (message.Length <= 20)
        {
            foreach (string word in followUpWords)
            {
                if (message.Contains(word))
                    return true;
            }
        }

        return false;
    }

    private static string Normalize(string text)
    {
        return text
            .Trim()
            .ToLower()
            .Replace("？", "")
            .Replace("?", "")
            .Replace("。", "")
            .Replace(".", "")
            .Replace("！", "")
            .Replace("!", "");
    }
}