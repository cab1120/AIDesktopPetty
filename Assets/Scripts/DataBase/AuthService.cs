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

        var character = CharacterRepository.GetByName(characterName);

        if (character == null)
        {
            error = "角色不存在";
            return false;
        }

        user.LastLoginAtTicks = DateTime.Now.Ticks;
        DatabaseManager.Connection.Update(user);

        CharacterRepository.SetActiveCharacter(user.UserId, character.CharacterId);

        GlobalSession.SetSession(user, character);

        return true;
    }
}