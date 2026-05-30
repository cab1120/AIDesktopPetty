using System;

public static class DefaultDataInitializer
{
    public const string DefaultUserId = "DefaultUser";
    public const string DefaultCharacterId = "DefaultCharacter";

    public static void Initialize()
    {
        CreateDefaultUser();
        CreateDefaultCharacter();
        UserCharacterStateRepository.GetOrCreate(
            DefaultUserId,
            DefaultCharacterId
        );
    }

    private static void CreateDefaultUser()
    {
        var user = DatabaseManager.Connection.Find<UserData>(DefaultUserId);

        if (user != null)
            return;

        user = new UserData
        {
            UserId = DefaultUserId,
            UserName = "默认用户",
            PasswordHash = "LocalUser",
            Role = "User",
            CreatedAtTicks = DateTime.Now.Ticks,
            LastLoginAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(user);
    }

    private static void CreateDefaultCharacter()
    {
        var character = DatabaseManager.Connection.Find<CharacterProfileData>(
            DefaultCharacterId
        );

        if (character != null)
            return;

        character = new CharacterProfileData
        {
            CharacterId = DefaultCharacterId,
            UserId = DefaultUserId,
            CharacterName = "酒寄彩叶",
            PromptJson = "",
            IsActive = true,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(character);
    }
}