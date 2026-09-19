using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public sealed partial class SakuraWeatherController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField]
    private SakuraWeatherProfile profile;

    [Header("Camera")]
    [SerializeField]
    private Camera targetCamera;

    [Header("VFX Layers")]
    [SerializeField]
    private VisualEffect backgroundVfx;

    [SerializeField]
    private VisualEffect midgroundVfx;

    [SerializeField]
    private VisualEffect foregroundVfx;

    private Transform cameraTransform;

    // Common VFX property IDs
    private static readonly int SpawnRateId =
        Shader.PropertyToID("SpawnRate");

    private static readonly int LifetimeMinId =
        Shader.PropertyToID("LifetimeMin");

    private static readonly int LifetimeMaxId =
        Shader.PropertyToID("LifetimeMax");

    private static readonly int SpawnBoxCenterId =
        Shader.PropertyToID("SpawnBoxCenter");

    private static readonly int SpawnBoxSizeId =
        Shader.PropertyToID("SpawnBoxSize");
    
    private static readonly int BoundsSizeId =
        Shader.PropertyToID("BoundsSize");

    private static readonly int PetalSizeMinId =
        Shader.PropertyToID("PetalSizeMin");

    private static readonly int PetalSizeMaxId =
        Shader.PropertyToID("PetalSizeMax");

    private static readonly int FallSpeedMinId =
        Shader.PropertyToID("FallSpeedMin");

    private static readonly int FallSpeedMaxId =
        Shader.PropertyToID("FallSpeedMax");

    private static readonly int WindDirectionId =
        Shader.PropertyToID("WindDirection");

    private static readonly int WindStrengthId =
        Shader.PropertyToID("WindStrength");

    private static readonly int TurbulenceStrengthId =
        Shader.PropertyToID("TurbulenceStrength");

    // Only Midground / Foreground use these.
    private static readonly int RotationSpeedMinId =
        Shader.PropertyToID("RotationSpeedMin");

    private static readonly int RotationSpeedMaxId =
        Shader.PropertyToID("RotationSpeedMax");


    private void Awake()
    {
        if (!TryInitializeReferences())
        {
            enabled = false;
            return;
        }

        if (!ValidateVfxInterfaces())
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (!enabled)
        {
            return;
        }

        ApplyProfile();
        UpdateSpawnCenters();
    }

    private void LateUpdate()
    {
        UpdateSpawnCenters();
    }


    public void ApplyProfile()
    {
        if (profile == null)
        {
            return;
        }

        ApplyLayerSettings(
            backgroundVfx,
            profile.background,
            false);

        ApplyLayerSettings(
            midgroundVfx,
            profile.midground,
            true);

        ApplyLayerSettings(
            foregroundVfx,
            profile.foreground,
            true);
    }


    public void SetTargetCamera(Camera newCamera)
    {
        if (newCamera == null)
        {
            Debug.LogWarning(
                "[SakuraWeather] Target Camera cannot be null.",
                this);

            return;
        }

        targetCamera = newCamera;
        cameraTransform = targetCamera.transform;

        UpdateSpawnCenters();
    }


    private bool TryInitializeReferences()
    {
        if (profile == null)
        {
            Debug.LogError(
                "[SakuraWeather] SakuraWeatherProfile is missing.",
                this);

            return false;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError(
                "[SakuraWeather] Target Camera is missing and no MainCamera was found.",
                this);

            return false;
        }

        if (backgroundVfx == null ||
            midgroundVfx == null ||
            foregroundVfx == null)
        {
            Debug.LogError(
                "[SakuraWeather] One or more VisualEffect layer references are missing.",
                this);

            return false;
        }

        cameraTransform = targetCamera.transform;

        return true;
    }


    private void ApplyLayerSettings(
        VisualEffect vfx,
        SakuraLayerSettings settings,
        bool applyRotationSpeed)
    {
        vfx.SetFloat(
            SpawnRateId,
            settings.spawnRate);

        vfx.SetFloat(
            LifetimeMinId,
            settings.lifetimeMin);

        vfx.SetFloat(
            LifetimeMaxId,
            settings.lifetimeMax);

        vfx.SetVector3(
            SpawnBoxSizeId,
            settings.spawnBoxSize);
        
        vfx.SetVector3(
            BoundsSizeId,
            settings.boundsSize);

        vfx.SetFloat(
            PetalSizeMinId,
            settings.petalSizeMin);

        vfx.SetFloat(
            PetalSizeMaxId,
            settings.petalSizeMax);

        vfx.SetFloat(
            FallSpeedMinId,
            settings.fallSpeedMin);

        vfx.SetFloat(
            FallSpeedMaxId,
            settings.fallSpeedMax);

        vfx.SetVector3(
            WindDirectionId,
            profile.windDirection);

        vfx.SetFloat(
            WindStrengthId,
            settings.windStrength);

        vfx.SetFloat(
            TurbulenceStrengthId,
            settings.turbulenceStrength);

        if (applyRotationSpeed)
        {
            vfx.SetFloat(
                RotationSpeedMinId,
                settings.rotationSpeedMin);

            vfx.SetFloat(
                RotationSpeedMaxId,
                settings.rotationSpeedMax);
        }
    }


    private void UpdateSpawnCenters()
    {
        if (cameraTransform == null ||
            profile == null)
        {
            return;
        }

        SetSpawnCenter(
            backgroundVfx,
            profile.background.spawnOffset);

        SetSpawnCenter(
            midgroundVfx,
            profile.midground.spawnOffset);

        SetSpawnCenter(
            foregroundVfx,
            profile.foreground.spawnOffset);
    }


    private void SetSpawnCenter(
        VisualEffect vfx,
        Vector3 cameraLocalOffset)
    {
        Vector3 worldCenter =
            CalculateWorldCenter(
                cameraTransform,
                cameraLocalOffset);

        vfx.SetVector3(
            SpawnBoxCenterId,
            worldCenter);
    }
    
    private static Vector3 CalculateWorldCenter(
        Transform cameraTransform,
        Vector3 cameraLocalOffset)
    {
        return cameraTransform.position
               + cameraTransform.rotation * cameraLocalOffset;
    }
    
    private bool ValidateVfxInterfaces()
    {
        bool valid = true;

        valid &= ValidateLayerInterface(
            backgroundVfx,
            "Background",
            false);

        valid &= ValidateLayerInterface(
            midgroundVfx,
            "Midground",
            true);

        valid &= ValidateLayerInterface(
            foregroundVfx,
            "Foreground",
            true);

        return valid;
    }
    
    private bool ValidateLayerInterface(
        VisualEffect vfx,
        string layerName,
        bool requiresRotationSpeed)
    {
        bool valid = true;

        valid &= RequireFloat(
            vfx,
            SpawnRateId,
            "SpawnRate",
            layerName);

        valid &= RequireFloat(
            vfx,
            LifetimeMinId,
            "LifetimeMin",
            layerName);

        valid &= RequireFloat(
            vfx,
            LifetimeMaxId,
            "LifetimeMax",
            layerName);

        valid &= RequireVector3(
            vfx,
            SpawnBoxCenterId,
            "SpawnBoxCenter",
            layerName);

        valid &= RequireVector3(
            vfx,
            SpawnBoxSizeId,
            "SpawnBoxSize",
            layerName);

        valid &= RequireVector3(
            vfx,
            BoundsSizeId,
            "BoundsSize",
            layerName);

        valid &= RequireFloat(
            vfx,
            PetalSizeMinId,
            "PetalSizeMin",
            layerName);

        valid &= RequireFloat(
            vfx,
            PetalSizeMaxId,
            "PetalSizeMax",
            layerName);

        valid &= RequireFloat(
            vfx,
            FallSpeedMinId,
            "FallSpeedMin",
            layerName);

        valid &= RequireFloat(
            vfx,
            FallSpeedMaxId,
            "FallSpeedMax",
            layerName);

        valid &= RequireVector3(
            vfx,
            WindDirectionId,
            "WindDirection",
            layerName);

        valid &= RequireFloat(
            vfx,
            WindStrengthId,
            "WindStrength",
            layerName);

        valid &= RequireFloat(
            vfx,
            TurbulenceStrengthId,
            "TurbulenceStrength",
            layerName);

        if (requiresRotationSpeed)
        {
            valid &= RequireFloat(
                vfx,
                RotationSpeedMinId,
                "RotationSpeedMin",
                layerName);

            valid &= RequireFloat(
                vfx,
                RotationSpeedMaxId,
                "RotationSpeedMax",
                layerName);
        }

        return valid;
    }
    
    private bool RequireFloat(
        VisualEffect vfx,
        int propertyId,
        string propertyName,
        string layerName)
    {
        if (vfx.HasFloat(propertyId))
        {
            return true;
        }

        Debug.LogError(
            $"[SakuraWeather] {layerName} VFX " +
            $"is missing Float property '{propertyName}'.",
            vfx);

        return false;
    }


    private bool RequireVector3(
        VisualEffect vfx,
        int propertyId,
        string propertyName,
        string layerName)
    {
        if (vfx.HasVector3(propertyId))
        {
            return true;
        }

        Debug.LogError(
            $"[SakuraWeather] {layerName} VFX " +
            $"is missing Vector3 property '{propertyName}'.",
            vfx);

        return false;
    }
}