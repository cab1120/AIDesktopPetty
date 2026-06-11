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

    public static CharacterProfileData GetByUserAndName(
        string userName,
        string characterName)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .FirstOrDefault(c =>
                c.UserName == userName &&
                c.CharacterName == characterName);
    }

    public static CharacterProfileData GetActiveCharacter(string userName)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .FirstOrDefault(c => c.UserName == userName && c.IsActive);
    }
    
    public static List<CharacterProfileData> SearchByCharacterName(string keyword)
    {
        DatabaseManager.Initialize();

        if (string.IsNullOrWhiteSpace(keyword))
            return GetAll();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .Where(c => c.CharacterName.Contains(keyword))
            .OrderBy(c => c.CharacterName)
            .ToList();
    }
    
    public static List<CharacterProfileData> SearchByCharacterNameForUser(
        string userName,
        string keyword)
    {
        DatabaseManager.Initialize();

        var query = DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .Where(c => c.UserName == userName);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c => c.CharacterName.Contains(keyword));
        }

        return query
            .OrderBy(c => c.CharacterName)
            .ToList();
    }

    public static List<CharacterProfileData> GetAll()
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .OrderBy(c => c.CharacterName)
            .ToList();
    }

    public static List<CharacterProfileData> GetByUserName(string userName)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .Where(c => c.UserName == userName)
            .OrderBy(c => c.CharacterName)
            .ToList();
    }

    public static bool AddCharacter(
        string userName,
        string characterName,
        string promptJson,
        bool isActive,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(userName))
        {
            error = "用户名不能为空";
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(characterName))
        {
            error = "角色名不能为空";
            return false;
        }

        if (GetByUserAndName(userName, characterName) != null)
        {
            error = "角色名已存在";
            return false;
        }

        if (isActive)
        {
            DisableAllCharacters(userName);
        }

        CharacterProfileData character = new CharacterProfileData
        {
            CharacterId = Guid.NewGuid().ToString(),
            UserName = userName,
            CharacterName = characterName,
            PromptJson = promptJson ?? "",
            IsActive = isActive,
            CreatedAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(character);

        EnsureAtLeastOneActive(userName);

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
        
        if (characterName == GlobalSession.CurrentCharacterName)
        {
            error = "默认角色不能删除";
            return false;
        }

        var sameName = GetByUserAndName(character.UserName, characterName);

        if (sameName != null && sameName.CharacterId != characterId)
        {
            error = "角色名已被使用";
            return false;
        }

        character.CharacterName = characterName;
        character.PromptJson = promptJson ?? "";

        if (isActive)
        {
            DisableAllCharacters(character.UserName);
            character.IsActive = true;
        }
        else
        {
            character.IsActive = false;
        }

        DatabaseManager.Connection.Update(character);

        if (!EnsureAtLeastOneActive(character.UserName))
        {
            character.IsActive = true;
            DatabaseManager.Connection.Update(character);
            error = "至少需要启用一个角色";
            return false;
        }

        return true;
    }
    
    public static bool UpdateCharacterByName(
        string userName,
        string oldCharacterName,
        string newCharacterName,
        string promptJson,
        bool isActive,
        out string error)
    {
        error = "";

        var character = GetByUserAndName(userName, oldCharacterName);

        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        if (string.IsNullOrWhiteSpace(newCharacterName))
        {
            newCharacterName = character.CharacterName;
        }

        var sameName = GetByUserAndName(userName, newCharacterName);

        if (sameName != null && sameName.CharacterId != character.CharacterId)
        {
            error = "角色名已被使用";
            return false;
        }

        character.CharacterName = newCharacterName;
        character.PromptJson = promptJson ?? character.PromptJson;

        if (isActive)
        {
            DisableAllCharacters(character.UserName);
            character.IsActive = true;
        }
        else
        {
            int activeCount = GetActiveCharacterCount(character.UserName);

            if (character.IsActive && activeCount <= 1)
            {
                error = "至少需要启用一个角色";
                return false;
            }

            character.IsActive = false;
        }

        DatabaseManager.Connection.Update(character);

        return true;
    }

    public static bool DeleteCharacter(string characterId, out string error)
    {
        error = "";

        var character =
            DatabaseManager.Connection.Find<CharacterProfileData>(characterId);

        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        if (character.CharacterName == DefaultDataInitializer.DefaultCharacterName)
        {
            error = "默认角色不能删除";
            return false;
        }

        string userName = character.UserName;

        DatabaseManager.Connection.Delete(character);

        if (!EnsureAtLeastOneActive(userName))
        {
            error = "删除失败：至少需要保留一个启用角色";
            return false;
        }

        return true;
    }
    
    public static bool DeleteCharacterByName(
        string userName,
        string characterName,
        out string error)
    {
        error = "";

        if (characterName == DefaultDataInitializer.DefaultCharacterName)
        {
            error = "默认角色不能删除";
            return false;
        }

        var character = GetByUserAndName(userName, characterName);

        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        var characters = GetByUserName(userName);

        if (characters.Count <= 1)
        {
            error = "至少需要保留一个角色";
            return false;
        }

        DatabaseManager.Connection.Delete(character);

        if (!EnsureAtLeastOneActive(userName))
        {
            error = "删除失败：至少需要保留一个启用角色";
            return false;
        }

        return true;
    }

    public static bool SetActiveCharacter(string userName, string characterName,out string error)
    {
        error = "";
        
        var character = GetByUserAndName(userName, characterName);

        if (character == null)
            return false;
        
        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        if (character.UserName != userName)
        {
            error = "该角色不属于当前用户";
            return false;
        }

        DisableAllCharacters(userName);

        character.IsActive = true;
        DatabaseManager.Connection.Update(character);
        
        GlobalSession.SetCurrentCharacter(character);

        return true;
    }

    private static void DisableAllCharacters(string userName)
    {
        var characters = GetByUserName(userName);

        foreach (var c in characters)
        {
            c.IsActive = false;
            DatabaseManager.Connection.Update(c);
        }
    }

    private static bool EnsureAtLeastOneActive(string userName)
    {
        var characters = GetByUserName(userName);

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
    
    public static int GetActiveCharacterCount(string userName)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<CharacterProfileData>()
            .Count(c => c.UserName == userName && c.IsActive);
    }

    public static bool ValidateActiveCharacterState(
        string userName,
        out string error)
    {
        error = "";

        int activeCount = GetActiveCharacterCount(userName);

        if (activeCount == 0)
        {
            error = "至少需要启用一个角色";
            return false;
        }

        if (activeCount > 1)
        {
            error = "最多只能启用一个角色，请先取消多余启用角色";
            return false;
        }

        return true;
    }
}
