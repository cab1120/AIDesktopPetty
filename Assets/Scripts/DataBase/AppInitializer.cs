using UnityEngine;

public class AppInitializer : MonoBehaviour
{
    public static string StartupError { get; private set; }

    private void Awake()
    {
        AppLogService.Start();
        try
        {
            DatabaseManager.Initialize();
            DefaultDataInitializer.Initialize();
            EmotionMemory.Initialize();
            StartupError = null;
        }
        catch (System.Exception ex)
        {
            StartupError = "应用初始化失败: " + ex.Message;
            Debug.LogError(StartupError);
        }
    }

    private void OnApplicationQuit()
    {
        DatabaseManager.Close();
        AppLogService.Stop();
    }
}
