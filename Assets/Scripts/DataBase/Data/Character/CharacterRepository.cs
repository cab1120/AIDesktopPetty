using System;
using System.Collections.Generic;
using System.Linq;

public static class CharacterRepository
{
    public static CharacterProfileData GetByName(string characterName)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .FirstOrDefault(c => c.CharacterName == characterName);
    }

    public static CharacterProfileData GetActiveCharacter(string userId)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .FirstOrDefault(c => c.UserId == userId && c.IsActive);
    }

    public static List<CharacterProfileData> GetAll()
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .OrderBy(c => c.CharacterName)
            .ToList();
    }

    public static List<CharacterProfileData> GetByUserId(string userId)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CharacterName)
            .ToList();
    }

    public static bool AddCharacter(
        string userId,
        string characterName,
        string promptJson,
        bool isActive,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(characterName))
        {
            error = "角色名不能为空";
            return false;
        }

        if (GetByName(characterName) != null)
        {
            error = "角色名已存在";
            return false;
        }

        if (isActive)
        {
            DisableAllCharacters(userId);
        }

        CharacterProfileData character = new CharacterProfileData
        {
            CharacterId = Guid.NewGuid().ToString(),
            UserId = userId,
            CharacterName = characterName,
            PromptJson = promptJson ?? "",
            IsActive = isActive,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(character);

        EnsureAtLeastOneActive(userId);

        return true;
    }

    public static bool UpdateCharacter(
        string characterId,
        string characterName,
        string promptJson,
        bool isActive,
        out string error)
    {
        error = "";

        var character =
            DatabaseManager.Connection.Find<CharacterProfileData>(characterId);

        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        var sameName = GetByName(characterName);

        if (sameName != null && sameName.CharacterId != characterId)
        {
            error = "角色名已被使用";
            return false;
        }

        character.CharacterName = characterName;
        character.PromptJson = promptJson ?? "";

        if (isActive)
        {
            DisableAllCharacters(character.UserId);
            character.IsActive = true;
        }
        else
        {
            character.IsActive = false;
        }

        DatabaseManager.Connection.Update(character);

        if (!EnsureAtLeastOneActive(character.UserId))
        {
            character.IsActive = true;
            DatabaseManager.Connection.Update(character);
            error = "至少需要启用一个角色";
            return false;
        }

        return true;
    }

    public static bool DeleteCharacter(string characterId, out string error)
    {
        error = "";

        if (characterId == DefaultDataInitializer.DefaultCharacterId)
        {
            error = "默认角色不能删除";
            return false;
        }

        var character =
            DatabaseManager.Connection.Find<CharacterProfileData>(characterId);

        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        string userId = character.UserId;

        DatabaseManager.Connection.Delete(character);

        if (!EnsureAtLeastOneActive(userId))
        {
            error = "删除失败：至少需要保留一个启用角色";
            return false;
        }

        return true;
    }

    public static bool SetActiveCharacter(string userId, string characterId)
    {
        var character =
            DatabaseManager.Connection.Find<CharacterProfileData>(characterId);

        if (character == null)
            return false;

        DisableAllCharacters(userId);

        character.IsActive = true;
        DatabaseManager.Connection.Update(character);

        return true;
    }

    private static void DisableAllCharacters(string userId)
    {
        var characters = GetByUserId(userId);

        foreach (var c in characters)
        {
            c.IsActive = false;
            DatabaseManager.Connection.Update(c);
        }
    }

    private static bool EnsureAtLeastOneActive(string userId)
    {
        var characters = GetByUserId(userId);

        if (characters.Count == 0)
            return false;

        bool hasActive = characters.Any(c => c.IsActive);

        if (hasActive)
            return true;

        var first = characters[0];
        first.IsActive = true;
        DatabaseManager.Connection.Update(first);

        return true;
    }
}