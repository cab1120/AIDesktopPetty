using System;
using System.IO;
using UnityEngine;

public static class EmotionMemory
{
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
    private static IEmotionStorage storage = new FileStorage(); 
    
    public static void Initialize()
    {
        LoadEmotion();

        // 如果没有存档
        if (currentEmotion == null)
        {
            GenerateNewEmotion();
            return;
        }

        // 检查情绪是否过期
        if (IsEmotionExpired())
        {
            GenerateNewEmotion();
        }
    }

    // =========================
    // 获取当前情绪
    // =========================

    public static EmotionData GetCurrentEmotion()
    {
        // 防止忘记初始化
        if (currentEmotion == null)
        {
            Initialize();
        }

        // 每次获取时检查是否过期
        if (IsEmotionExpired())
        {
            GenerateNewEmotion();
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

    public static void SetEmotion(EmotionData data)
    {
        currentEmotion = data;

        SaveEmotion();
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

    public static void GenerateNewEmotion()
    {
        EmotionData newEmotion =
            EmotionGenerator.GenerateEmotion();

        SetEmotion(newEmotion);
    }

    // =========================
    // 保存情绪
    // =========================

    private static void SaveEmotion()
    {
        try
        {
            // 目前使用文件存储
            storage.Save(currentEmotion); 
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Emotion Save Failed: {e}");
        }
    }

    // =========================
    // 读取情绪
    // =========================

    private static void LoadEmotion()
    {
        try
        {
            currentEmotion = storage.Load();
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Emotion Load Failed: {e}");
        }
    }

    // =========================
    // 手动重置（调试用）
    // =========================

    public static void ResetEmotion()
    {
        currentEmotion = null;

        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        GenerateNewEmotion();
    }
}

// 默认的文件存储实现
public class FileStorage : IEmotionStorage
{
    private string path = Path.Combine(Application.persistentDataPath, "iroha_emotion.json");
    public void Save(EmotionData data) => File.WriteAllText(path, JsonUtility.ToJson(data, true));
    public EmotionData Load() => File.Exists(path) ? JsonUtility.FromJson<EmotionData>(File.ReadAllText(path)) : null;
}