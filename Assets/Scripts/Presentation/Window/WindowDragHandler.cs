using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;

using Platform.Windows;


public sealed class WindowDragHandler
    : MonoBehaviour,
      IPointerDownHandler,
      IPointerUpHandler,
      IDragHandler
{
    // ======================================================
    // Configuration
    // ======================================================

    [Header("Drag")]

    [Tooltip(
        "鼠标移动超过该 UI 距离后，" +
        "才认为用户是在拖动窗口而不是点击。")]
    [SerializeField]
    private float dragThreshold =
        10f;

    [Header("Snap")]

    [SerializeField]
    private WindowSnapController
        snapController;
    
    // ======================================================
    // Runtime State
    // ======================================================

    private Vector2
        _startMousePosition;


    private bool
        _windowWasDragged;


    private CustomButtonClicker
        _customButton;


    private IWindowService
        _windowService;


    // ======================================================
    // Unity Lifecycle
    // ======================================================

    private void Awake()
    {
        _customButton =
            GetComponent<
                CustomButtonClicker
            >();
        
    }


    private void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        _windowService =
            WindowsPlatformBootstrap
                .WindowService;


        if (_windowService == null)
        {
            Debug.LogError(
                "[WindowDragHandler] " +
                "WindowService is not available."
            );


            return;
        }


        if (!_windowService
                .IsInitialized)
        {
            Debug.LogError(
                "[WindowDragHandler] " +
                "WindowService is not initialized."
            );
        }

#endif
    }


    // ======================================================
    // Pointer Down
    // ======================================================

    public void OnPointerDown(
        PointerEventData eventData)
    {
        _startMousePosition =
            eventData.position;


        _windowWasDragged =
            false;


        if (_customButton != null)
        {
            _customButton
                .OnPointerDownVisual();
        }
    }


    // ======================================================
    // Drag
    // ======================================================

    public void OnDrag(
        PointerEventData eventData)
    {
        if (_windowWasDragged)
        {
            return;
        }


        float distance =
            Vector2.Distance(
                _startMousePosition,
                eventData.position
            );


        if (distance <=
            dragThreshold)
        {
            return;
        }


        _windowWasDragged =
            true;


        // ==================================================
        // Restore Unity visual state before native move
        // ==================================================

        //
        // Windows 接管移动后，
        // Unity 主线程会暂时进入系统移动流程。
        //
        // 因此必须先恢复按钮视觉状态。
        //

        if (_customButton != null)
        {
            _customButton
                .OnPointerUpVisual();
        }


        // ==================================================
        // Release EventSystem drag ownership
        // ==================================================

        //
        // 与旧实现保持一致：
        //
        // 防止 Windows 接管拖动后，
        // Unity EventSystem 还认为这个对象
        // 正处于 drag 状态。
        //

        eventData.pointerDrag =
            null;


        eventData.dragging =
            false;


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        if (_windowService == null ||
            !_windowService.IsInitialized)
        {
            Debug.LogError(
                "[WindowDragHandler] " +
                "Cannot begin window drag: " +
                "WindowService unavailable."
            );


            return;
        }


        // ==================================================
        // Native system drag
        // ==================================================

        bool success =
            _windowService
                .BeginWindowDrag();


        if (!success)
        {
            Debug.LogError(
                "[WindowDragHandler] " +
                "Native window drag failed."
            );


            StartCoroutine(
                ResetInputNextFrame()
            );


            return;
        }


        // ==================================================
        // Character / Business Event
        // ==================================================

        InteractionEventService
            .RecordPetDragged();

        // ==================================================
        // Snap
        // ==================================================
        if (snapController != null)
        {
            snapController
                .HandleWindowDragFinished();
        }

        // ==================================================
        // Recover Unity Input
        // ==================================================

        StartCoroutine(
            ResetInputNextFrame()
        );

#endif
    }


    // ======================================================
    // Pointer Up
    // ======================================================

    public void OnPointerUp(
        PointerEventData eventData)
    {
        // 如果整个操作没有进入窗口拖动，
        // 就仍然把它当成普通点击。
        if (!_windowWasDragged)
        {
            if (_customButton != null)
            {
                _customButton
                    .OnPointerUpVisual();


                _customButton
                    .PerformClick();
            }
        }
    }


    // ======================================================
    // Input Recovery
    // ======================================================

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    private IEnumerator
        ResetInputNextFrame()
    {
        // 先允许 Unity Player
        // 恢复一次自己的 message pump。
        yield return null;


        Input.ResetInputAxes();


        EventSystem eventSystem =
            EventSystem.current;


        if (eventSystem == null)
        {
            yield break;
        }


        eventSystem.enabled =
            false;


        // 保持一帧关闭，
        // 清理之前的 Pointer / Drag 状态。
        yield return null;


        eventSystem.enabled =
            true;
    }

#endif
}