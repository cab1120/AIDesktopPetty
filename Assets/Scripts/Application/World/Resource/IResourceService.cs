using System;
using System.Collections;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// World 层看到的资源能力。
    ///
    /// WorldCoordinator 不应该知道：
    /// 当前资源来自 Unity Build Settings、
    /// YooAsset、本地 Bundle 还是远端。
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// 在真正加载前检查当前后端是否有能力加载该世界。
        /// </summary>
        bool CanLoadWorldScene(
            WorldDefinition definition,
            out string error);

        /// <summary>
        /// 异步加载世界场景。
        /// 完成后必须返回一个 Success 或 Failure。
        /// </summary>
        IEnumerator LoadWorldScene(
            WorldDefinition definition,
            Action<WorldSceneLoadResult> completed);

        /// <summary>
        /// 释放之前返回的世界场景句柄。
        /// </summary>
        IEnumerator ReleaseWorldScene(
            WorldSceneHandle handle,
            Action<WorldSceneReleaseResult> completed);
    }
}