using UnityEngine;

[CreateAssetMenu(
    fileName = "SakuraWeatherProfile",
    menuName = "VFX/Sakura Weather/Profile")]
public class SakuraWeatherProfile : ScriptableObject
{
    [Header("Global Wind")]
    public Vector3 windDirection = new Vector3(1f, 0f, 0.25f);

    [Header("Background")]
    public SakuraLayerSettings background;

    [Header("Midground")]
    public SakuraLayerSettings midground;

    [Header("Foreground")]
    public SakuraLayerSettings foreground;
}

[System.Serializable]
public class SakuraLayerSettings
{
    [Header("Spawn")]
    public float spawnRate;

    public float lifetimeMin;
    public float lifetimeMax;

    public Vector3 spawnOffset;
    public Vector3 spawnBoxSize;
    public Vector3 boundsSize;

    [Header("Appearance")]
    public float petalSizeMin;
    public float petalSizeMax;

    [Header("Movement")]
    public float fallSpeedMin;
    public float fallSpeedMax;

    public float windStrength;
    public float turbulenceStrength;

    [Header("Rotation")]
    public bool useRotationSpeed;
    public float rotationSpeedMin;
    public float rotationSpeedMax;
}