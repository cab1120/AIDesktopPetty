using System;
using System.Collections.Generic;
using System.Linq;

public static class UserRepository
{
    public static UserData GetByUserName(string userName)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<UserData>()
            .FirstOrDefault(u => u.UserName == userName);
    }

    public static List<UserData> GetAll()
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<UserData>()
            .OrderBy(u => u.UserName)
            .ToList();
    }
    
    public static List<UserData> SearchByUserName(string keyword)
    {
        DatabaseManager.Initialize();

        if (string.IsNullOrWhiteSpace(keyword))
            return GetAll();

        return DatabaseManager.Connection
            .Table<UserData>()
            .Where(u => u.UserName.Contains(keyword))
            .OrderBy(u => u.UserName)
            .ToList();
    }

    public static bool AddUser(
        string userName,
        string password,
        string role,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(userName))
        {
            error = "用户名不能为空";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "密码不能为空";
            return false;
        }

        if (role != "Admin" && role != "User" && role != "Guest")
        {
            error = "权限类型不合法";
            return false;
        }

        if (GetByUserName(userName) != null)
        {
            error = "用户名已存在";
            return false;
        }

        UserData user = new UserData
        {
            UserId = Guid.NewGuid().ToString(),
            UserName = userName,
            PasswordHash = PasswordHasher.Hash(password),
            Role = role,
            CreatedAtTicks = DateTime.Now.Ticks,
            LastLoginAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(user);
        return true;
    }

    public static bool UpdateUser(
        string userId,
        string newUserName,
        string newPassword,
        string newRole,
        out string error)
    {
        error = "";

        var user = DatabaseManager.Connection.Find<UserData>(userId);

        if (user == null)
        {
            error = "用户不存在";
            return false;
        }

        var sameNameUser = GetByUserName(newUserName);

        if (sameNameUser != null && sameNameUser.UserId != userId)
        {
            error = "用户名已被其他用户使用";
            return false;
        }

        user.UserName = newUserName;
        user.Role = newRole;

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            user.PasswordHash = PasswordHasher.Hash(newPassword);
        }

        DatabaseManager.Connection.Update(user);
        return true;
    }

    public static bool DeleteUser(string userId, out string error)
    {
        error = "";

        if (userId == DefaultDataInitializer.DefaultUserId)
        {
            error = "默认管理员不能删除";
            return false;
        }

        var user = DatabaseManager.Connection.Find<UserData>(userId);

        if (user == null)
        {
            error = "用户不存在";
            return false;
        }

        DatabaseManager.Connection.Delete(user);
        return true;
    }
}