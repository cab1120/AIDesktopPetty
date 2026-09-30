public readonly struct SessionSnapshot
{
    public string UserId { get; }
    public string UserName { get; }
    public string Role { get; }

    public string CharacterId { get; }
    public string CharacterName { get; }

    public long SessionVersion { get; }

    public bool IsLoggedIn =>
        !string.IsNullOrEmpty(UserId) &&
        !string.IsNullOrEmpty(CharacterId);

    public SessionSnapshot(
        string userId,
        string userName,
        string role,
        string characterId,
        string characterName,
        long sessionVersion)
    {
        UserId = userId;
        UserName = userName;
        Role = role;

        CharacterId = characterId;
        CharacterName = characterName;

        SessionVersion = sessionVersion;
    }
}
