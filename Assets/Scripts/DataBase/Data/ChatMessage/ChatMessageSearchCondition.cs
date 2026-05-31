/// <summary>
/// 搜索条件
/// </summary>
public class ChatMessageSearchCondition
{
    public string UserId;
    public string CharacterId;

    public string UserNameKeyword;
    public string CharacterNameKeyword;
    public string ContentKeyword;

    public string Sender; // User / Assistant / 空表示全部

    public long StartTicks;
    public long EndTicks;
}