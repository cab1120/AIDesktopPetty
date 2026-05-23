using System;

public static class IrohaPromptBuilder
{
    public static string Build(
        string currentTime,
        string searchResults,
        string userMemory)
    {
        EmotionData emotion = EmotionGenerator.GenerateEmotion();
        
        string emotionPrompt =
            IrohaEmotionPromptBuilder.Build(emotion);

        return
            IrohaCorePersonality.Build() + "\n" 
                                         + IrohaWorldView.Build() + "\n"
                                         + "### 实时环境描述 ###\n"
                                         + $"- 电脑系统时间：{currentTime}\n"
                                         + $"- 角色长期心境：{IrohaStatusContext.LongTermMood}\n\n"
                                         + IrohaDataState.Build() + "\n"
                                         + IrohaMemoryContext.Build(userMemory) + "\n"
                                         + IrohaRealtimeContext.Build(currentTime, searchResults) + "\n"
                                         + emotionPrompt + "\n"// 情绪和风格限制放在最后，权重最高
                                          +IrohaProhibitedItems.Build();
    }
    
    public static string BubbleBuild(
        string currentTime,
        string searchResults,
        string userMemory)
    {
        EmotionData emotion = EmotionGenerator.GenerateEmotion();
        
        string emotionPrompt =
            IrohaEmotionPromptBuilder.Build(emotion);

        return
            IrohaCorePersonality.Build() + "\n"
                                         + IrohaWorldView.Build() + "\n"
                                         + "### 实时环境描述 ###\n"
                                         + $"- 电脑系统时间：{currentTime}\n"
                                         + $"- 角色长期心境：{IrohaStatusContext.LongTermMood}\n\n"
                                         + IrohaDataState.Build() + "\n"
                                         + IrohaMemoryContext.Build(userMemory) + "\n"
                                         + IrohaRealtimeContext.Build(currentTime, searchResults) + "\n"
                                         + emotionPrompt + "\n" // 情绪和风格限制放在最后，权重最高
                                         + IrohaProhibitedItems.Build()+ "\n"
                                         + IrohaBubblePrompt.Build();
    }
}