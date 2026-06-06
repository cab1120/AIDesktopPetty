using System;
/// <summary>
/// 通用角色提示词
/// </summary>
[Serializable]
public class CharacterPromptProfile
{
    public string characterName;

    // 通用角色设定
    public string corePersonality;
    public string worldView;
    public string speechStyle;
    public string prohibitedItems;

    // Chat 专用
    public string chatRule;

    // Bubble 专用
    public string bubbleRule;

    // 实时信息处理规则
    public string realtimeRule;
}