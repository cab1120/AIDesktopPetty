using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

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
            error = "User name cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "Password cannot be empty";
            return false;
        }

        if (role != "Admin" && role != "User" && role != "Guest")
        {
            error = "Invalid role type";
            return false;
        }

        if (GetByUserName(userName) != null)
        {
            error = "User name already exists";
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

        string creationError = null;
        try
        {
            DatabaseManager.Connection.RunInTransaction(() =>
            {
                DatabaseManager.Connection.Insert(user);
                if (!CreateDefaultCharacterForUser(user.UserName, out creationError))
                    throw new InvalidOperationException(creationError);
            });
        }
        catch (Exception ex)
        {
            error = creationError ?? ex.Message;
            return false;
        }

        return true;
    }

    private static bool CreateDefaultCharacterForUser(
        string userName,
        out string error)
    {
        string promptPath = Path.Combine(
            Application.streamingAssetsPath,
            "DefaultCharacterPrompt.json"
        );

        if (!File.Exists(promptPath))
        {
            error = "Default character prompt file does not exist";
            return false;
        }

        return CharacterRepository.AddCharacter(
            userName,
            DefaultDataInitializer.DefaultCharacterName,
            File.ReadAllText(promptPath),
            true,
            out error
        );
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
            error = "User does not exist";
            return false;
        }

        var sameNameUser = GetByUserName(newUserName);

        if (sameNameUser != null && sameNameUser.UserId != userId)
        {
            error = "User name is already used by another user";
            return false;
        }

        if (string.IsNullOrWhiteSpace(newUserName) ||
            (newRole != "Admin" && newRole != "User" && newRole != "Guest"))
        {
            error = "Invalid user name or role";
            return false;
        }

        user.UserName = newUserName;
        user.Role = newRole;

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            user.PasswordHash = PasswordHasher.Hash(newPassword);
        }

        DatabaseManager.Connection.RunInTransaction(() =>
        {
            DatabaseManager.Connection.Update(user);
            DatabaseManager.Connection.Execute(
                "UPDATE CharacterProfile SET UserName = ? WHERE UserId = ?",
                user.UserName, user.UserId);
        });
        GlobalSession.RefreshCurrentUser(user);
        return true;
    }

    public static bool DeleteUser(string userId, out string error)
    {
        error = "";

        var user = DatabaseManager.Connection.Find<UserData>(userId);

        if (user == null)
        {
            error = "User does not exist";
            return false;
        }

        if (user.UserName == DefaultDataInitializer.DefaultUserName)
        {
            error = "Default admin cannot be deleted";
            return false;
        }

        if (user.UserId == GlobalSession.CaptureSnapshot().UserId)
        {
            error = "Current login user cannot delete itself";
            return false;
        }

        try
        {
            DeleteAllUserData(user);
            return true;
        }
        catch (Exception ex)
        {
            error = "删除用户失败: " + ex.Message;
            return false;
        }
    }
    
    public static bool DeleteUserByName(string userName, out string error)
    {
        error = "";

        if (userName == DefaultDataInitializer.DefaultUserName)
        {
            error = "Default admin cannot be deleted";
            return false;
        }

        var user = GetByUserName(userName);

        if (user == null)
        {
            error = "User does not exist";
            return false;
        }

        if (user.UserId == GlobalSession.CaptureSnapshot().UserId)
        {
            error = "Current login user cannot delete itself";
            return false;
        }

        return DeleteUser(user.UserId, out error);
    }

    private static void DeleteAllUserData(UserData user)
    {
        DatabaseManager.Connection.RunInTransaction(() =>
        {
            const string byUser = " WHERE UserId = ?";
            DatabaseManager.Connection.Execute("DELETE FROM ChatMessage" + byUser, user.UserId);
            DatabaseManager.Connection.Execute("DELETE FROM UserCharacterState" + byUser, user.UserId);
            DatabaseManager.Connection.Execute("DELETE FROM EmotionState" + byUser, user.UserId);
            DatabaseManager.Connection.Execute("DELETE FROM InteractionEvent" + byUser, user.UserId);
            DatabaseManager.Connection.Execute("DELETE FROM CharacterProfile" + byUser, user.UserId);
            DatabaseManager.Connection.Delete(user);
        });
    }
}
