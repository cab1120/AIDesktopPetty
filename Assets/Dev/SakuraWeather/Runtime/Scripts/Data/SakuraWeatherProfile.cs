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
    
    public bool IsValid(
        string layerName,
        out string error)
    {
        if (spawnRate < 0f)
        {
            error =
                $"{layerName}: SpawnRate cannot be negative.";

            return false;
        }

        if (lifetimeMin < 0f ||
            lifetimeMax < lifetimeMin)
        {
            error =
                $"{layerName}: Invalid lifetime range.";

            return false;
        }

        if (petalSizeMin < 0f ||
            petalSizeMax < petalSizeMin)
        {
            error =
                $"{layerName}: Invalid petal size range.";

            return false;
        }

        if (fallSpeedMin < 0f ||
            fallSpeedMax < fallSpeedMin)
        {
            error =
                $"{layerName}: Invalid fall speed range.";

            return false;
        }

        if (boundsSize.x <= 0f ||
            boundsSize.y <= 0f ||
            boundsSize.z <= 0f)
        {
            error =
                $"{layerName}: Bounds size must be positive.";

            return false;
        }

        error = null;

        return true;
    }
}

