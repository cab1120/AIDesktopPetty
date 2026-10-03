using System;
using System.Collections;
using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    public sealed class WorldCoordinator : MonoBehaviour
    {
        [Header("World Configuration")]

        [SerializeField]
        private WorldCatalog worldCatalog;

        [SerializeField]
        private WorldResourceServiceBehaviour resourceService;
        
        [SerializeField]
        private WorldPresentationController presentationController;

        private WorldState state =
            WorldState.Desktop;

        private WorldScope currentScope;

        private long nextWorldInstanceId;

        /// <summary>
        /// Exiting 时是否已经有一个真实清理协程在工作。
        ///
        /// 正常情况下重复 Exit 不会启动第二份清理。
        /// 如果一次 Release 明确失败，下一次 RequestExit
        /// 可以重新尝试。
        /// </summary>
        private bool exitRoutineRunning;

        public WorldState State => state;

        public WorldScope CurrentScope =>
            currentScope;

        public bool HasActiveWorld =>
            currentScope != null;

        public event Action<
            WorldState,
            WorldState> StateChanged;

        private void Awake()
        {
            state = WorldState.Desktop;
            currentScope = null;
            exitRoutineRunning = false;
        }

        public bool TryEnterWorld(
            string worldId,
            out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(worldId))
            {
                error = "WorldId 不能为空。";
                return false;
            }

            if (state != WorldState.Desktop)
            {
                error =
                    $"当前状态为 {state}，" +
                    $"不能再次进入世界。";

                return false;
            }

            if (currentScope != null)
            {
                error =
                    "当前仍存在 WorldScope，" +
                    "状态与生命周期不一致。";

                Debug.LogError(error);
                return false;
            }

            if (worldCatalog == null)
            {
                error =
                    "WorldCatalog 尚未绑定。";

                return false;
            }

            if (resourceService == null)
            {
                error =
                    "ResourceService 尚未绑定。";

                return false;
            }

            if (!worldCatalog.TryValidate(
                    out string catalogError))
            {
                error =
                    $"WorldCatalog 无效：" +
                    $"{catalogError}";

                return false;
            }

            if (!worldCatalog.TryGet(
                    worldId,
                    out WorldDefinition definition))
            {
                error =
                    $"WorldCatalog 中不存在 WorldId：" +
                    $"{worldId}";

                return false;
            }

            // ----------------------------
            // Validate Before Mutate
            // ----------------------------
            // 能在创建 WorldScope 之前发现的问题，
            // 尽量都先发现。
            if (!resourceService.CanLoadWorldScene(
                    definition,
                    out string resourceError))
            {
                error =
                    $"World 资源当前不可加载：" +
                    $"{resourceError}";

                return false;
            }
            
            if (presentationController == null)
            {
                error =
                    "WorldPresentationController 尚未绑定。";

                return false;
            }


            if (!presentationController
                    .TryCaptureDesktop(
                        out DesktopPresentationSnapshot
                            desktopSnapshot,
                        out string presentationError))
            {
                error =
                    $"无法捕获 Desktop Presentation：" +
                    $"{presentationError}";

                return false;
            }

            long instanceId =
                ++nextWorldInstanceId;

            WorldScope scope =
                new WorldScope(
                    instanceId,
                    definition);
            
            scope.AttachDesktopSnapshot(
                desktopSnapshot);
            
            currentScope = scope;

            if (!TryChangeState(
                    WorldState.Entering))
            {
                currentScope = null;

                error =
                    "无法切换到 Entering 状态。";

                return false;
            }

            Debug.Log(
                $"[World] Enter accepted: {scope}");

            StartCoroutine(
                EnterRoutine(scope));

            return true;
        }

        public bool RequestExit()
        {
            switch (state)
            {
                case WorldState.Desktop:
                    // 目标已经满足。
                    return true;

                case WorldState.Entering:
                    if (currentScope == null)
                    {
                        Debug.LogError(
                            "[World] Entering 状态没有 " +
                            "WorldScope。");

                        return false;
                    }

                    // ----------------------------
                    // Logical cancellation
                    // ----------------------------
                    // 不假装 LoadSceneAsync 已经停止。
                    // 这里只声明：
                    // “即使它成功，我也不再要这个世界。”
                    currentScope.RequestExit();

                    Debug.Log(
                        $"[World] Exit requested " +
                        $"while entering: " +
                        $"{currentScope}");

                    return true;

                case WorldState.Explore:
                    if (currentScope == null)
                    {
                        Debug.LogError(
                            "[World] Explore 状态没有 " +
                            "WorldScope。");

                        return false;
                    }

                    if (!TryChangeState(
                            WorldState.Exiting))
                    {
                        return false;
                    }

                    Debug.Log(
                        $"[World] Exit started: " +
                        $"{currentScope}");

                    StartExitRoutine(
                        currentScope);

                    return true;

                case WorldState.Exiting:
                    // 正在清理：
                    // 重复 Exit 不启动第二个协程。
                    //
                    // 如果之前清理明确失败，
                    // exitRoutineRunning 已经恢复 false，
                    // 此时允许重新尝试。
                    if (!exitRoutineRunning
                        && currentScope != null)
                    {
                        Debug.Log(
                            $"[World] Retry exit: " +
                            $"{currentScope}");

                        StartExitRoutine(
                            currentScope);
                    }

                    return true;

                default:
                    Debug.LogError(
                        $"[World] Unknown state: " +
                        $"{state}");

                    return false;
            }
        }

        private IEnumerator EnterRoutine(
            WorldScope scope)
        {
            WorldSceneLoadResult loadResult =
                null;

            bool callbackReceived =
                false;


            // =====================================================
            // Phase 1:
            // Suspend Desktop
            // =====================================================

            if (!presentationController
                    .TrySuspendDesktopForEntering(
                        scope,
                        out string suspendError))
            {
                FailEnterWithoutLoadedScene(
                    scope,
                    $"暂停 Desktop Presentation 失败：" +
                    $"{suspendError}");

                yield break;
            }


            // =====================================================
            // Phase 2:
            // Load World Scene
            // =====================================================

            yield return resourceService
                .LoadWorldScene(
                    scope.Definition,
                    result =>
                    {
                        if (callbackReceived)
                        {
                            Debug.LogError(
                                "[World] ResourceService " +
                                "对同一次 Load 调用了多次 completed。");

                            return;
                        }

                        callbackReceived =
                            true;

                        loadResult =
                            result;
                    });


            // =====================================================
            // Commit Gate 1:
            // 这还是不是当前 World？
            // =====================================================

            if (!IsCurrentScope(scope))
            {
                /*
                 * 当前 Scope 已经失效，
                 * 但 Scene 可能真的加载成功了。
                 *
                 * 不能因为业务结果过期就泄漏资源。
                 */
                if (loadResult != null
                    && loadResult.IsSuccess
                    && loadResult.Handle != null)
                {
                    yield return
                        ReleaseOrphanHandle(
                            loadResult.Handle);
                }

                yield break;
            }


            // =====================================================
            // Validate Resource Result
            // =====================================================

            if (!callbackReceived
                || loadResult == null)
            {
                FailEnterWithoutLoadedScene(
                    scope,
                    "ResourceService 的 Load 协程结束，" +
                    "但没有返回完成结果。");

                yield break;
            }


            if (!loadResult.IsSuccess)
            {
                FailEnterWithoutLoadedScene(
                    scope,
                    loadResult.Error
                    ?? "未知场景加载错误。");

                yield break;
            }


            if (loadResult.Handle == null)
            {
                FailEnterWithoutLoadedScene(
                    scope,
                    "ResourceService 返回 Success，" +
                    "但 Handle 为 null。");

                yield break;
            }


            // =====================================================
            // Ownership Transfer
            // =====================================================

            scope.AttachSceneHandle(
                loadResult.Handle);

            Debug.Log(
                $"[World] Scene handle attached: " +
                $"{scope}");


            // =====================================================
            // Commit Gate 2:
            // 用户是不是已经要求退出？
            // =====================================================

            if (scope.ExitRequested)
            {
                Debug.Log(
                    $"[World] Scene loaded, but " +
                    $"Exit was already requested: " +
                    $"{scope}");

                /*
                 * 非常重要：
                 *
                 * 这里不要 Bind。
                 * 不要 Activate。
                 * 不要进入 Explore。
                 *
                 * Scene 只是物理加载完成，
                 * 业务已经取消。
                 */
                if (!TryChangeState(
                        WorldState.Exiting))
                {
                    Debug.LogError(
                        "[World] 无法从 Entering " +
                        "切换到 Exiting。");

                    yield break;
                }

                StartExitRoutine(scope);

                yield break;
            }


            // =====================================================
            // Phase 3:
            // Resolve + Validate World Bindings
            // =====================================================

            if (!presentationController
                    .TryBindLoadedWorld(
                        scope,
                        out string bindingError))
            {
                Debug.LogError(
                    $"[World] Binding failed: " +
                    $"{bindingError}");

                if (TryChangeState(
                        WorldState.Exiting))
                {
                    StartExitRoutine(scope);
                }

                yield break;
            }


            // =====================================================
            // Phase 4:
            // Transfer Presentation Ownership
            // =====================================================

            if (!presentationController
                    .TryActivateWorld(
                        scope,
                        out string activationError))
            {
                Debug.LogError(
                    $"[World] World presentation " +
                    $"activation failed: " +
                    $"{activationError}");

                if (TryChangeState(
                        WorldState.Exiting))
                {
                    StartExitRoutine(scope);
                }

                yield break;
            }


            // =====================================================
            // Final Commit:
            // Publish Explore only after everything is ready
            // =====================================================

            if (!TryChangeState(
                    WorldState.Explore))
            {
                Debug.LogError(
                    $"[World] World 已经加载并激活，" +
                    $"但无法进入 Explore：" +
                    $"{scope}");

                /*
                 * Presentation 已经取得控制权，
                 * 因此失败时必须走完整补偿。
                 */
                if (state == WorldState.Entering
                    && TryChangeState(
                        WorldState.Exiting))
                {
                    StartExitRoutine(scope);
                }

                yield break;
            }


            Debug.Log(
                $"[World] Enter completed: " +
                $"{scope}");
        }

        private void StartExitRoutine(
            WorldScope scope)
        {
            if (exitRoutineRunning)
                return;

            exitRoutineRunning = true;

            StartCoroutine(
                ExitRoutineWrapper(scope));
        }

        private IEnumerator ExitRoutineWrapper(
            WorldScope scope)
        {
            try
            {
                yield return ExitRoutine(scope);
            }
            finally
            {
                exitRoutineRunning = false;
            }
        }

        private IEnumerator ExitRoutine(
            WorldScope scope)
        {
            if (!IsCurrentScope(scope))
                yield break;


            // =====================================================
            // Phase 1:
            // World immediately gives up presentation ownership
            // =====================================================

            presentationController
                .PrepareWorldForExit(scope);


            /*
             * 从这一刻开始，
             * Coordinator 不再认为 RuntimeBindings
             * 属于活动 World Presentation。
             *
             * Scene 对象本身仍然存在，
             * 直到 Release 完成。
             */
            scope.DetachRuntimeBindings();


            // =====================================================
            // Phase 2:
            // Release Scene Resource, if still owned
            // =====================================================

            WorldSceneHandle handle =
                scope.SceneHandle;


            if (handle != null)
            {
                WorldSceneReleaseResult
                    releaseResult = null;

                bool callbackReceived =
                    false;


                yield return resourceService
                    .ReleaseWorldScene(
                        handle,
                        result =>
                        {
                            if (callbackReceived)
                            {
                                Debug.LogError(
                                    "[World] ResourceService " +
                                    "对同一次 Release " +
                                    "调用了多次 completed。");

                                return;
                            }

                            callbackReceived =
                                true;

                            releaseResult =
                                result;
                        });


                if (!IsCurrentScope(scope))
                    yield break;


                if (!callbackReceived
                    || releaseResult == null)
                {
                    Debug.LogError(
                        $"[World] Release 协程结束，" +
                        $"但没有完成结果：" +
                        $"{scope}");

                    /*
                     * Handle 仍属于 Scope。
                     * 不宣称退出成功。
                     */
                    yield break;
                }


                if (!releaseResult.IsSuccess)
                {
                    Debug.LogError(
                        $"[World] Scene release failed: " +
                        $"{scope}; " +
                        $"{releaseResult.Error}");

                    /*
                     * Handle 仍留在 Scope。
                     * RequestExit 可以之后重试。
                     */
                    yield break;
                }


                scope.DetachSceneHandle();

                Debug.Log(
                    $"[World] Scene handle released: " +
                    $"{scope}");
            }


            // =====================================================
            // Phase 3:
            // Restore Desktop
            //
            // 注意：
            // 这一段必须放在 if(handle != null) 外面。
            //
            // 因为：
            // Scene 可能已经成功 Release，
            // 但上一次 Restore 失败。
            // Retry 时 Handle 已经是 null，
            // 仍然必须再次尝试 Restore。
            // =====================================================

            if (!IsCurrentScope(scope))
                yield break;


            if (!presentationController
                    .TryRestoreDesktop(
                        scope,
                        out string restoreError))
            {
                Debug.LogError(
                    $"[World] Desktop restore failed: " +
                    $"{restoreError}");

                /*
                 * 不 CompleteExit。
                 *
                 * State 继续保持 Exiting。
                 * 当前 Scope 继续存在。
                 *
                 * 再次 RequestExit 时：
                 * handle 即使已经 null，
                 * 也会重新走到这里尝试 Restore。
                 */
                yield break;
            }


            // =====================================================
            // Final Commit
            // =====================================================

            CompleteExit(scope);
        }

        private IEnumerator ReleaseOrphanHandle(
            WorldSceneHandle handle)
        {
            WorldSceneReleaseResult result =
                null;

            yield return resourceService
                .ReleaseWorldScene(
                    handle,
                    releaseResult =>
                    {
                        result = releaseResult;
                    });

            if (result == null
                || !result.IsSuccess)
            {
                Debug.LogError(
                    "[World] Failed to release " +
                    "orphaned scene handle: " +
                    $"{result?.Error}");
            }
        }

        /// <summary>
        /// 处理“还没有取得场景 Handle”的进入失败。
        ///
        /// 因为没有资源需要释放，
        /// 可以通过 Exiting 统一回到 Desktop。
        /// </summary>
        private void FailEnterWithoutLoadedScene(
            WorldScope scope,
            string error)
        {
            if (!IsCurrentScope(scope))
                return;


            Debug.LogError(
                $"[World] Enter failed: " +
                $"{scope}; {error}");


            if (state != WorldState.Entering)
            {
                Debug.LogError(
                    $"[World] Enter failure " +
                    $"occurred in unexpected state: " +
                    $"{state}");

                return;
            }


            if (!TryChangeState(
                    WorldState.Exiting))
            {
                return;
            }


            /*
             * 即使没有 SceneHandle，
             * Entering 阶段也可能已经修改了：
             *
             * AIContextReactionManager
             * Bubble
             * WindowSnapController
             * ClickThrough
             *
             * 所以统一交给 ExitRoutine 做补偿。
             */
            StartExitRoutine(scope);
        }

        private bool CompleteExit(
            WorldScope scope)
        {
            if (!IsCurrentScope(scope))
                return false;

            if (scope.SceneHandle != null)
            {
                Debug.LogError(
                    $"[World] 拒绝完成 Exit：" +
                    $"{scope} 仍然持有 SceneHandle。");

                return false;
            }
            
            if (scope.RuntimeBindings != null)
            {
                Debug.LogError(
                    $"[World] 拒绝完成 Exit：" +
                    $"{scope} 仍然持有 RuntimeBindings。");

                return false;
            }

            if (!CanTransition(
                    state,
                    WorldState.Desktop))
            {
                Debug.LogError(
                    $"[World] Illegal transition: " +
                    $"{state} -> Desktop");

                return false;
            }

            Debug.Log(
                $"[World] Exit completed: {scope}");

            // ----------------------------
            // Commit lifecycle teardown
            // ----------------------------
            //
            // 在广播 Desktop 之前，
            // CurrentScope 必须已经为空。
            //
            // 这样 StateChanged 的订阅者
            // 观察到 Desktop 时，
            // 不会同时看到一个活动 Scope。
            WorldState previous =
                state;

            currentScope = null;

            ApplyStateChange(
                previous,
                WorldState.Desktop);

            return true;
        }

        private bool IsCurrentScope(
            WorldScope scope)
        {
            if (scope == null
                || currentScope == null)
            {
                return false;
            }

            return ReferenceEquals(
                       scope,
                       currentScope)
                   &&
                   scope.InstanceId
                   ==
                   currentScope.InstanceId;
        }

        private bool TryChangeState(
            WorldState next)
        {
            WorldState previous =
                state;

            if (!CanTransition(
                    previous,
                    next))
            {
                Debug.LogError(
                    $"[World] Illegal transition: " +
                    $"{previous} -> {next}");

                return false;
            }

            ApplyStateChange(
                previous,
                next);

            return true;
        }

        private void ApplyStateChange(
            WorldState previous,
            WorldState next)
        {
            state = next;

            Debug.Log(
                $"[World] State: " +
                $"{previous} -> {next}");

            NotifyStateChanged(
                previous,
                next);
        }

        private static bool CanTransition(
            WorldState from,
            WorldState to)
        {
            switch (from)
            {
                case WorldState.Desktop:
                    return to
                           == WorldState.Entering;

                case WorldState.Entering:
                    return to
                           == WorldState.Explore
                           ||
                           to
                           == WorldState.Exiting;

                case WorldState.Explore:
                    return to
                           == WorldState.Exiting;

                case WorldState.Exiting:
                    return to
                           == WorldState.Desktop;

                default:
                    return false;
            }
        }

        private void NotifyStateChanged(
            WorldState previous,
            WorldState current)
        {
            Action<
                WorldState,
                WorldState> handlers =
                StateChanged;

            if (handlers == null)
                return;

            foreach (
                Action<
                    WorldState,
                    WorldState> handler
                in handlers.GetInvocationList())
            {
                try
                {
                    handler(
                        previous,
                        current);
                }
                catch (Exception exception)
                {
                    // Observer 失败不能破坏
                    // World 生命周期。
                    Debug.LogException(exception);
                }
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            currentScope = null;
            exitRoutineRunning = false;
        }
    }
}