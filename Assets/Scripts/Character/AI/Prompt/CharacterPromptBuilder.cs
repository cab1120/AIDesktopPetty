using System.Text;
/// <summary>
/// 提示词生成
/// </summary>
public static class CharacterPromptBuilder
{
    public static string BuildChatPrompt(PromptContext context)
    {
        CharacterPromptProfile profile =
            CharacterPromptLoader.LoadCurrentProfile();

        StringBuilder sp = new StringBuilder();

        AppendCommonCharacterPrompt(sp, profile);
        AppendCommonRuntimePrompt(sp, context);

        sp.AppendLine("### 本轮任务：对话回复 ###");
        sp.AppendLine(profile.chatRule);
        sp.AppendLine("用户接下来会主动对你说话。");
        sp.AppendLine("你需要根据用户的问题、情绪、上下文进行回应。");
        sp.AppendLine("不要把回复写成主动通知，也不要突然切换话题。");

        return sp.ToString();
    }

    public static string BuildBubblePrompt(PromptContext context)
    {
        CharacterPromptProfile profile =
            CharacterPromptLoader.LoadCurrentProfile();

        StringBuilder sp = new StringBuilder();

        AppendCommonCharacterPrompt(sp, profile);
        AppendCommonRuntimePrompt(sp, context);

        sp.AppendLine("### 本轮任务：桌宠主动搭话 ###");
        sp.AppendLine(profile.bubbleRule);
        sp.AppendLine("这不是用户发给你的聊天消息。");
        sp.AppendLine("接下来的输入可能是电脑进程名、窗口标题、用户当前状态或环境信息。");
        sp.AppendLine("你要像桌宠一样主动说一句自然的话。");
        sp.AppendLine("不要说“系统检测到”“根据进程名”“我看到你打开了”。");
        sp.AppendLine("如果实在无话可说，只回复：[IGNORE]");
        sp.AppendLine("回复必须简短，适合作为桌宠气泡显示。");

        return sp.ToString();
    }

    private static void AppendCommonCharacterPrompt(
        StringBuilder sp,
        CharacterPromptProfile profile)
    {
        sp.AppendLine($"### 当前角色：{profile.characterName} ###");
        sp.AppendLine(profile.corePersonality);
        sp.AppendLine(profile.worldView);
        sp.AppendLine(profile.speechStyle);
        sp.AppendLine(profile.prohibitedItems);
    }

    private static void AppendCommonRuntimePrompt(
        StringBuilder sp,
        PromptContext context)
    {
        sp.AppendLine("### 当前运行环境 ###");
        sp.AppendLine($"当前用户：{GlobalSession.CurrentUserName}");
        sp.AppendLine($"当前角色：{GlobalSession.CurrentCharacterName}");
        sp.AppendLine($"当前时间：{context.CurrentTime}");

        sp.AppendLine("### 与用户相关的记忆 ###");
        sp.AppendLine(string.IsNullOrWhiteSpace(context.UserMemory)
            ? "你还在慢慢了解用户。"
            : context.UserMemory);

        sp.AppendLine("### 实时信息处理规则 ###");
        CharacterPromptProfile profile =
            CharacterPromptLoader.LoadCurrentProfile();
        sp.AppendLine(profile.realtimeRule);

        if (!string.IsNullOrWhiteSpace(context.SearchResults))
        {
            sp.AppendLine("### 当前可用实时信息 ###");
            sp.AppendLine(context.SearchResults);
        }

        if (context.Emotion != null)
        {
            sp.AppendLine("### 当前情绪状态 ###");
            sp.AppendLine(IrohaEmotionPromptBuilder.Build(context.Emotion));
        }

        sp.AppendLine($"- 角色长期心境：{IrohaStatusContext.LongTermMood}\n");
    }
}