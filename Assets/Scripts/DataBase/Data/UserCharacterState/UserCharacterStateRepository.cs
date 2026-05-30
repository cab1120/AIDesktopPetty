using System;

public static class UserCharacterStateRepository
{
    public static UserCharacterStateData GetOrCreate(
        string userId,
        string characterId)
    {
        DatabaseManager.Initialize();

        string stateId = userId + "_" + characterId;

        var state = DatabaseManager.Connection.Find<UserCharacterStateData>(stateId);

        if (state != null)
            return state;

        state = new UserCharacterStateData
        {
            StateId = stateId,
            UserId = userId,
            CharacterId = characterId,
            Favorability = 0,
            TrustValue = 0.5f,
            InteractionDays = 0,
            CreatedAtTicks = DateTime.Now.Ticks,
            LastInteractionAtTicks = DateTime.Now.Ticks
        };

        DatabaseManager.Connection.Insert(state);

        return state;
    }

    public static void AddFavorability(
        string userId,
        string characterId,
        int value)
    {
        var state = GetOrCreate(userId, characterId);

        state.Favorability += value;
        state.LastInteractionAtTicks = DateTime.Now.Ticks;

        DatabaseManager.Connection.Update(state);
    }
}