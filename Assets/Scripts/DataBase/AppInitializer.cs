using UnityEngine;

public class AppInitializer : MonoBehaviour
{
    private void Awake()
    {
        DatabaseManager.Initialize();

        DefaultDataInitializer.Initialize();

        EmotionMemory.Initialize();
    }

    private void OnApplicationQuit()
    {
        DatabaseManager.Close();
    }
}