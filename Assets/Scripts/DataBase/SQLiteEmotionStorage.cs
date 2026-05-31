using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SQLiteEmotionStorage : IEmotionStorage
{
    private const int DefaultKeepCount = 20;

    public void Save(
        EmotionData data)
    {
        if (data == null)
            return;
        try
        {
            DatabaseManager.Initialize();

            EmotionRecord record =
                EmotionDataMapper.ToRecord(data);

            DatabaseManager.Connection.Insert(record);

            TrimHistory(
                record.UserId,
                record.CharacterId,
                DefaultKeepCount
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存情感数据失败: {ex.Message}");
            throw;  // 或处理
        }
    }

    public EmotionData LoadLatest(
        string userId,
        string characterId)
    {
        DatabaseManager.Initialize();

        EmotionRecord record = DatabaseManager.Connection
            .Table<EmotionRecord>()
            .Where(e =>
                e.UserId == userId &&
                e.CharacterId == characterId)
            .OrderByDescending(e => e.CreatedAtTicks)
            .FirstOrDefault();

        return EmotionDataMapper.ToData(record);
    }

    public List<EmotionData> LoadHistory(
        string userId,
        string characterId,
        int limit)
    {
        DatabaseManager.Initialize();

        return DatabaseManager.Connection
            .Table<EmotionRecord>()
            .Where(e =>
                e.UserId == userId &&
                e.CharacterId == characterId)
            .OrderByDescending(e => e.CreatedAtTicks)
            .Take(limit)
            .ToList()
            .Select(EmotionDataMapper.ToData)
            .ToList();
    }

    public void TrimHistory(
        string userId,
        string characterId,
        int keepCount)
    {
        DatabaseManager.Initialize();

        List<EmotionRecord> oldRecords =
            DatabaseManager.Connection
                .Table<EmotionRecord>()
                .Where(e =>
                    e.UserId == userId &&
                    e.CharacterId == characterId)
                .OrderByDescending(e => e.CreatedAtTicks)
                .Skip(keepCount)
                .ToList();

        foreach (EmotionRecord record in oldRecords)
        {
            DatabaseManager.Connection.Delete(record);
        }
    }

    public void DeleteAll(
        string userId,
        string characterId)
    {
        DatabaseManager.Initialize();

        DatabaseManager.Connection.Execute(
            "DELETE FROM EmotionState WHERE UserId = ? AND CharacterId = ?",
            userId,
            characterId
        );
    }
}
public interface IEmotionStorage
{
    void Save(EmotionData data);

    EmotionData LoadLatest(
        string userId = "DefaultUser",
        string characterId = "DefaultCharacter"
    );

    List<EmotionData> LoadHistory(
        string userId,
        string characterId,
        int limit
    );

    void TrimHistory(
        string userId,
        string characterId,
        int keepCount
    );

    void DeleteAll(
        string userId,
        string characterId
    );
}