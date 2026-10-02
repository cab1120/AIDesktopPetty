using System;
using System.Collections;
using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// IResourceService 在 Unity Scene 中的可序列化宿主。
    ///
    /// WorldCoordinator 依赖这个抽象类型，
    /// 而不是具体 LocalSceneResourceService。
    /// </summary>
    public abstract class WorldResourceServiceBehaviour
        : MonoBehaviour, IResourceService
    {
        public abstract bool CanLoadWorldScene(
            WorldDefinition definition,
            out string error);

        public abstract IEnumerator LoadWorldScene(
            WorldDefinition definition,
            Action<WorldSceneLoadResult> completed);

        public abstract IEnumerator ReleaseWorldScene(
            WorldSceneHandle handle,
            Action<WorldSceneReleaseResult> completed);
    }
}