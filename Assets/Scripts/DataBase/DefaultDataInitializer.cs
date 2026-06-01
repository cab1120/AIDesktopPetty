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
            UserName = "DefaultUser",
            PasswordHash = PasswordHasher.Hash("123456"),
            Role = "Admin",
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
            CharacterName = "DefaultCharacter",
            PromptJson = "",
            IsActive = true,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(character);
    }
}