using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public sealed class DesktopPetLayoutController
    : MonoBehaviour
{
    public static DesktopPetLayoutController
        Instance { get; private set; }


    [Header("Dependencies")]

    [SerializeField]
    private CanvasScaler canvasScaler;


    [SerializeField]
    private WindowSizeController
        windowSizeController;


    [Header("Layout Profiles")]

    [SerializeField]
    private DesktopPetLayoutProfile[]
        layoutProfiles;


    public DesktopPetLayoutMode
        CurrentMode { get; private set; }


    private readonly Dictionary<
        DesktopPetLayoutMode,
        DesktopPetLayoutProfile>
        _profiles =
            new Dictionary<
                DesktopPetLayoutMode,
                DesktopPetLayoutProfile>();


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        BuildProfileDictionary();


        ConfigureCanvasScaler();
    }


    private void BuildProfileDictionary()
    {
        _profiles.Clear();


        if (layoutProfiles == null)
        {
            Debug.LogError(
                "[DesktopPetLayoutController] " +
                "Layout profiles are missing."
            );

            return;
        }


        foreach (
            DesktopPetLayoutProfile profile
            in layoutProfiles)
        {
            if (_profiles.ContainsKey(
                    profile.Mode))
            {
                Debug.LogWarning(
                    "[DesktopPetLayoutController] " +
                    $"Duplicate layout profile: " +
                    $"{profile.Mode}"
                );
            }


            _profiles[
                profile.Mode
            ] = profile;
        }
    }


    private void ConfigureCanvasScaler()
    {
        if (canvasScaler == null)
        {
            Debug.LogError(
                "[DesktopPetLayoutController] " +
                "CanvasScaler is missing."
            );

            return;
        }


        canvasScaler.enabled =
            true;


        canvasScaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;


        canvasScaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode
                .MatchWidthOrHeight;
    }


    public bool ApplyLayout(
        DesktopPetLayoutMode mode)
    {
        if (!_profiles.TryGetValue(
                mode,
                out DesktopPetLayoutProfile profile))
        {
            Debug.LogError(
                "[DesktopPetLayoutController] " +
                $"Layout profile not found: {mode}"
            );

            return false;
        }


        bool windowSuccess =
            ApplyWindowSize(
                profile
            );


        ApplyCanvasProfile(
            profile
        );


        CurrentMode =
            mode;


        Debug.Log(
            "[DesktopPetLayoutController] " +
            $"Layout={mode}, " +
            $"Window=" +
            $"{profile.WindowWidth}x" +
            $"{profile.WindowHeight}, " +
            $"Reference=" +
            $"{profile.ReferenceResolution.x}x" +
            $"{profile.ReferenceResolution.y}"
        );


        return windowSuccess;
    }


    private bool ApplyWindowSize(
        DesktopPetLayoutProfile profile)
    {
        if (windowSizeController == null)
        {
            Debug.LogError(
                "[DesktopPetLayoutController] " +
                "WindowSizeController is missing."
            );

            return false;
        }


        return
            windowSizeController
                .SetLogicalSize(
                    profile.WindowWidth,
                    profile.WindowHeight
                );
    }


    private void ApplyCanvasProfile(
        DesktopPetLayoutProfile profile)
    {
        if (canvasScaler == null)
        {
            return;
        }


        canvasScaler.enabled =
            true;


        canvasScaler.referenceResolution =
            profile.ReferenceResolution;


        canvasScaler.matchWidthOrHeight =
            profile.MatchWidthOrHeight;
    }
}