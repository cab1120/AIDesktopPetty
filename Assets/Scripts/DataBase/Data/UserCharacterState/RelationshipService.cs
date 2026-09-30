using UnityEngine;

public static class RelationshipService
{
    public static UserCharacterStateData GetCurrentState()
    {
        return GetState(
            GlobalSession.CaptureSnapshot()
        );
    }

    public static UserCharacterStateData GetState(
        SessionSnapshot session)
    {
        if (!session.IsLoggedIn)
            return null;

        return UserCharacterStateRepository
            .GetOrCreate(
                session.UserId,
                session.CharacterId
            );
    }

    public static void OnLogin()
    {
        SessionSnapshot session = GlobalSession.CaptureSnapshot();
        if (!GlobalSession.IsCurrent(session))
            return;

        UserCharacterStateRepository.ApplyTimeDecay(
            session.UserId,
            session.CharacterId
        );

        UserCharacterStateRepository.UpdateInteractionDays(
            session.UserId,
            session.CharacterId
        );

        UserCharacterStateRepository.ApplyFavorabilityChange(
            session.UserId,
            session.CharacterId,
            1,
            true
        );

        UserCharacterStateRepository.ApplyTrustChange(
            session.UserId,
            session.CharacterId,
            0.005f,
            true
        );
    }

    public static void OnUserSendMessage(
        string message)
    {
        OnUserSendMessage(
            GlobalSession.CaptureSnapshot(),
            message
        );
    }

    public static void OnUserSendMessage(
        SessionSnapshot session,
        string message)
    {
        if (!session.IsLoggedIn)
            return;

        if (string.IsNullOrWhiteSpace(message))
        {
            UserCharacterStateRepository
                .ApplyFavorabilityChange(
                    session.UserId,
                    session.CharacterId,
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

        UserCharacterStateRepository
            .ApplyFavorabilityChange(
                session.UserId,
                session.CharacterId,
                delta,
                true
            );

        if (message.Length >= 30 &&
            !IsMeaninglessMessage(message))
        {
            UserCharacterStateRepository
                .ApplyTrustChange(
                    session.UserId,
                    session.CharacterId,
                    0.003f,
                    true
                );
        }
    }

    public static void OnAssistantReplyFinished()
    {
        OnAssistantReplyFinished(
            GlobalSession.CaptureSnapshot()
        );
    }

    public static void OnAssistantReplyFinished(
        SessionSnapshot session)
    {
        if (!session.IsLoggedIn)
            return;

        UserCharacterStateRepository
            .ApplyTrustChange(
                session.UserId,
                session.CharacterId,
                0.001f,
                true
            );
    }
    
    public static void OnAssistantReplyFailed()
    {
        SessionSnapshot session = GlobalSession.CaptureSnapshot();
        if (!GlobalSession.IsCurrent(session))
            return;

        UserCharacterStateRepository.ApplyFavorabilityChange(
            session.UserId,
            session.CharacterId,
            -1,
            true
        );
    }

    public static void OnOpenPetPanel()
    {
        SessionSnapshot session = GlobalSession.CaptureSnapshot();
        if (!GlobalSession.IsCurrent(session))
            return;

        UserCharacterStateRepository.ApplyFavorabilityChange(
            session.UserId,
            session.CharacterId,
            1,
            true
        );
    }
    

    public static string BuildRelationshipPromptText()
    {
        return BuildRelationshipPromptText(
            GlobalSession.CaptureSnapshot()
        );
    }

    public static string BuildRelationshipPromptText(
        SessionSnapshot session)
    {
        if (!session.IsLoggedIn)
            return "";

        var state = GetState(session);

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
