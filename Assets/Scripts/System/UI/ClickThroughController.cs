using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Platform.Windows;


public sealed class ClickThroughController
    : MonoBehaviour
{
    // ======================================================
    // Unity UI Detection
    // ======================================================

    [Header("UI Detection")]

    [Tooltip(
        "用于判断鼠标是否位于可交互 Unity UI 上。")]
    [SerializeField]
    private GraphicRaycaster raycaster;


    // ======================================================
    // Runtime State
    // ======================================================

    private PointerEventData
        _pointerData;


    private readonly List<RaycastResult>
        _raycastResults =
            new List<RaycastResult>();


    private IWindowService
        _windowService;


    private bool
        _isClickThrough;


    private bool
        _hasAppliedInitialState;


    // ======================================================
    // Unity Lifecycle
    // ======================================================

    private void Start()
    {
        EventSystem eventSystem =
            EventSystem.current;


        if (eventSystem == null)
        {
            Debug.LogError(
                "[ClickThroughController] " +
                "No EventSystem found."
            );


            enabled =
                false;


            return;
        }


        _pointerData =
            new PointerEventData(
                eventSystem
            );


        if (raycaster == null)
        {
            raycaster =
                GetComponent<
                    GraphicRaycaster
                >();
        }


        if (raycaster == null)
        {
            Debug.LogError(
                "[ClickThroughController] " +
                "GraphicRaycaster is missing."
            );


            enabled =
                false;


            return;
        }


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        _windowService =
            WindowsPlatformBootstrap
                .WindowService;


        if (_windowService == null)
        {
            Debug.LogError(
                "[ClickThroughController] " +
                "WindowService is not available."
            );


            enabled =
                false;


            return;
        }


        if (!_windowService
                .IsInitialized)
        {
            Debug.LogError(
                "[ClickThroughController] " +
                "WindowService is not initialized."
            );


            enabled =
                false;


            return;
        }

#endif
    }


    private void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        bool isOverUI =
            IsMouseOverInteractiveUI();


        bool shouldClickThrough =
            !isOverUI;


        // 第一次运行必须主动同步一次，
        // 后面只有状态变化才调用 Native。
        if (!_hasAppliedInitialState ||
            shouldClickThrough !=
            _isClickThrough)
        {
            ApplyClickThrough(
                shouldClickThrough
            );
        }

#endif
    }


    // ======================================================
    // UI Detection
    // ======================================================

    private bool
        IsMouseOverInteractiveUI()
    {
        if (raycaster == null ||
            _pointerData == null)
        {
            return false;
        }


        _pointerData.position =
            Input.mousePosition;


        _raycastResults.Clear();


        raycaster.Raycast(
            _pointerData,
            _raycastResults
        );


        return
            _raycastResults.Count > 0;
    }


    // ======================================================
    // Platform State
    // ======================================================

    private void ApplyClickThrough(
        bool enabledState)
    {
        if (_windowService == null)
        {
            return;
        }


        bool success =
            _windowService
                .SetClickThrough(
                    enabledState
                );


        if (!success)
        {
            Debug.LogError(
                "[ClickThroughController] " +
                "Failed to set " +
                $"click-through={enabledState}."
            );


            return;
        }


        _isClickThrough =
            enabledState;


        _hasAppliedInitialState =
            true;
    }


    // ======================================================
    // Cleanup
    // ======================================================

    private void OnDisable()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        //
        // 如果这个组件被禁用，
        // 尽量恢复窗口可点击状态。
        //
        // 否则整个桌宠可能保持穿透，
        // 用户再也点不到它。
        //

        if (_windowService != null &&
            _windowService.IsInitialized &&
            _isClickThrough)
        {
            _windowService
                .SetClickThrough(
                    false
                );


            _isClickThrough =
                false;
        }

#endif
    }
}