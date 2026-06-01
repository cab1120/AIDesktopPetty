using System;
using SQLite4Unity3d;

[Table("CharacterProfile")]
public class CharacterProfileData
{
    [PrimaryKey]
    public string CharacterId { get; set; } = Guid.NewGuid().ToString();

    [Indexed, NotNull]
    public string UserId { get; set; }

    [NotNull]
    public string CharacterName { get; set; }

    public string PromptJson { get; set; } // Prompt配置

    public bool IsActive { get; set; } = true;  // 是否启用

    public long CreatedAtTicks { get; set; } // 创建时间
}