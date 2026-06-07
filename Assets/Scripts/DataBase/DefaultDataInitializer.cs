using System;
using System.IO;

public static class DefaultDataInitializer
{
    public const string DefaultUserName = "DefaultUser";
    public const string DefaultCharacterName = "DefaultCharacter";

    public static void Initialize()
    {
        CreateDefaultUser();
        CreateDefaultCharacter();
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

    private static void CreateDefaultCharacter()
    {
        var character = CharacterRepository.GetByName(DefaultCharacterName);

        if (character != null)
            return;

        character = new CharacterProfileData
        {
            CharacterId = Guid.NewGuid().ToString(),
            UserName = DefaultUserName,
            CharacterName =  DefaultCharacterName,
            PromptJson = File.ReadAllText(
                "E:\\unity\\AIDesktopPetty\\Assets\\StreamingAssets\\DefaultCharacterPrompt.json"),
            IsActive = true,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(character);
    }
}