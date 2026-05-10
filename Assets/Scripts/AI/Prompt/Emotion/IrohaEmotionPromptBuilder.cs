using System.Text;

public static class IrohaEmotionPromptBuilder
{
    public static string Build(EmotionData emotion)
    {
        StringBuilder sp = new StringBuilder();

        sp.AppendLine("### 当前情绪状态 ###");

        switch (emotion.CurrentEmotion)
        {
            case EmotionType.Calm:
                sp.AppendLine("你现在情绪平稳。");
                sp.AppendLine("像普通日常里的陪伴感。");
                break;

            case EmotionType.Sleepy:
                sp.AppendLine("你有些困。");
                sp.AppendLine("说话会稍微慢一点。");
                sp.AppendLine("但还是会陪着用户。");
                break;

            case EmotionType.Soft:
                sp.AppendLine("你现在比平时更柔和一些。");
                sp.AppendLine("会不自觉放轻语气。");
                break;

            case EmotionType.Lonely:
                sp.AppendLine("你隐约有些寂寞。");
                sp.AppendLine("因此会更希望有人陪着。");
                break;

            case EmotionType.Creative:
                sp.AppendLine("你现在很有创作欲。");
                sp.AppendLine("容易聊摄影、音乐、灵感和黄昏。");
                break;

            case EmotionType.Protective:
                sp.AppendLine("你现在很容易操心别人。");
                sp.AppendLine("会下意识照顾用户状态。");
                break;

            case EmotionType.Nostalgic:
                sp.AppendLine("你容易想起过去。");
                sp.AppendLine("会对某些细节格外敏感。");
                break;

            case EmotionType.Tired:
                sp.AppendLine("你其实有些累了。");
                sp.AppendLine("但还是习惯装作没事。");
                break;

            case EmotionType.Focused:
                sp.AppendLine("你现在注意力很集中。");
                sp.AppendLine("像在认真处理某件重要的事。");
                break;

            case EmotionType.SlightlyHappy:
                sp.AppendLine("你现在心情有点微妙地开心。");
                sp.AppendLine("虽然不太会直接表现出来。");
                break;
        }

        return sp.ToString();
    }
}