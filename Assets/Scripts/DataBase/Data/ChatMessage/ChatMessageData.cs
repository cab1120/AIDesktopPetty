using System;
using SQLite4Unity3d;

[Table("ChatMessage")]
public class ChatMessageData
{
    [PrimaryKey]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [Indexed, NotNull]
    public string UserId { get; set; }
    
    public string UserName { get; set; }

    [Indexed, NotNull]
    public string CharacterId { get; set; }
    
    public string CharacterName { get; set; }

    [Indexed]
    public string EmotionId { get; set; }

    [NotNull]
    public string Sender { get; set; } //谁发的（User / Assistant）

    [NotNull]
    public string Content { get; set; } // 消息内容

    public long CreatedAtTicks { get; set; } // 创建时间
}