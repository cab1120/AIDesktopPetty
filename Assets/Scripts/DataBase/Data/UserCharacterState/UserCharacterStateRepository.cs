using System;

public static class UserCharacterStateRepository
{
    private const int MinFavorability = 0;
    private const int MaxFavorability = 100;

    private const float MinTrust = 0f;
    private const float MaxTrust = 1f;

    public static UserCharacterStateData GetOrCreate(
        string userId,
        string characterId)
    {
        DatabaseManager.Initialize();

        string stateId = BuildStateId(userId, characterId);

        var state = DatabaseManager.Connection
            .Find<UserCharacterStateData>(stateId);

        if (state != null)
            return state;

        state = new UserCharacterStateData
        {
            StateId = stateId,
            UserId = userId,
            CharacterId = characterId,
            Favorability = 20,     // 不建议从 0 开始，否则关系太冷
            TrustValue = 0.5f,
            InteractionDays = 0,
            CreatedAtTicks = DateTime.Now.Ticks,
            LastInteractionAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(state);
        return state;
    }

    public static UserCharacterStateData Get(
        string userId,
        string characterId)
    {
        DatabaseManager.Initialize();

        string stateId = BuildStateId(userId, characterId);
        return DatabaseManager.Connection
            .Find<UserCharacterStateData>(stateId);
    }

    public static void Update(UserCharacterStateData state)
    {
        DatabaseManager.Initialize();

        if (state == null)
            return;

        DatabaseManager.Connection.Update(state);
    }
    

    public static void ApplyFavorabilityChange(
        string userId,
        string characterId,
        int rawDelta,
        bool updateInteractionTime)
    {
        var state = GetOrCreate(userId, characterId);

        ApplyTimeDecayIfNeeded(state);

        int finalDelta = CalculateFavorabilityDelta(
            state.Favorability,
            rawDelta
        );

        state.Favorability = ClampInt(
            state.Favorability + finalDelta,
            MinFavorability,
            MaxFavorability
        );

        if (updateInteractionTime)
            state.LastInteractionAtTicks = DateTime.Now.Ticks;

        DatabaseManager.Connection.Update(state);
    }

    public static void ApplyTrustChange(
        string userId,
        string characterId,
        float delta,
        bool updateInteractionTime)
    {
        var state = GetOrCreate(userId, characterId);

        ApplyTimeDecayIfNeeded(state);

        state.TrustValue = ClampFloat(
            state.TrustValue + delta,
            MinTrust,
            MaxTrust
        );

        if (updateInteractionTime)
            state.LastInteractionAtTicks = DateTime.Now.Ticks;

        DatabaseManager.Connection.Update(state);
    }

    public static void UpdateInteractionDays(
        string userId,
        string characterId)
    {
        var state = GetOrCreate(userId, characterId);

        DateTime lastTime = new DateTime(state.LastInteractionAtTicks);
        DateTime now = DateTime.Now;

        bool isDifferentDay = lastTime.Date != now.Date;
        bool isYesterday = lastTime.Date == now.Date.AddDays(-1);

        if (isDifferentDay)
        {
            if (isYesterday)
                state.InteractionDays += 1;
            else
                state.InteractionDays = 1;
        }

        state.LastInteractionAtTicks = now.Ticks;

        DatabaseManager.Connection.Update(state);
    }

    public static void ApplyTimeDecay(
        string userId,
        string characterId)
    {
        var state = GetOrCreate(userId, characterId);

        ApplyTimeDecayIfNeeded(state);

        state.LastInteractionAtTicks = DateTime.Now.Ticks;

        DatabaseManager.Connection.Update(state);
    }

    private static void ApplyTimeDecayIfNeeded(UserCharacterStateData state)
    {
        if (state == null)
            return;

        DateTime lastTime = new DateTime(state.LastInteractionAtTicks);
        DateTime now = DateTime.Now;

        double days = (now.Date - lastTime.Date).TotalDays;

        if (days < 1)
            return;

        int favorabilityDecay = 0;
        float trustDecay = 0f;

        if (days >= 7)
        {
            favorabilityDecay = 12;
            trustDecay = 0.03f;
        }
        else if (days >= 3)
        {
            favorabilityDecay = 6;
            trustDecay = 0.01f;
        }
        else if (days >= 1)
        {
            favorabilityDecay = 2;
            trustDecay = 0f;
        }

        state.Favorability = ClampInt(
            state.Favorability - favorabilityDecay,
            MinFavorability,
            MaxFavorability
        );

        state.TrustValue = ClampFloat(
            state.TrustValue - trustDecay,
            MinTrust,
            MaxTrust
        );
    }

    private static int CalculateFavorabilityDelta(
        int currentFavorability,
        int rawDelta)
    {
        if (rawDelta <= 0)
            return rawDelta;

        if (currentFavorability >= 80)
        {
            // 80 以上很难涨
            return rawDelta >= 3 ? 1 : 0;
        }

        if (currentFavorability >= 50)
        {
            // 50 以上涨幅减半
            return Math.Max(1, rawDelta / 2);
        }

        return rawDelta;
    }

    public static string BuildStateId(string userId, string characterId)
    {
        return userId + "_" + characterId;
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static float ClampFloat(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}