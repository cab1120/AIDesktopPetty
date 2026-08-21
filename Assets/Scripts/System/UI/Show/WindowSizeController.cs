using UnityEngine;

using Platform.Windows;


public sealed class WindowSizeController
    : MonoBehaviour
{
    // ======================================================
    // Logical Window Sizes
    // ======================================================

    [Header("Logical Window Size")]

    [SerializeField]
    private WindowLogicalSize
        expandedSize =
            new WindowLogicalSize(
                432,
                768
            );


    [SerializeField]
    private WindowLogicalSize
        collapsedSize =
            new WindowLogicalSize(
                180,
                250
            );


    // ======================================================
    // Window Policy
    // ======================================================

    [Header("Window Policy")]

    [SerializeField]
    private bool alwaysOnTop =
        true;


    private IWindowService
        _windowService;


    // ======================================================
    // Unity Lifecycle
    // ======================================================

    private void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        _windowService =
            WindowsPlatformBootstrap
                .WindowService;


        if (_windowService ==
            null)
        {
            Debug.LogError(
                "[WindowSizeController] " +
                "WindowService is not available."
            );


            return;
        }


        if (!_windowService
                .IsInitialized)
        {
            Debug.LogError(
                "[WindowSizeController] " +
                "WindowService is not initialized."
            );


            return;
        }


        // TopMost 与 Resize 已经完全独立。
        if (alwaysOnTop)
        {
            _windowService
                .SetTopMost(
                    true
                );
        }


        CollapseFirst();

#endif
    }


    // ======================================================
    // Public Window State API
    // ======================================================

    public void ToggleWindow(
        bool isExpanded)
    {
        if (isExpanded)
        {
            Expand();
        }
        else
        {
            Collapse();
        }
    }


    public void Expand()
    {
        ApplySize(
            expandedSize,
            "Expanded"
        );
    }


    public void Collapse()
    {
        ApplySize(
            collapsedSize,
            "Collapsed"
        );
    }


    // ======================================================
    // Initial State
    // ======================================================

    private void CollapseFirst()
    {
        ApplySize(
            collapsedSize,
            "InitialCollapsed"
        );
    }


    // ======================================================
    // Internal
    // ======================================================

    private void ApplySize(
        WindowLogicalSize size,
        string stateName)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        if (_windowService ==
            null)
        {
            Debug.LogError(
                "[WindowSizeController] " +
                "Cannot resize: " +
                "WindowService is null."
            );


            return;
        }


        bool success =
            _windowService
                .SetLogicalSize(
                    size
                );


        if (!success)
        {
            Debug.LogError(
                "[WindowSizeController] " +
                $"Failed to apply " +
                $"{stateName} size: {size}"
            );
        }

#endif
    }
    public bool SetLogicalSize(
        int width,
        int height)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    if (_windowService == null)
    {
        Debug.LogError(
            "[WindowSizeController] " +
            "WindowService is null."
        );

        return false;
    }


    WindowLogicalSize size =
        new WindowLogicalSize(
            width,
            height
        );


    bool success =
        _windowService
            .SetLogicalSize(
                size
            );


    if (!success)
    {
        Debug.LogError(
            "[WindowSizeController] " +
            $"Failed to set logical size: " +
            $"{width}x{height}"
        );
    }


    return success;

#else

        return false;

#endif
    }
}