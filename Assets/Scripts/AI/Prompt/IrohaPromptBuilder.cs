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
                                         + IrohaWorldView.Build()
                                         + "\n"
                                         + IrohaDataState.Build()
                                         + "\n"
                                         + IrohaMemoryContext.Build(userMemory)
                                         + "\n"
                                         + IrohaRealtimeContext.Build(currentTime, searchResults)
                                         + "\n"
                                         + emotionPrompt;
    }
}