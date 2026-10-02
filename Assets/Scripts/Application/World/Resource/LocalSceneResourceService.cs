using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// M1 本地资源实现。
    ///
    /// 世界场景直接来自 Unity Player Build Settings。
    ///
    /// 注意：
    /// WorldCoordinator 不应该直接调用 SceneManager；
    /// SceneManager 被限制在这个 Adapter 内。
    /// </summary>
    public sealed class LocalSceneResourceService
        : IResourceService
    {
        public bool CanLoadWorldScene(
            WorldDefinition definition,
            out string error)
        {
            if (definition == null)
            {
                error = "WorldDefinition 不能为空。";
                return false;
            }

            if (!definition.TryValidate(
                    out string definitionError))
            {
                error = definitionError;
                return false;
            }

            // 1. 先检查这个场景是否已经被加载。
            Scene existingScene =
                SceneManager.GetSceneByPath(
                    definition.ScenePath);

            if (existingScene.IsValid()
                && existingScene.isLoaded)
            {
                error =
                    $"场景已经加载：" +
                    $"{definition.ScenePath}";

                return false;
            }

            // 2. 按“完整场景路径”检查它是否属于 Player Build。
            int buildIndex =
                SceneUtility.GetBuildIndexByScenePath(
                    definition.ScenePath);

            if (buildIndex < 0)
            {
                error =
                    $"场景未加入 Build Settings：" +
                    $"{definition.ScenePath}";

                return false;
            }

            error = null;
            return true;
        }

        public IEnumerator LoadWorldScene(
            WorldDefinition definition,
            Action<WorldSceneLoadResult> completed)
        {
            if (completed == null)
                throw new ArgumentNullException(
                    nameof(completed));

            if (!CanLoadWorldScene(
                    definition,
                    out string validationError))
            {
                completed(
                    WorldSceneLoadResult.Failure(
                        validationError));

                yield break;
            }

            AsyncOperation operation;

            try
            {
                operation =
                    SceneManager.LoadSceneAsync(
                        definition.ScenePath,
                        LoadSceneMode.Additive);
            }
            catch (Exception exception)
            {
                completed(
                    WorldSceneLoadResult.Failure(
                        $"启动场景加载失败：" +
                        $"{exception.Message}"));

                yield break;
            }

            if (operation == null)
            {
                completed(
                    WorldSceneLoadResult.Failure(
                        "SceneManager.LoadSceneAsync " +
                        "返回了 null。"));

                yield break;
            }

            while (!operation.isDone)
            {
                // 可以在这里回调 progress 表示进度条
                yield return null;
            }

            Scene scene =
                SceneManager.GetSceneByPath(
                    definition.ScenePath);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                completed(
                    WorldSceneLoadResult.Failure(
                        $"加载操作结束，但场景没有处于 " +
                        $"Loaded 状态：" +
                        $"{definition.ScenePath}"));

                yield break;
            }

            WorldSceneHandle handle =
                new WorldSceneHandle(
                    definition.ScenePath,
                    scene);

            completed(
                WorldSceneLoadResult.Success(handle));
        }

        public IEnumerator ReleaseWorldScene(
            WorldSceneHandle handle,
            Action<WorldSceneReleaseResult> completed)
        {
            if (completed == null)
                throw new ArgumentNullException(
                    nameof(completed));

            if (handle == null)
            {
                completed(
                    WorldSceneReleaseResult.Failure(
                        "WorldSceneHandle 不能为空。"));

                yield break;
            }

            // 已释放时再次 Release：
            // 结果已经满足，所以保持幂等成功。
            if (handle.IsReleased)
            {
                completed(
                    WorldSceneReleaseResult.Success());

                yield break;
            }

            Scene scene = handle.Scene;

            // 如果场景已经不在了，
            // 对这个 handle 来说目标同样已经达到。
            if (!scene.IsValid() || !scene.isLoaded)
            {
                handle.MarkReleased();

                completed(
                    WorldSceneReleaseResult.Success());

                yield break;
            }

            AsyncOperation operation;

            try
            {
                operation =
                    SceneManager.UnloadSceneAsync(scene);
            }
            catch (Exception exception)
            {
                completed(
                    WorldSceneReleaseResult.Failure(
                        $"启动场景卸载失败：" +
                        $"{exception.Message}"));

                yield break;
            }

            if (operation == null)
            {
                completed(
                    WorldSceneReleaseResult.Failure(
                        "SceneManager.UnloadSceneAsync " +
                        "返回了 null。"));

                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            handle.MarkReleased();

            completed(
                WorldSceneReleaseResult.Success());
        }
    }
}