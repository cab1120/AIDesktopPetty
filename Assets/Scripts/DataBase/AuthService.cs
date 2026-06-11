using System;

public static class AuthService
{
    public static bool Login(
        string userName,
        string password,
        string characterName,
        out string error)
    {
        error = "";

        DatabaseManager.Initialize();

        var user = UserRepository.GetByUserName(userName);

        if (user == null)
        {
            error = "用户不存在";
            return false;
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            error = "密码错误";
            return false;
        }

        var character = CharacterRepository.GetByUserAndName(
            user.UserName,
            characterName
        );

        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        user.LastLoginAtTicks = DateTime.Now.Ticks;
        DatabaseManager.Connection.Update(user);

        bool activeSuccess = CharacterRepository.SetActiveCharacter(
            user.UserName,
            character.CharacterName,
            out string activeError
        );
        if (!activeSuccess)
        {
            error = activeError;
            return false;
        }

        GlobalSession.SetSession(user, character);
        GlobalSession.RefreshCurrentCharacterFromDatabase();
        
        //登录立赠1好感度
        UserCharacterStateRepository.GetOrCreate(
            GlobalSession.CurrentUserId,
            GlobalSession.CurrentCharacterId
        );

        RelationshipService.OnLogin();
        
        return true;
    }
}
