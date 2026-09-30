using System;
using System.IO;
using UnityEngine;

public static class DefaultDataInitializer
{
    public const string DefaultUserName = "DefaultUser";
    public const string DefaultCharacterName = "DefaultCharacter";

    public static void Initialize()
    {
        string promptPath = Path.Combine(Application.streamingAssetsPath,
            "DefaultCharacterPrompt.json");
        if (!File.Exists(promptPath))
            throw new FileNotFoundException("默认角色 Prompt 文件不存在", promptPath);
        string prompt = File.ReadAllText(promptPath);
        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidDataException("默认角色 Prompt 文件为空: " + promptPath);
        DatabaseManager.Connection.RunInTransaction(() =>
        {
            CreateDefaultUser();
            CreateDefaultCharacter(prompt);
        });
    }

    private static void CreateDefaultUser()
    {
        var user = UserRepository.GetByUserName(DefaultUserName);

        if (user != null)
            return;

        user = new UserData
        {
            UserId = Guid.NewGuid().ToString(),
            UserName = DefaultUserName,
            PasswordHash = PasswordHasher.Hash("123456"),
            Role = "Admin",
            CreatedAtTicks = DateTime.Now.Ticks,
            LastLoginAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(user);
    }

    private static void CreateDefaultCharacter(string prompt)
    {
        var character = CharacterRepository.GetByUserAndName(
            DefaultUserName,
            DefaultCharacterName
        );
        
        if (character != null)
            return;

        var owner = UserRepository.GetByUserName(DefaultUserName);
        character = new CharacterProfileData
        {
            CharacterId = Guid.NewGuid().ToString(),
            UserId = owner.UserId,
            UserName = DefaultUserName,
            CharacterName =  DefaultCharacterName,
            PromptJson = prompt,
            IsActive = true,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(character);
    }
}
