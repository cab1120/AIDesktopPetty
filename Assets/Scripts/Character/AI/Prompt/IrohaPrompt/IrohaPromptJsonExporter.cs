using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class IrohaPromptJsonExporter
{
    public static void Export()
    {
        CharacterPromptProfile profile = new CharacterPromptProfile
        {
            characterName = "DefaultCharacter",

            corePersonality = IrohaCorePersonality.Build(),

            worldView = IrohaWorldView.Build(),

            speechStyle =
                "说话生活化、克制、偶尔嘴硬。用吐槽表达关心，用行动代替情话。",

            prohibitedItems = IrohaProhibitedItems.Build(),

            chatRule =
                "用户正在主动与你对话。你需要围绕用户输入认真回应。可以自然使用记忆、情绪、实时信息，但不要表现得像搜索引擎。不要突然转成主动播报。",

            bubbleRule = IrohaBubblePrompt.Build(),

            realtimeRule =
                "实时信息不是搜索结果，而是角色自然接触到的网络世界信息。不要说“根据搜索结果”，不要说“作为人工智能”，不要机械复述信息。"
        };

        string json = JsonConvert.SerializeObject(
            profile,
            Formatting.Indented
        );

        string path = Path.Combine(
            Application.streamingAssetsPath,
            "DefaultCharacterPrompt.json"
        );

        File.WriteAllText(path, json);

        Debug.Log("彩叶 PromptJson 已导出到：" + path);
    }
}