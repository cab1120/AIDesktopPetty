using System;
using System.IO;
using UnityEngine;

public static class EmotionMemory
{
    //默认id
    private const string DefaultUserId = "DefaultUser";
    private const string DefaultCharacterId = "DefaultCharacter";
    // 当前情绪缓存
    private static EmotionData currentEmotion;

    // 存档路径
    private static readonly string SavePath =
        Path.Combine(Application.persistentDataPath, "iroha_emotion.json");

    // =========================
    // 初始化
    // =========================

    // --- 预处理：定义当前的存储驱动 ---
    // 未来如果换数据库，只需要把 FileStorage 换成 DatabaseStorage
    private static IEmotionStorage storage = new SQLiteEmotionStorage(); 
    
    public static void Initialize()
    {
        LoadEmotion(DefaultUserId,DefaultCharacterId);

        // 如果没有存档
        if (currentEmotion == null)
        {
            GenerateNewEmotion(DefaultUserId,DefaultCharacterId);
            return;
        }
    }

    // =========================
    // 获取当前情绪
    // =========================

    public static EmotionData GetCurrentEmotion(string userId,string characterId)
    {
        LoadEmotion(userId, characterId);
        
        // 防止忘记初始化
        if (currentEmotion == null)
        {
            GenerateNewEmotion(userId,characterId);
        }

        // 每次获取时检查是否过期
        if (IsEmotionExpired())
        {
            GenerateNewEmotion(userId,characterId);
        }

        return currentEmotion;
    }
    public static EmotionData PeekCurrentEmotion()
    {
        return currentEmotion; // 可能为 null，调用方自己判断
    }
    // =========================
    // 设置情绪
    // =========================

    public static void SetEmotion(EmotionData data,string userId,string characterId)
    {
        currentEmotion = data;
        currentEmotion.UserId = userId;
        currentEmotion.CharacterId = characterId;
    }

    // =========================
    // 判断是否过期
    // =========================

    private static bool IsEmotionExpired()
    {
        if (currentEmotion == null)
            return true;

        DateTime lastTime =
            currentEmotion.GetLastUpdateTime();

        TimeSpan elapsed =
            DateTime.Now - lastTime;

        return elapsed.TotalMinutes >=
               currentEmotion.RemainingMinutes;
    }

    // =========================
    // 生成新情绪
    // =========================

    public static void GenerateNewEmotion(string userId,string characterId)
    {
        EmotionData newEmotion =
            EmotionGenerator.GenerateEmotion();

        SetEmotion(newEmotion,userId, characterId);
        
        SaveEmotion();
    }

    // =========================
    // 保存情绪
    // =========================

    private static void SaveEmotion()
    {
        try
        {
            storage.Save(
                currentEmotion);
        }
        catch (Exception e)
        {
            Debug.LogError($"Emotion Save Failed: {e}");
        }
    }

    // =========================
    // 读取情绪
    // =========================

    private static void LoadEmotion(string userId,string characterId)
    {
        try
        {
            currentEmotion = storage.LoadLatest(
                userId,
                characterId
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Emotion Load Failed: {e}");
        }
    }

    // =========================
    // 手动重置（调试用）
    // =========================

    public static void ResetEmotion()
    {
        currentEmotion = null;

        storage.DeleteAll(
            DefaultUserId,
            DefaultCharacterId
        );

        GenerateNewEmotion(DefaultUserId,DefaultCharacterId);
    }
}

// 默认的文件存储实现
/*public class FileStorage : IEmotionStorage
{
    private string path = Path.Combine(Application.persistentDataPath, "iroha_emotion.json");
    public void Save(EmotionData data) => File.WriteAllText(path, JsonUtility.ToJson(data, true));
    public EmotionData Load() => File.Exists(path) ? JsonUtility.FromJson<EmotionData>(File.ReadAllText(path)) : null;
}*/