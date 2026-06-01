using System;
using SQLite4Unity3d;

[Table("User")]
public class UserData
{
    [PrimaryKey]
    public string UserId { get; set; } = Guid.NewGuid().ToString();

    [NotNull, Unique]
    public string UserName { get; set; }

    [NotNull]
    public string PasswordHash { get; set; }

    [NotNull]
    public string Role { get; set; } = "User"; // Admin / User / Guest

    public long CreatedAtTicks { get; set; } // 注册时间

    public long LastLoginAtTicks { get; set; } // 最后登录时间
}