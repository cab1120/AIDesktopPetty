public static class GlobalSession
{
    public static string CurrentUserId { get; private set; }
    public static string CurrentUserName { get; private set; }
    public static string CurrentRole { get; private set; }

    public static string CurrentCharacterId { get; private set; }
    public static string CurrentCharacterName { get; private set; }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUserId);

    public static void SetSession(
        UserData user,
        CharacterProfileData character)
    {
        CurrentUserId = user.UserId;
        CurrentUserName = user.UserName;
        CurrentRole = user.Role;

        CurrentCharacterId = character.CharacterId;
        CurrentCharacterName = character.CharacterName;
    }

    public static void Clear()
    {
        CurrentUserId = null;
        CurrentUserName = null;
        CurrentRole = null;
        CurrentCharacterId = null;
        CurrentCharacterName = null;
    }

    public static bool IsAdmin()
    {
        return CurrentRole == "Admin";
    }

    public static bool IsUser()
    {
        return CurrentRole == "User";
    }

    public static bool IsGuest()
    {
        return CurrentRole == "Guest";
    }
}