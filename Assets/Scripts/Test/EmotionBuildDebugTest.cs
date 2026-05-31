using System;
using UnityEngine;
using System.Collections.Generic;

public class EmotionBuildDebugTest : MonoBehaviour
{
    private SQLiteEmotionStorage storage = new SQLiteEmotionStorage();

    private const string UserId = "DefaultUser";
    private const string CharacterId = "DefaultCharacter";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            EmotionData data = EmotionGenerator.GenerateEmotion();
            data.UserId = UserId;
            data.CharacterId = CharacterId;
            data.SetLastUpdate(System.DateTime.Now);

            storage.Save(data);

            Debug.Log("已写入一条情绪：" + data.CurrentEmotion);
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            List<EmotionData> history =
                storage.LoadHistory(UserId, CharacterId, 100);

            Debug.Log("当前情绪历史数量：" + history.Count);

            foreach (var item in history)
            {
                Debug.Log(item.CurrentEmotion + " / " + item.GetLastUpdateTime());
            }
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            try
            {
                EmotionData latest =
                    storage.LoadLatest(UserId, CharacterId);

                Debug.Log("最新情绪：" + latest.CurrentEmotion);
                Debug.Log("更新时间：" + latest.GetLastUpdateTime());
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取最新情绪失败: {ex.Message}");
                throw;  // 或处理
            }
        }

        if (Input.GetKeyDown(KeyCode.F12))
        {
            storage.DeleteAll(UserId, CharacterId);
            Debug.Log("delete all");
        }
    }
}