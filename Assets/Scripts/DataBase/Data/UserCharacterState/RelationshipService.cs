using UnityEngine;

public static class RelationshipService
{
    public static UserCharacterStateData GetCurrentState()
    {
        if (!GlobalSession.IsLoggedIn)
            return null;
        
        return UserCharacterStateRepository.GetOrCreate(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId
        );
    }

    public static void OnLogin()
    {
        if (!GlobalSession.IsLoggedIn)
            return;

        UserCharacterStateRepository.ApplyTimeDecay(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId
        );

        UserCharacterStateRepository.UpdateInteractionDays(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId
        );

        UserCharacterStateRepository.ApplyFavorabilityChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            1,
            true
        );

        UserCharacterStateRepository.ApplyTrustChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            0.005f,
            true
        );
    }

    public static void OnUserSendMessage(string message)
    {
        if (!GlobalSession.IsLoggedIn)
        return;

        if (string.IsNullOrWhiteSpace(message))
        {
            UserCharacterStateRepository.ApplyFavorabilityChange(
                GlobalSession.CurrentUserId,
                GlobalSession.CurrentCharacterId,
                -1,
                true
            );

            return;
        }

        int delta = 1;

        if (message.Length >= 30)
            delta += 1;

        if (IsMeaninglessMessage(message))
            delta -= 2;

        UserCharacterStateRepository.ApplyFavorabilityChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            delta,
            true
        );

        if (message.Length >= 30 && !IsMeaninglessMessage(message))
        {
            UserCharacterStateRepository.ApplyTrustChange(
                GlobalSession.CurrentUserId,
                GlobalSession.CurrentCharacterId,
                0.003f,
                true
            );
        }
    }

    public static void OnAssistantReplyFinished()
    {
        if (!GlobalSession.IsLoggedIn)
            return;

        UserCharacterStateRepository.ApplyTrustChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            0.001f,
            true
        );
    }
    
    public static void OnAssistantReplyFailed()
    {
        if (!GlobalSession.IsLoggedIn)
            return;

        UserCharacterStateRepository.ApplyFavorabilityChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            -1,
            true
        );
    }

    public static void OnOpenPetPanel()
    {
        if (!GlobalSession.IsLoggedIn)
            return;

        UserCharacterStateRepository.ApplyFavorabilityChange(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId,
            1,
            true
        );
    }
    

    public static string BuildRelationshipPromptText()
    {
        if (!GlobalSession.IsLoggedIn)
            return "";

        var state = GetCurrentState();

        if (state == null)
            return "";

        return
            $"好感度：{state.Favorability}/100\n" +
            $"信任值：{state.TrustValue:0.00}/1.00\n" +
            $"连续互动天数：{state.InteractionDays}\n" +
            $"关系阶段：{GetRelationshipLevel(state)}\n" +
            $"关系表现要求：{GetRelationshipBehaviorHint(state)}";
    }

    public static string GetRelationshipLevel(UserCharacterStateData state)
    {
        if (state.Favorability < 20)
            return "疏离";

        if (state.Favorability < 40)
            return "初识";

        if (state.Favorability < 70)
            return "熟悉";

        if (state.Favorability < 90)
            return "信赖";

        return "亲密";
    }

    private static string GetRelationshipBehaviorHint(UserCharacterStateData state)
    {
        if (state.Favorability < 20)
            return "保持礼貌和距离，不要表现得过分亲近。";

        if (state.Favorability < 40)
            return "可以自然交流，但语气仍然略显克制。";

        if (state.Favorability < 70)
            return "可以表现出熟悉感，适度关心用户。";

        if (state.Favorability < 90)
            return "可以更主动地关心用户，但不要过度依赖。";

        return "可以表现出较深的信任和陪伴感，但仍保持角色性格的一致性。";
    }
    
    private static bool IsMeaninglessMessage(string message)
    {
        string[] words =
        {
            "烦",
            "讨厌",
            "闭嘴",
            "别说了",
            "无语",
            "滚",
            "没用"
        };

        foreach (string word in words)
        {
            if (message.Contains(word))
                return true;
        }

        return false;
    }
}