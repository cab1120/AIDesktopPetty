using System;
using System.Collections;
using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    public sealed class WorldCoordinator : MonoBehaviour
    {
        [Header("M1-1 临时模拟参数")]
        [SerializeField, Min(0f)]
        private float simulateEnterSeconds = 2f;

        [SerializeField, Min(0f)]
        private float simulateExitSeconds = 1f;

        [SerializeField]
        private WorldCatalog worldCatalog;

        private WorldState state = WorldState.Desktop;

        private WorldScope currentScope;

        private long nextWorldInstanceId;

        private Coroutine transitionRoutine;

        public WorldState State => state;

        public WorldScope CurrentScope => currentScope;

        public bool HasActiveWorld => currentScope != null;

        public event Action<WorldState, WorldState> StateChanged;

        private void Awake()
        {
            state = WorldState.Desktop;
            currentScope = null;
            transitionRoutine = null;
        }

        /// <summary>
        /// 尝试开始进入一个世界。
        ///
        /// true:
        /// 本次进入请求已被接受。
        ///
        /// false:
        /// 请求被拒绝，例如当前已经在进入/世界/退出流程中。
        /// </summary>
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
                    $"当前状态为 {state}，不能再次进入世界。";

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
                error = "WorldCatalog 尚未配置。";
                return false;
            }

            if (!worldCatalog.TryValidate(
                    out string catalogError))
            {
                error =
                    $"WorldCatalog 无效：{catalogError}";

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

            long instanceId =
                ++nextWorldInstanceId;

            WorldScope scope =
                new WorldScope(
                    instanceId,
                    definition);

            currentScope = scope;

            if (!TryChangeState(WorldState.Entering))
            {
                currentScope = null;

                error =
                    "无法切换到 Entering 状态。";

                return false;
            }

            Debug.Log(
                $"[World] Enter accepted: {scope}");

            transitionRoutine =
                StartCoroutine(
                    EnterRoutine(scope));

            return true;
        }

        /// <summary>
        /// 请求退出当前世界。
        ///
        /// 这是一个幂等操作：
        /// 多次调用不会造成多次释放。
        /// </summary>
        public bool RequestExit()
        {
            switch (state)
            {
                case WorldState.Desktop:
                    // 已经在桌面。
                    // Exit 的目标已经满足，因此直接视为成功。
                    return true;

                case WorldState.Entering:
                    if (currentScope == null)
                    {
                        Debug.LogError(
                            "[World] Entering 状态没有有效 WorldScope。");

                        return false;
                    }

                    // 注意：
                    // 不在这里假装“加载已经取消”。
                    // 这里只记录用户已经不再需要这个世界。
                    currentScope.RequestExit();

                    Debug.Log(
                        $"[World] Exit requested while entering: {currentScope}");

                    return true;

                case WorldState.Explore:
                    if (currentScope == null)
                    {
                        Debug.LogError(
                            "[World] Explore 状态没有有效 WorldScope。");

                        return false;
                    }

                    if (!TryChangeState(WorldState.Exiting))
                        return false;

                    Debug.Log(
                        $"[World] Exit started: {currentScope}");

                    transitionRoutine =
                        StartCoroutine(ExitRoutine(currentScope));

                    return true;

                case WorldState.Exiting:
                    // 退出已经开始。
                    // 再次退出不重复执行清理。
                    return true;

                default:
                    Debug.LogError(
                        $"[World] Unknown state: {state}");

                    return false;
            }
        }

        private IEnumerator EnterRoutine(WorldScope scope)
        {
            // ===========================
            // M1-1 ONLY
            // ===========================
            // 这里只是模拟真实场景加载耗时。
            //
            // M1-2 / M1-3 会把这里替换成
            // IResourceService + Scene Load。
            yield return new WaitForSecondsRealtime(
                simulateEnterSeconds);

            if (!IsCurrentScope(scope))
            {
                transitionRoutine = null;
                yield break;
            }

            // 用户可能在“加载”过程中已经点击退出。
            if (scope.ExitRequested)
            {
                if (!TryChangeState(WorldState.Exiting))
                {
                    transitionRoutine = null;
                    yield break;
                }

                Debug.Log(
                    $"[World] Enter completed but exit was already requested: {scope}");

                yield return new WaitForSecondsRealtime(
                    simulateExitSeconds);

                CompleteExit(scope);

                transitionRoutine = null;
                yield break;
            }

            if (!TryChangeState(WorldState.Explore))
            {
                transitionRoutine = null;
                yield break;
            }

            Debug.Log(
                $"[World] Enter completed: {scope}");

            transitionRoutine = null;
        }

        private IEnumerator ExitRoutine(WorldScope scope)
        {
            // M1-1 ONLY:
            // 模拟以后真正的世界清理 / 场景卸载。
            yield return new WaitForSecondsRealtime(
                simulateExitSeconds);

            if (!IsCurrentScope(scope))
            {
                transitionRoutine = null;
                yield break;
            }

            CompleteExit(scope);

            transitionRoutine = null;
        }

        private void CompleteExit(WorldScope scope)
        {
            if (!IsCurrentScope(scope))
                return;

            Debug.Log(
                $"[World] Exit completed: {scope}");

            // 非常重要：
            // 先让世界实例失效。
            currentScope = null;

            TryChangeState(WorldState.Desktop);
        }

        private bool IsCurrentScope(WorldScope scope)
        {
            if (scope == null || currentScope == null)
                return false;

            return ReferenceEquals(scope, currentScope)
                   &&
                   scope.InstanceId == currentScope.InstanceId;
        }

        private bool TryChangeState(WorldState next)
        {
            WorldState previous = state;

            if (!CanTransition(previous, next))
            {
                Debug.LogError(
                    $"[World] Illegal transition: {previous} -> {next}");

                return false;
            }

            state = next;

            Debug.Log(
                $"[World] State: {previous} -> {next}");

            NotifyStateChanged(previous, next);

            return true;
        }

        private static bool CanTransition(
            WorldState from,
            WorldState to)
        {
            switch (from)
            {
                case WorldState.Desktop:
                    return to == WorldState.Entering;

                case WorldState.Entering:
                    return to == WorldState.Explore
                           ||
                           to == WorldState.Exiting;

                case WorldState.Explore:
                    return to == WorldState.Exiting;

                case WorldState.Exiting:
                    return to == WorldState.Desktop;

                default:
                    return false;
            }
        }

        private void NotifyStateChanged(
            WorldState previous,
            WorldState current)
        {
            Action<WorldState, WorldState> handlers =
                StateChanged;

            if (handlers == null)
                return;

            foreach (Action<WorldState, WorldState> handler
                     in handlers.GetInvocationList())
            {
                try
                {
                    handler(previous, current);
                }
                catch (Exception exception)
                {
                    // Observer 的异常不应该破坏
                    // WorldCoordinator 自己的状态机。
                    Debug.LogException(exception);
                }
            }
        }

        private void OnDestroy()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            currentScope = null;
        }
    }
}