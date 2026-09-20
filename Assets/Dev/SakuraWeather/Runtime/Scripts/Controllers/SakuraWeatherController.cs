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
    
    private static readonly int CameraRightId =
        Shader.PropertyToID("CameraRight");

    private static readonly int CameraUpId =
        Shader.PropertyToID("CameraUp");

    private static readonly int CameraForwardId =
        Shader.PropertyToID("CameraForward");


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
        UpdateCameraRelativeData();
    }

    private void LateUpdate()
    {
        UpdateCameraRelativeData();
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
    
    public void SetProfile(
        SakuraWeatherProfile newProfile)
    {
        if (newProfile == null)
        {
            Debug.LogWarning(
                "[SakuraWeather] Cannot apply a null profile.",
                this);

            return;
        }

        SakuraWeatherProfile previousProfile =
            profile;

        profile = newProfile;

        if (!ValidateProfile())
        {
            profile = previousProfile;

            return;
        }

        ApplyProfile();
        UpdateCameraRelativeData();
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

        UpdateCameraRelativeData();
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


    private void UpdateCameraRelativeData()
    {
        if (cameraTransform == null ||
            profile == null)
        {
            return;
        }

        Quaternion cameraRotation =
            cameraTransform.rotation;

        Vector3 cameraRight =
            cameraRotation * Vector3.right;

        Vector3 cameraUp =
            cameraRotation * Vector3.up;

        Vector3 cameraForward =
            cameraRotation * Vector3.forward;

        UpdateLayerCameraData(
            backgroundVfx,
            profile.background.spawnOffset,
            cameraRight,
            cameraUp,
            cameraForward);

        UpdateLayerCameraData(
            midgroundVfx,
            profile.midground.spawnOffset,
            cameraRight,
            cameraUp,
            cameraForward);

        UpdateLayerCameraData(
            foregroundVfx,
            profile.foreground.spawnOffset,
            cameraRight,
            cameraUp,
            cameraForward);
    }


    private void UpdateLayerCameraData(
        VisualEffect vfx,
        Vector3 cameraLocalOffset,
        Vector3 cameraRight,
        Vector3 cameraUp,
        Vector3 cameraForward)
    {
        Vector3 worldCenter =
            CalculateWorldCenter(
                cameraTransform,
                cameraLocalOffset);

        vfx.SetVector3(
            SpawnBoxCenterId,
            worldCenter);

        vfx.SetVector3(
            CameraRightId,
            cameraRight);

        vfx.SetVector3(
            CameraUpId,
            cameraUp);

        vfx.SetVector3(
            CameraForwardId,
            cameraForward);
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
        
        valid &= RequireVector3(
            vfx,
            CameraRightId,
            "CameraRight",
            layerName);

        valid &= RequireVector3(
            vfx,
            CameraUpId,
            "CameraUp",
            layerName);

        valid &= RequireVector3(
            vfx,
            CameraForwardId,
            "CameraForward",
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
    
    private bool ValidateProfile()
    {
        if (!profile.background.IsValid(
                "Background",
                out string backgroundError))
        {
            Debug.LogError(
                $"[SakuraWeather] {backgroundError}",
                profile);

            return false;
        }

        if (!profile.midground.IsValid(
                "Midground",
                out string midgroundError))
        {
            Debug.LogError(
                $"[SakuraWeather] {midgroundError}",
                profile);

            return false;
        }

        if (!profile.foreground.IsValid(
                "Foreground",
                out string foregroundError))
        {
            Debug.LogError(
                $"[SakuraWeather] {foregroundError}",
                profile);

            return false;
        }

        return true;
    }
}