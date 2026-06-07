using System;

public static class InteractionEventService
{
    private static readonly TimeSpan SameContextCooldown =
        TimeSpan.FromMinutes(10);

    public static string BuildContextKey(string title, string processName)
    {
        string safeTitle = string.IsNullOrWhiteSpace(title)
            ? "unknown_title"
            : title.Trim().ToLower();

        string safeProcess = string.IsNullOrWhiteSpace(processName)
            ? "unknown_process"
            : processName.Trim().ToLower();

        return safeProcess + "|" + safeTitle;
    }

    public static bool CanTriggerBubble(
        string title,
        string processName,
        out string contextKey,
        out string reason)
    {
        contextKey = BuildContextKey(title, processName);
        reason = "";

        if (!GlobalSession.IsLoggedIn)
        {
            reason = "用户未登录";
            return false;
        }

        if (!ContextEvaluator.IsInteresting(title, processName))
        {
            reason = "窗口不值得主动搭话";
            return false;
        }

        bool recentlyShown =
            InteractionEventRepository.HasRecentEvent(
                GlobalSession.CurrentUserId,
                GlobalSession.CurrentCharacterId,
                InteractionEventType.BubbleShown,
                contextKey,
                SameContextCooldown
            );

        if (recentlyShown)
        {
            reason = "短时间内已经对该窗口主动搭话";
            return false;
        }

        return true;
    }

    public static void RecordBubbleRequested(string title, string processName)
    {
        Add(
            InteractionEventType.BubbleRequested,
            processName,
            BuildContextKey(title, processName),
            title,
            0,
            0
        );
    }

    public static void RecordBubbleShown(
        string title,
        string processName,
        string reply)
    {
        Add(
            InteractionEventType.BubbleShown,
            processName,
            BuildContextKey(title, processName),
            $"窗口：{title}\n回复：{reply}",
            0,
            0
        );

        UserCharacterStateRepository.ApplyTrustChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            0.001f,
            false
        );
    }

    public static void RecordBubbleIgnored(string title, string processName)
    {
        Add(
            InteractionEventType.BubbleIgnored,
            processName,
            BuildContextKey(title, processName),
            title,
            0,
            0
        );
    }

    public static void RecordBubbleSuppressed(
        string title,
        string processName,
        string reason)
    {
        Add(
            InteractionEventType.BubbleSuppressed,
            processName,
            BuildContextKey(title, processName),
            $"窗口：{title}\n原因：{reason}",
            0,
            0
        );
    }

    public static void RecordPetDragged()
    {
        Add(
            InteractionEventType.PetWindowDragged,
            "WindowDragHandler",
            "pet_window_dragged",
            "用户拖拽移动了桌宠窗口",
            0,
            0
        );
    }

    public static void RecordPetExpanded()
    {
        Add(
            InteractionEventType.PetExpanded,
            "PetToggleUI",
            "pet_expanded",
            "用户展开了桌宠聊天窗口",
            0,
            1
        );

        UserCharacterStateRepository.ApplyFavorabilityChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            1,
            true
        );
    }

    public static void RecordPetCollapsed()
    {
        Add(
            InteractionEventType.PetCollapsed,
            "PetToggleUI",
            "pet_collapsed",
            "用户收起了桌宠聊天窗口",
            0,
            0
        );
    }

    private static void Add(
        string eventType,
        string eventSource,
        string contextKey,
        string description,
        float emotionImpact,
        int favorabilityImpact)
    {
        if (!GlobalSession.IsLoggedIn)
            return;

        InteractionEventRepository.AddEvent(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            eventType,
            eventSource,
            contextKey,
            description,
            emotionImpact,
            favorabilityImpact
        );
    }
}