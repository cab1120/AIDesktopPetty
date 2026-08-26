using System;
using UnityEngine;

using Platform.Windows;


public sealed class WindowSnapController
    : MonoBehaviour
{
    // ======================================================
    // Snap Configuration
    // ======================================================

    [Header("Snap")]

    [Tooltip(
        "窗口距离显示器工作区边缘多少逻辑单位时触发吸附。")]
    [SerializeField]
    private int snapDistance =
        20;


    [Header("Auto Hide")]

    [Tooltip(
        "隐藏后留在显示器内的逻辑宽度。")]
    [SerializeField]
    private int hideOffset =
        80;


    [Tooltip(
        "吸附后的鼠标位置检测间隔。")]
    [SerializeField]
    private float autoHideCheckInterval =
        0.05f;


    // ======================================================
    // Runtime State
    // ======================================================

    private IWindowService
        _windowService;


    private bool _wasMouseDown;


    private bool _isHidden;


    private SnapEdge _currentEdge =
        SnapEdge.None;


    // ======================================================
    // Cached Snap State
    // ======================================================

    private int _cachedWindowWidth;

    private int _cachedWindowHeight;

    private int _cachedSnapY;


    private WindowMonitorInfo
        _cachedMonitor;


    private float _nextAutoHideCheckTime;


    // ======================================================
    // Snap Edge
    // ======================================================

    [Flags]
    private enum SnapEdge
    {
        None = 0,

        Left = 1 << 0,

        Right = 1 << 1,

        Top = 1 << 2,

        Bottom = 1 << 3
    }


    // ======================================================
    // Unity Lifecycle
    // ======================================================

    private void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        _windowService =
            WindowsPlatformBootstrap
                .WindowService;


        if (_windowService == null)
        {
            Debug.LogError(
                "[WindowSnapController] " +
                "WindowService is not available."
            );


            enabled =
                false;


            return;
        }


        if (!_windowService.IsInitialized)
        {
            Debug.LogError(
                "[WindowSnapController] " +
                "WindowService is not initialized."
            );


            enabled =
                false;


            return;
        }


        _wasMouseDown =
            _windowService
                .IsLeftMouseButtonDown();

#endif
    }


    private void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        HandleMouseRelease();


        if (Time.unscaledTime >=
            _nextAutoHideCheckTime)
        {
            _nextAutoHideCheckTime =
                Time.unscaledTime +
                autoHideCheckInterval;


            HandleAutoHide();
        }

#endif
    }


    // ======================================================
    // Mouse Release
    // ======================================================

    private void HandleMouseRelease()
    {
        bool isMouseDown =
            _windowService
                .IsLeftMouseButtonDown();


        // 与旧逻辑保持一致：
        //
        // 上一帧按下
        // 当前帧松开
        //
        // → 检查窗口是否应该吸附。
        if (_wasMouseDown &&
            !isMouseDown)
        {
            TrySnapToEdge();
        }


        _wasMouseDown =
            isMouseDown;
    }


    // ======================================================
    // Snap
    // ======================================================

    private void TrySnapToEdge()
    {
        if (!_windowService
                .TryGetWindowRect(
                    out WindowRect rect))
        {
            return;
        }


        if (!_windowService
                .TryGetCurrentMonitorInfo(
                    out WindowMonitorInfo monitor))
        {
            return;
        }


        int windowWidth =
            rect.Width;


        int windowHeight =
            rect.Height;


        int snapDistancePixels =
            LogicalToPhysical(
                snapDistance,
                monitor.DpiScale
            );


        int newX =
            rect.Left;


        int newY =
            rect.Top;


        SnapEdge newEdge =
            SnapEdge.None;


        // ==================================================
        // Horizontal
        // ==================================================

        bool nearLeft =
            rect.Left <=
            monitor.WorkArea.Left +
            snapDistancePixels;


        bool nearRight =
            rect.Right >=
            monitor.WorkArea.Right -
            snapDistancePixels;


        if (nearLeft)
        {
            newX =
                monitor.WorkArea.Left;


            newEdge |=
                SnapEdge.Left;
        }
        else if (nearRight)
        {
            newX =
                monitor.WorkArea.Right -
                windowWidth;


            newEdge |=
                SnapEdge.Right;
        }


        // ==================================================
        // Vertical
        // ==================================================

        bool nearTop =
            rect.Top <=
            monitor.WorkArea.Top +
            snapDistancePixels;


        bool nearBottom =
            rect.Bottom >=
            monitor.WorkArea.Bottom -
            snapDistancePixels;


        if (nearTop)
        {
            newY =
                monitor.WorkArea.Top;


            newEdge |=
                SnapEdge.Top;
        }
        else if (nearBottom)
        {
            newY =
                monitor.WorkArea.Bottom -
                windowHeight;


            newEdge |=
                SnapEdge.Bottom;
        }


        // ==================================================
        // Safety Clamp
        // ==================================================

        int maximumX =
            monitor.WorkArea.Right -
            windowWidth;


        int maximumY =
            monitor.WorkArea.Bottom -
            windowHeight;


        newX =
            Mathf.Clamp(
                newX,
                monitor.WorkArea.Left,
                maximumX
            );


        newY =
            Mathf.Clamp(
                newY,
                monitor.WorkArea.Top,
                maximumY
            );


        // ==================================================
        // Apply
        // ==================================================

        bool positionChanged =
            newX != rect.Left ||
            newY != rect.Top;


        if (positionChanged)
        {
            bool success =
                _windowService
                    .SetPhysicalBounds(
                        newX,
                        newY,
                        windowWidth,
                        windowHeight
                    );


            if (!success)
            {
                return;
            }
        }


        // ==================================================
        // State
        // ==================================================

        _currentEdge =
            newEdge;


        _isHidden =
            false;


        if (_currentEdge !=
            SnapEdge.None)
        {
            CacheSnapState(
                newX,
                newY,
                windowWidth,
                windowHeight,
                monitor
            );


            Debug.Log(
                "[WindowSnapController] " +
                $"Snapped: {_currentEdge}, " +
                $"Position=({newX},{newY}), " +
                $"DPI={monitor.Dpi}"
            );


            OnSnapped(
                _currentEdge
            );
        }
    }


    // ======================================================
    // Cache
    // ======================================================

    private void CacheSnapState(
        int x,
        int y,
        int width,
        int height,
        WindowMonitorInfo monitor)
    {
        _cachedWindowWidth =
            width;


        _cachedWindowHeight =
            height;


        _cachedSnapY =
            y;


        _cachedMonitor =
            monitor;
    }


    // ======================================================
    // Auto Hide
    // ======================================================

    private void HandleAutoHide()
    {
        if (_currentEdge ==
            SnapEdge.None)
        {
            return;
        }


        bool snappedLeft =
            HasEdge(
                SnapEdge.Left
            );


        bool snappedRight =
            HasEdge(
                SnapEdge.Right
            );


        // 和原版本一样：
        // Top / Bottom 本身不做自动隐藏。
        if (!snappedLeft &&
            !snappedRight)
        {
            return;
        }


        if (!_windowService
                .TryGetCursorPosition(
                    out WindowPoint cursor))
        {
            return;
        }


        int revealPixels =
            LogicalToPhysical(
                hideOffset,
                _cachedMonitor.DpiScale
            );


        if (snappedRight &&
            CanAutoHideRight())
        {
            HandleRightAutoHide(
                cursor,
                revealPixels
            );


            return;
        }


        if (snappedLeft &&
            CanAutoHideLeft())
        {
            HandleLeftAutoHide(
                cursor,
                revealPixels
            );
        }
    }


    // ======================================================
    // Right Auto Hide
    // ======================================================

    private void HandleRightAutoHide(
        WindowPoint cursor,
        int revealPixels)
    {
        int revealBoundary =
            _cachedMonitor
                .Bounds.Right -
            revealPixels;


        if (!_isHidden &&
            cursor.X <
            revealBoundary)
        {
            HideRight(
                revealPixels
            );


            return;
        }


        if (_isHidden &&
            cursor.X >=
            revealBoundary)
        {
            ShowRight();
        }
    }


    private void HideRight(
        int revealPixels)
    {
        int newX =
            _cachedMonitor
                .Bounds.Right -
            revealPixels;


        if (_windowService
                .SetPhysicalBounds(
                    newX,
                    _cachedSnapY,
                    _cachedWindowWidth,
                    _cachedWindowHeight))
        {
            _isHidden =
                true;


            OnHidden(
                SnapEdge.Right
            );
        }
    }


    private void ShowRight()
    {
        int newX =
            _cachedMonitor
                .WorkArea.Right -
            _cachedWindowWidth;


        if (_windowService
                .SetPhysicalBounds(
                    newX,
                    _cachedSnapY,
                    _cachedWindowWidth,
                    _cachedWindowHeight))
        {
            _isHidden =
                false;


            OnShown(
                SnapEdge.Right
            );
        }
    }


    // ======================================================
    // Left Auto Hide
    // ======================================================

    private void HandleLeftAutoHide(
        WindowPoint cursor,
        int revealPixels)
    {
        int revealBoundary =
            _cachedMonitor
                .Bounds.Left +
            revealPixels;


        if (!_isHidden &&
            cursor.X >
            revealBoundary)
        {
            HideLeft(
                revealPixels
            );


            return;
        }


        if (_isHidden &&
            cursor.X <=
            revealBoundary)
        {
            ShowLeft();
        }
    }


    private void HideLeft(
        int revealPixels)
    {
        int newX =
            _cachedMonitor
                .Bounds.Left -
            _cachedWindowWidth +
            revealPixels;


        if (_windowService
                .SetPhysicalBounds(
                    newX,
                    _cachedSnapY,
                    _cachedWindowWidth,
                    _cachedWindowHeight))
        {
            _isHidden =
                true;


            OnHidden(
                SnapEdge.Left
            );
        }
    }


    private void ShowLeft()
    {
        int newX =
            _cachedMonitor
                .WorkArea.Left;


        if (_windowService
                .SetPhysicalBounds(
                    newX,
                    _cachedSnapY,
                    _cachedWindowWidth,
                    _cachedWindowHeight))
        {
            _isHidden =
                false;


            OnShown(
                SnapEdge.Left
            );
        }
    }


    // ======================================================
    // External Monitor Edge Detection
    // ======================================================

    private bool CanAutoHideLeft()
    {
        // 如果任务栏正好位于左侧，
        // WorkArea 左边缘并不是物理屏幕边缘，
        // 不进行隐藏。
        if (_cachedMonitor.WorkArea.Left !=
            _cachedMonitor.Bounds.Left)
        {
            return false;
        }


        int testX =
            _cachedMonitor
                .Bounds.Left -
            1;


        int testY =
            GetMonitorVerticalCenter();


        // 左侧 1px 已经属于其他显示器：
        // 说明这是显示器之间的内部边缘。
        return
            !_windowService
                .IsPointOnAnyMonitor(
                    testX,
                    testY
                );
    }


    private bool CanAutoHideRight()
    {
        if (_cachedMonitor.WorkArea.Right !=
            _cachedMonitor.Bounds.Right)
        {
            return false;
        }


        // RECT.Right 是 exclusive edge。
        // 所以 Right 本身就是边界外第一列坐标。
        int testX =
            _cachedMonitor
                .Bounds.Right;


        int testY =
            GetMonitorVerticalCenter();


        return
            !_windowService
                .IsPointOnAnyMonitor(
                    testX,
                    testY
                );
    }


    private int GetMonitorVerticalCenter()
    {
        return
            _cachedMonitor.Bounds.Top +
            _cachedMonitor.Bounds.Height /
            2;
    }


    // ======================================================
    // Helpers
    // ======================================================

    private bool HasEdge(
        SnapEdge edge)
    {
        return
            (_currentEdge & edge)
            != 0;
    }


    private static int LogicalToPhysical(
        int logicalPixels,
        float dpiScale)
    {
        return
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    logicalPixels *
                    dpiScale
                )
            );
    }


    // ======================================================
    // Character / Presentation Extension Points
    // ======================================================

    private void OnSnapped(
        SnapEdge edge)
    {
        // 这里属于 Character / Presentation。
        //
        // 将来：
        //
        // petAnimator.SetTrigger("OnSnap");
        //
        // 或：
        //
        // petStateManager.EnterSnapState(...);
    }


    private void OnHidden(
        SnapEdge edge)
    {
        // 将来可以：
        //
        // petAnimator.SetBool("isHidden", true);
    }


    private void OnShown(
        SnapEdge edge)
    {
        // 将来可以：
        //
        // petAnimator.SetBool("isHidden", false);
    }
}