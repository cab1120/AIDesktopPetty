using System;
using SQLite4Unity3d;

[Table("EmotionState")]
public class EmotionRecord
{
    [PrimaryKey]
    public string EmotionId { get; set; } = Guid.NewGuid().ToString();

    [Indexed, NotNull]
    public string UserId { get; set; } = "DefaultUser";

    [Indexed, NotNull]
    public string CharacterId { get; set; } = "DefaultCharacter";

    [NotNull]
    public string EmotionType { get; set; } 

    public float Intensity { get; set; } //情绪强度

   
    public int RemainingMinutes { get; set; } //持续时间
    
    
    public long CreatedAtTicks { get; set; } //开始时间
}