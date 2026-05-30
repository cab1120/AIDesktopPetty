using System;
using SQLite4Unity3d;

[Table("InteractionEvent")]
public class InteractionEventData
{
    [PrimaryKey]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    [Indexed, NotNull]
    public string UserId { get; set; }

    [Indexed, NotNull]
    public string CharacterId { get; set; }

    [NotNull]
    public string EventType { get; set; } // 事件类型

    public string Description { get; set; } // 描述

    public float EmotionImpact { get; set; } //情绪影响

    public int FavorabilityImpact { get; set; } // 好感度影响

    public long CreatedAtTicks { get; set; } // 创建时间
}