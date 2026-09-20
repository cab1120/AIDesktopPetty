#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.VFX;

public sealed partial class SakuraWeatherController
{
    [Header("Debug Gizmos")]
    [SerializeField]
    private bool showDebugGizmos = true;

    [SerializeField]
    private bool showSpawnVolumes = true;

    [SerializeField]
    private bool showBounds = true;

    [SerializeField]
    private bool showCameraLinks = true;
    
    [SerializeField]
    private SakuraWeatherProfile debugLowProfile;

    [SerializeField]
    private SakuraWeatherProfile debugMediumProfile;

    [SerializeField]
    private SakuraWeatherProfile debugHighProfile;


    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos ||
            profile == null)
        {
            return;
        }

        Camera debugCamera = targetCamera;

        if (debugCamera == null)
        {
            debugCamera = Camera.main;
        }

        if (debugCamera == null)
        {
            return;
        }

        Transform cameraTf =
            debugCamera.transform;

        DrawLayerGizmos(
            cameraTf,
            profile.background,
            new Color(0.25f, 0.8f, 1f, 1f));

        DrawLayerGizmos(
            cameraTf,
            profile.midground,
            new Color(1f, 0.8f, 0.2f, 1f));

        DrawLayerGizmos(
            cameraTf,
            profile.foreground,
            new Color(1f, 0.35f, 0.8f, 1f));
    }


    private void DrawLayerGizmos(
        Transform cameraTf,
        SakuraLayerSettings settings,
        Color layerColor)
    {
        Vector3 worldCenter =
            CalculateWorldCenter(
                cameraTf,
                settings.spawnOffset);

        // Spawn Volume
        if (showSpawnVolumes)
        {
            Matrix4x4 previousMatrix =
                Gizmos.matrix;

            Gizmos.color =
                layerColor;

            Gizmos.matrix =
                Matrix4x4.TRS(
                    worldCenter,
                    cameraTf.rotation,
                    Vector3.one);

            Gizmos.DrawWireCube(
                Vector3.zero,
                settings.spawnBoxSize);

            Gizmos.matrix =
                previousMatrix;
        }

        // Manual VFX Bounds
        if (showBounds)
        {
            Color boundsColor =
                new Color(
                    layerColor.r,
                    layerColor.g,
                    layerColor.b,
                    0.35f);

            Gizmos.color = boundsColor;

            Gizmos.DrawWireCube(
                worldCenter,
                settings.boundsSize);
        }

        // Camera -> Spawn Center
        if (showCameraLinks)
        {
            Gizmos.color = layerColor;

            Gizmos.DrawLine(
                cameraTf.position,
                worldCenter);

            Gizmos.DrawSphere(
                worldCenter,
                0.05f);
        }
    }
    
    [ContextMenu("Debug/Print VFX Runtime State")]
    private void PrintVfxRuntimeState()
    {
        PrintLayerRuntimeState(
            "Background",
            backgroundVfx);

        PrintLayerRuntimeState(
            "Midground",
            midgroundVfx);

        PrintLayerRuntimeState(
            "Foreground",
            foregroundVfx);
    }


    private void PrintLayerRuntimeState(
        string layerName,
        VisualEffect vfx)
    {
        if (vfx == null)
        {
            Debug.LogWarning(
                $"[SakuraWeather] {layerName} VFX is null.",
                this);

            return;
        }

        Vector3 spawnCenter =
            vfx.HasVector3(SpawnBoxCenterId)
                ? vfx.GetVector3(SpawnBoxCenterId)
                : Vector3.zero;

        Vector3 boundsSize =
            vfx.HasVector3(BoundsSizeId)
                ? vfx.GetVector3(BoundsSizeId)
                : Vector3.zero;

        Debug.Log(
            $"[SakuraWeather] {layerName}\n" +
            $"Alive Particles: {vfx.aliveParticleCount}\n" +
            $"Culled: {vfx.culled}\n" +
            $"Spawn Center: {spawnCenter}\n" +
            $"Bounds Size: {boundsSize}",
            vfx);
    }
    
    [ContextMenu("Debug/Quality/Low")]
    private void DebugSetLow()
    {
        if (debugLowProfile != null)
        {
            SetProfile(debugLowProfile);
        }
    }


    [ContextMenu("Debug/Quality/Medium")]
    private void DebugSetMedium()
    {
        if (debugMediumProfile != null)
        {
            SetProfile(debugMediumProfile);
        }
    }


    [ContextMenu("Debug/Quality/High")]
    private void DebugSetHigh()
    {
        if (debugHighProfile != null)
        {
            SetProfile(debugHighProfile);
        }
    }
}

#endif