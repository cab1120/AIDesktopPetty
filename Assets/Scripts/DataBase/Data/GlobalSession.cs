using System;

public static class GlobalSession
{
    public static string CurrentUserId { get; private set; }
    public static string CurrentUserName { get; private set; }
    public static string CurrentRole { get; private set; }

    public static string CurrentCharacterId { get; private set; }
    public static string CurrentCharacterName { get; private set; }
    
    /// <summary>
    /// 当前会话代数。
    ///
    /// 它不是“登录次数”，而是用于判断异步任务是否仍属于当前会话的 Generation Token。
    /// 每次建立新登录会话、切换角色或清除会话时都会发生变化。
    /// </summary>
    public static long SessionVersion { get; private set; }

    /// <summary>
    /// 会话代数发生变化时触发。
    ///
    /// 后续 ConversationService 会监听这个事件，
    /// 用来取消属于旧 Session 的聊天请求。
    /// </summary>
    public static event Action<long> SessionVersionChanged;

    public static bool IsLoggedIn =>
        !string.IsNullOrEmpty(CurrentUserId) &&
        !string.IsNullOrEmpty(CurrentCharacterId);

    public static void SetSession(
        UserData user,
        CharacterProfileData character)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (character == null)
            throw new ArgumentNullException(nameof(character));
        if (!string.Equals(user.UserName, character.UserName, StringComparison.Ordinal))
            throw new ArgumentException("角色不属于当前登录用户", nameof(character));
        
        CurrentUserId = user.UserId;
        CurrentUserName = user.UserName;
        CurrentRole = user.Role;

        CurrentCharacterId = character.CharacterId;
        CurrentCharacterName = character.CharacterName;
        
        // 一次成功的 SetSession 被视为一个新的 Session。
        // 即使重新登录的是同一个用户和同一个角色，
        // 旧请求也不应该继续属于这个新 Session。
        AdvanceSessionVersion();
    }

    public static void Clear()
    {
        bool hadSession =
            !string.IsNullOrEmpty(CurrentUserId) ||
            !string.IsNullOrEmpty(CurrentCharacterId);

        CurrentUserId = null;
        CurrentUserName = null;
        CurrentRole = null;

        CurrentCharacterId = null;
        CurrentCharacterName = null;
        
        // 必须让此前的异步请求立即失效。
        if (hadSession)
        {
            AdvanceSessionVersion();
        }
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
    
    public static void SetCurrentCharacter(CharacterProfileData character)
    {
        if (character == null)
            return;

        // 管理界面可以启用其他用户的角色；该操作不能改变当前登录身份。
        if (!IsLoggedIn || !string.Equals(
                CurrentUserName, character.UserName, StringComparison.Ordinal))
            return;

        // Session 的身份判断必须使用稳定 ID，
        bool characterChanged =
            !string.Equals(
                CurrentCharacterId,
                character.CharacterId,
                StringComparison.Ordinal);

        CurrentCharacterId = character.CharacterId;
        CurrentCharacterName = character.CharacterName;

        if (characterChanged)
        {
            AdvanceSessionVersion();
        }
    }

    public static void RefreshCurrentCharacterFromDatabase()
    {
        if (string.IsNullOrEmpty(CurrentUserName))
            return;

        var activeCharacter =
            CharacterRepository.GetActiveCharacter(CurrentUserName);

        if (activeCharacter != null)
        {
            SetCurrentCharacter(activeCharacter);
        }
    }
    
    /// <summary>
    /// 捕获当前 Session 的不可变快照。
    ///
    /// 异步请求开始以后应该使用这个 Snapshot
    /// </summary>
    public static SessionSnapshot CaptureSnapshot()
    {
        return new SessionSnapshot(
            CurrentUserId,
            CurrentUserName,
            CurrentRole,
            CurrentCharacterId,
            CurrentCharacterName,
            SessionVersion
        );
    }

    /// <summary>
    /// 判断一个之前捕获的 SessionSnapshot
    /// 是否仍属于当前有效 Session。
    /// </summary>
    public static bool IsCurrent(
        SessionSnapshot snapshot)
    {
        if (!snapshot.IsLoggedIn || !IsLoggedIn)
            return false;

        if (snapshot.SessionVersion != SessionVersion)
            return false;

        // 真正的身份比较使用稳定 ID。
        // Name 属于可修改的展示数据，不参与 Session 身份判断。
        return
            string.Equals(
                snapshot.UserId,
                CurrentUserId,
                StringComparison.Ordinal)
            &&
            string.Equals(
                snapshot.CharacterId,
                CurrentCharacterId,
                StringComparison.Ordinal);
    }

    private static void AdvanceSessionVersion()
    {
        unchecked
        {
            SessionVersion++;
        }

        SessionVersionChanged?.Invoke(SessionVersion);
    }
}
