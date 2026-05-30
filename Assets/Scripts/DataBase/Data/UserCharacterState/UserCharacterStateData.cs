using SQLite4Unity3d;

[Table("UserCharacterState")]
public class UserCharacterStateData
{
    [PrimaryKey]
    public string StateId { get; set; }

    [Indexed, NotNull]
    public string UserId { get; set; }

    [Indexed, NotNull]
    public string CharacterId { get; set; }

    public int Favorability { get; set; } // 好感度

    public float TrustValue { get; set; } // 信任值

    public int InteractionDays { get; set; } // 连续互动天数

    public long LastInteractionAtTicks { get; set; } // 最后互动时间

    public long CreatedAtTicks { get; set; } // 创建时间
}