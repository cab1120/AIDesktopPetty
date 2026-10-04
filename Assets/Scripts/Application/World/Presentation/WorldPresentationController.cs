using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Platform.Windows;
using Platform.Windows.Models;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// Desktop 与 World 之间的表现控制权切换器。
    ///
    /// 它不负责：
    /// - Scene Load / Unload
    /// - World State Machine
    /// - 数据库
    /// - Conversation
    ///
    /// 它只负责：
    /// Capture / Suspend / Activate / Restore。
    /// </summary>
    public sealed class WorldPresentationController
        : MonoBehaviour
    {
        [Header("Desktop Layout")]

        [SerializeField]
        private DesktopPetLayoutController
            desktopLayoutController;


        [Header("Desktop Camera / Audio")]

        [SerializeField]
        private Camera
            desktopCamera;

        [SerializeField]
        private AudioListener
            desktopAudioListener;


        [Header("Desktop Window Controllers")]

        [SerializeField]
        private ClickThroughController
            clickThroughController;

        [SerializeField]
        private WindowSnapController
            windowSnapController;


        [Header("Desktop Background AI")]

        [SerializeField]
        private AIContextReactionManager
            aiContextReactionManager;

        [SerializeField]
        private BubbleUIManager
            bubbleUIManager;


        [Header("Desktop World-Exclusive Behaviours")]

        [Tooltip(
            "进入 Explore 后必须暂停的桌面表现组件。\n" +
            "建议放 Desktop Canvas、GraphicRaycaster、" +
            "桌面专用输入/拖拽组件。\n" +
            "不要放 ConversationService、AIChat、" +
            "WorldCoordinator。")]
        [SerializeField]
        private Behaviour[]
            desktopWorldExclusiveBehaviours =
                new Behaviour[0];


        [Header("World Presentation")]

        [SerializeField]
        private WorldPresentationProfile
            worldProfile;


        // =====================================================
        // Capture
        // =====================================================

        public bool TryCaptureDesktop(
            out DesktopPresentationSnapshot snapshot,
            out string error)
        {
            snapshot = null;

            if (!TryValidateConfiguration(
                    out error))
            {
                return false;
            }


            bool hasWindowRect =
                false;

            WindowRect windowRect =
                default;


            bool hasMonitorInfo =
                false;

            WindowMonitorInfo monitorInfo =
                default;


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            IWindowService windowService =
                WindowsPlatformBootstrap
                    .WindowService;

            if (windowService == null)
            {
                error =
                    "WindowService 不可用。";

                return false;
            }

            if (!windowService.IsInitialized)
            {
                error =
                    "WindowService 尚未初始化。";

                return false;
            }

            if (!windowService.TryGetWindowRect(
                    out windowRect))
            {
                error =
                    "无法读取进入 World 前的窗口位置。";

                return false;
            }

            hasWindowRect =
                true;


            if (!windowService
                    .TryGetCurrentMonitorInfo(
                        out monitorInfo))
            {
                error =
                    "无法读取进入 World 前所在的显示器。";

                return false;
            }

            hasMonitorInfo =
                true;

#endif


            BehaviourState[] behaviourStates =
                CaptureBehaviourStates(
                    desktopWorldExclusiveBehaviours);


            snapshot =
                new DesktopPresentationSnapshot(
                    desktopLayoutController.CurrentMode,
                    hasWindowRect,
                    windowRect,
                    hasMonitorInfo,
                    monitorInfo,
                    desktopCamera.enabled,
                    desktopAudioListener.enabled,
                    clickThroughController.enabled,
                    windowSnapController.enabled,
                    aiContextReactionManager.enabled,
                    behaviourStates
                );


            error = null;

            return true;
        }


        // =====================================================
        // Entering: suspend desktop background behaviour
        // =====================================================

        public bool TrySuspendDesktopForEntering(
            WorldScope scope,
            out string error)
        {
            if (scope == null)
            {
                error =
                    "WorldScope 不能为空。";

                return false;
            }

            if (scope.DesktopSnapshot == null)
            {
                error =
                    "WorldScope 没有 DesktopSnapshot。";

                return false;
            }


            /*
             * 1. 先阻止新主动气泡产生。
             *
             * AIContextReactionManager.OnDisable：
             * - unsubscribe
             * - CancelBubbleRequests
             * - StopAllCoroutines
             * - requestInFlight=false
             */
            aiContextReactionManager.enabled =
                false;


            /*
             * 2. 已显示出来的旧 Bubble
             *    是 transient UI，不恢复。
             */
            bubbleUIManager.HideImmediately();


            /*
             * 3. 暂停桌面窗口自动吸附/隐藏。
             *
             * 否则 World 调整窗口期间，
             * SnapController 仍可能移动窗口。
             */
            windowSnapController.enabled =
                false;


            /*
             * 4. 暂停桌面的动态穿透判断。
             *
             * ClickThroughController.OnDisable
             * 自己也会尽量恢复 false。
             */
            clickThroughController.enabled =
                false;


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            IWindowService windowService =
                WindowsPlatformBootstrap
                    .WindowService;

            if (windowService == null ||
                !windowService.IsInitialized)
            {
                error =
                    "WindowService 在 Entering 中不可用。";

                return false;
            }

            /*
             * 再显式写一次 false。
             *
             * 即使 ClickThroughController
             * 原本就已经 disabled，
             * World 也必须确保窗口现在可以接收输入。
             */
            if (!windowService.SetClickThrough(false))
            {
                error =
                    "无法关闭窗口 ClickThrough。";

                return false;
            }

#endif


            error = null;

            return true;
        }


        // =====================================================
        // World Binding
        // =====================================================

        public bool TryBindLoadedWorld(
            WorldScope scope,
            out string error)
        {
            if (scope == null ||
                scope.SceneHandle == null)
            {
                error =
                    "World 没有有效的 SceneHandle。";

                return false;
            }

            if (!WorldRuntimeBindingsResolver.TryResolve(
                    scope.SceneHandle.Scene,
                    out WorldRuntimeBindings bindings,
                    out error))
            {
                return false;
            }

            scope.AttachRuntimeBindings(
                bindings);

            return true;
        }


        // =====================================================
        // Commit World Presentation
        // =====================================================

        public bool TryActivateWorld(
            WorldScope scope,
            out string error)
        {
            if (scope == null)
            {
                error =
                    "WorldScope 不能为空。";

                return false;
            }


            if (scope.RuntimeBindings == null)
            {
                error =
                    "WorldRuntimeBindings 尚未绑定。";

                return false;
            }


            if (scope.SceneHandle == null)
            {
                error =
                    "World 没有有效 SceneHandle。";

                return false;
            }


            // =====================================================
            // 1. Apply World Window
            // =====================================================

            /*
             * 窗口先调整。
             *
             * 此时仍然没有把 Camera / Active Scene
             * 控制权交给 World。
             */
            if (!TryApplyWorldWindow(
                    scope.DesktopSnapshot,
                    out error))
            {
                return false;
            }


            // =====================================================
            // 2. Capture Previous Active Scene
            // =====================================================

            Scene previousActiveScene =
                SceneManager.GetActiveScene();


            if (!scope.HasPreviousActiveScene)
            {
                scope.CapturePreviousActiveScene(
                    previousActiveScene);
            }


            // =====================================================
            // 3. Make World Scene Active
            // =====================================================

            Scene worldScene =
                scope.SceneHandle.Scene;


            if (!worldScene.IsValid() ||
                !worldScene.isLoaded)
            {
                error =
                    "World Scene 无效或尚未加载。";

                return false;
            }


            if (!SceneManager.SetActiveScene(
                    worldScene))
            {
                error =
                    $"无法把 World Scene 设为 Active：" +
                    $"{worldScene.name}";

                return false;
            }


            Debug.Log(
                $"[WorldPresentation] " +
                $"ActiveScene: " +
                $"{previousActiveScene.name} -> " +
                $"{worldScene.name}");


            // =====================================================
            // 4. Activate World-specific Content
            // =====================================================

            /*
             * 到这一刻：
             *
             * RenderSettings / Lighting Settings
             * 才应该来自 3DScene。
             *
             * 所以 Skybox 必须在这里初始化，
             * 而不是 Additive Scene 的 Start() 中抢先初始化。
             */
            if (!scope.RuntimeBindings
                    .TryActivateContent(
                        out string contentError))
            {
                /*
                 * Content 可能已经做了一部分修改，
                 * 先让它自行补偿。
                 */
                scope.RuntimeBindings
                    .DeactivateContent();


                /*
                 * Active Scene 也恢复。
                 */
                RestorePreviousActiveScene(
                    scope);


                error =
                    $"World Content 激活失败：" +
                    $"{contentError}";

                return false;
            }


            // =====================================================
            // 5. Desktop releases presentation ownership
            // =====================================================

            SetDesktopExclusiveBehaviours(
                false);


            /*
             * AudioListener 先关闭 Desktop，
             * 避免出现两个有效 Listener。
             */
            desktopAudioListener.enabled =
                false;


            desktopCamera.enabled =
                false;


            // =====================================================
            // 6. World receives control
            // =====================================================

            scope.RuntimeBindings
                .SetControlEnabled(true);


            error = null;

            return true;
        }


        // =====================================================
        // Exit
        // =====================================================

        public void PrepareWorldForExit(
            WorldScope scope)
        {
            if (scope == null)
            {
                return;
            }


            // =====================================================
            // 1. World loses Camera / Audio / Input ownership
            // =====================================================

            if (scope.RuntimeBindings != null)
            {
                scope.RuntimeBindings
                    .SetControlEnabled(false);


                // =================================================
                // 2. World-specific content releases ownership
                // =================================================

                /*
                 * SkyboxRotator 会在这里销毁自己的
                 * Runtime Material。
                 */
                scope.RuntimeBindings
                    .DeactivateContent();
            }


            // =====================================================
            // 3. Restore previous Active Scene
            // =====================================================

            /*
             * 一定要发生在 Unload 3DScene 之前。
             */
            if (!RestorePreviousActiveScene(
                    scope))
            {
                Debug.LogError(
                    $"[WorldPresentation] " +
                    $"无法恢复 Previous Active Scene：" +
                    $"{scope}");
            }
        }
        
        private bool RestorePreviousActiveScene(
            WorldScope scope)
        {
            if (scope == null ||
                !scope.HasPreviousActiveScene)
            {
                return true;
            }


            Scene previous =
                scope.PreviousActiveScene;


            if (!previous.IsValid() ||
                !previous.isLoaded)
            {
                Debug.LogError(
                    "[WorldPresentation] " +
                    "Previous Active Scene 已经失效。");

                return false;
            }


            Scene current =
                SceneManager.GetActiveScene();


            /*
             * 已经恢复过时保持幂等。
             */
            if (current == previous)
            {
                return true;
            }


            if (!SceneManager.SetActiveScene(
                    previous))
            {
                Debug.LogError(
                    "[WorldPresentation] " +
                    $"无法 SetActiveScene：" +
                    $"{previous.name}");

                return false;
            }


            Debug.Log(
                $"[WorldPresentation] " +
                $"ActiveScene: " +
                $"{current.name} -> " +
                $"{previous.name}");


            return true;
        }


        // =====================================================
        // Restore
        // =====================================================

        public bool TryRestoreDesktop(
            WorldScope scope,
            out string error)
        {
            if (scope == null ||
                scope.DesktopSnapshot == null)
            {
                error =
                    "没有可恢复的 DesktopSnapshot。";

                return false;
            }

            DesktopPresentationSnapshot snapshot =
                scope.DesktopSnapshot;


            List<string> errors =
                new List<string>();


            /*
             * 防御性保证：
             * 只要 World Binding 还存在，
             * 先让它失去控制权。
             */
            if (scope.RuntimeBindings != null)
            {
                scope.RuntimeBindings
                    .SetControlEnabled(false);
            }


            // ---------------------------------------------
            // 1. Restore coarse desktop layout
            // ---------------------------------------------

            bool layoutSuccess =
                desktopLayoutController
                    .ApplyLayout(
                        snapshot.LayoutMode);


#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            if (!layoutSuccess)
            {
                errors.Add(
                    $"恢复桌面 Layout 失败：" +
                    $"{snapshot.LayoutMode}");
            }

#endif


            // ---------------------------------------------
            // 2. Restore exact physical window state
            // ---------------------------------------------

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            IWindowService windowService =
                WindowsPlatformBootstrap
                    .WindowService;

            if (windowService == null ||
                !windowService.IsInitialized)
            {
                errors.Add(
                    "Restore 时 WindowService 不可用。");
            }
            else
            {
                /*
                 * Profile 是 coarse state。
                 * WindowRect 是 exact runtime state。
                 *
                 * 所以必须：
                 *
                 * ApplyLayout
                 * ↓
                 * SetPhysicalBounds
                 */
                if (snapshot.HasWindowRect)
                {
                    WindowRect rect =
                        snapshot.WindowRect;

                    if (!windowService
                            .SetPhysicalBounds(
                                rect.Left,
                                rect.Top,
                                rect.Width,
                                rect.Height))
                    {
                        errors.Add(
                            "无法恢复原桌面物理窗口位置。");
                    }
                }


                /*
                 * 先回到一个确定的 non-click-through
                 * 状态，再把 owner 交还
                 * ClickThroughController。
                 */
                if (!windowService
                        .SetClickThrough(false))
                {
                    errors.Add(
                        "无法恢复 ClickThrough 基线。");
                }
            }

#endif


            // ---------------------------------------------
            // 3. Restore Desktop visual/input behaviours
            // ---------------------------------------------

            RestoreBehaviourStates(
                snapshot.DesktopBehaviourStates);


            // ---------------------------------------------
            // 4. Restore camera / audio
            // ---------------------------------------------

            desktopCamera.enabled =
                snapshot.DesktopCameraEnabled;

            desktopAudioListener.enabled =
                snapshot.DesktopAudioListenerEnabled;


            // ---------------------------------------------
            // 5. Restore window behaviour owners
            // ---------------------------------------------

            windowSnapController.enabled =
                snapshot.WindowSnapControllerEnabled;

            clickThroughController.enabled =
                snapshot.ClickThroughControllerEnabled;


            /*
             * AI reaction 最后恢复。
             *
             * 这样不会在窗口/UI 还没稳定的时候
             * 马上触发一个主动气泡。
             */
            aiContextReactionManager.enabled =
                snapshot.AIReactionManagerEnabled;


            if (errors.Count > 0)
            {
                error =
                    string.Join(
                        "\n",
                        errors);

                return false;
            }


            error = null;

            return true;
        }


        // =====================================================
        // Window
        // =====================================================

        private bool TryApplyWorldWindow(
            DesktopPresentationSnapshot snapshot,
            out string error)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            IWindowService windowService =
                WindowsPlatformBootstrap
                    .WindowService;

            if (windowService == null ||
                !windowService.IsInitialized)
            {
                error =
                    "WindowService 不可用。";

                return false;
            }


            WindowLogicalSize worldSize =
                new WindowLogicalSize(
                    worldProfile.WindowWidth,
                    worldProfile.WindowHeight);


            if (!windowService.SetLogicalSize(
                    worldSize))
            {
                error =
                    $"无法切换 World 窗口尺寸：" +
                    $"{worldProfile.WindowWidth}x" +
                    $"{worldProfile.WindowHeight}";

                return false;
            }


            /*
             * World 窗口和桌宠窗口是两个不同的
             * presentation policy。
             *
             * 桌宠可能处于边缘隐藏位置，
             * 所以不能沿用那个物理 Rect。
             */
            if (worldProfile.CenterOnCurrentMonitor &&
                snapshot != null &&
                snapshot.HasMonitorInfo)
            {
                if (!windowService.TryGetWindowRect(
                        out WindowRect resizedRect))
                {
                    error =
                        "World Resize 后无法读取窗口 Rect。";

                    return false;
                }


                WindowRect workArea =
                    snapshot.MonitorInfo.WorkArea;


                int workWidth =
                    workArea.Right -
                    workArea.Left;

                int workHeight =
                    workArea.Bottom -
                    workArea.Top;


                int x =
                    workArea.Left +
                    (workWidth -
                     resizedRect.Width) / 2;

                int y =
                    workArea.Top +
                    (workHeight -
                     resizedRect.Height) / 2;


                if (!windowService.SetPhysicalBounds(
                        x,
                        y,
                        resizedRect.Width,
                        resizedRect.Height))
                {
                    error =
                        "无法把 World 窗口居中到当前显示器。";

                    return false;
                }
            }

#endif


            error = null;

            return true;
        }


        // =====================================================
        // Validation
        // =====================================================

        private bool TryValidateConfiguration(
            out string error)
        {
            if (desktopLayoutController == null)
            {
                error =
                    "DesktopPetLayoutController 未绑定。";

                return false;
            }

            if (desktopCamera == null)
            {
                error =
                    "Desktop Camera 未绑定。";

                return false;
            }

            if (desktopAudioListener == null)
            {
                error =
                    "Desktop AudioListener 未绑定。";

                return false;
            }

            if (clickThroughController == null)
            {
                error =
                    "ClickThroughController 未绑定。";

                return false;
            }

            if (windowSnapController == null)
            {
                error =
                    "WindowSnapController 未绑定。";

                return false;
            }

            if (aiContextReactionManager == null)
            {
                error =
                    "AIContextReactionManager 未绑定。";

                return false;
            }

            if (bubbleUIManager == null)
            {
                error =
                    "BubbleUIManager 未绑定。";

                return false;
            }

            if (worldProfile == null)
            {
                error =
                    "WorldPresentationProfile 未绑定。";

                return false;
            }


            for (int i = 0;
                 i < desktopWorldExclusiveBehaviours.Length;
                 i++)
            {
                if (desktopWorldExclusiveBehaviours[i]
                    == null)
                {
                    error =
                        $"Desktop World Exclusive " +
                        $"Behaviours 第 {i} 项为空。";

                    return false;
                }
            }


            error = null;

            return true;
        }


        // =====================================================
        // Behaviour snapshots
        // =====================================================

        private static BehaviourState[]
            CaptureBehaviourStates(
                Behaviour[] behaviours)
        {
            if (behaviours == null ||
                behaviours.Length == 0)
            {
                return new BehaviourState[0];
            }


            BehaviourState[] states =
                new BehaviourState[
                    behaviours.Length];


            for (int i = 0;
                 i < behaviours.Length;
                 i++)
            {
                Behaviour behaviour =
                    behaviours[i];

                states[i] =
                    new BehaviourState(
                        behaviour,
                        behaviour != null &&
                        behaviour.enabled);
            }


            return states;
        }


        private void
            SetDesktopExclusiveBehaviours(
                bool enabled)
        {
            for (int i = 0;
                 i <
                 desktopWorldExclusiveBehaviours.Length;
                 i++)
            {
                Behaviour behaviour =
                    desktopWorldExclusiveBehaviours[i];

                if (behaviour != null)
                {
                    behaviour.enabled =
                        enabled;
                }
            }
        }


        private static void RestoreBehaviourStates(
            BehaviourState[] states)
        {
            if (states == null)
                return;


            for (int i = 0;
                 i < states.Length;
                 i++)
            {
                Behaviour target =
                    states[i].Target;

                if (target != null)
                {
                    target.enabled =
                        states[i].WasEnabled;
                }
            }
        }
    }
}