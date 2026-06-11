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

        DatabaseManager.Connection.Insert(user);

        if (!CreateDefaultCharacterForUser(user.UserName, out error))
        {
            DatabaseManager.Connection.Delete(user);
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

        if (user.UserName == GlobalSession.CurrentUserName)
        {
            error = "Current login user cannot delete itself";
            return false;
        }

        DatabaseManager.Connection.Delete(user);
        return true;
    }
    
    public static bool DeleteUserByName(string userName, out string error)
    {
        error = "";

        if (userName == DefaultDataInitializer.DefaultUserName)
        {
            error = "Default admin cannot be deleted";
            return false;
        }

        if (userName == GlobalSession.CurrentUserName)
        {
            error = "Current login user cannot delete itself";
            return false;
        }

        var user = GetByUserName(userName);

        if (user == null)
        {
            error = "User does not exist";
            return false;
        }

        DatabaseManager.Connection.Delete(user);
        return true;
    }
}