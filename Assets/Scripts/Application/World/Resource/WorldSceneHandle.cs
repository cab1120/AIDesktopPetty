using UnityEngine.SceneManagement;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 一次已经加载的世界场景资源句柄。
    ///
    /// WorldScope 持有它；
    /// IResourceService 知道如何释放它。
    /// </summary>
    public sealed class WorldSceneHandle
    {
        public string ScenePath { get; }

        public Scene Scene { get; }

        public bool IsReleased { get; private set; }

        public bool IsLoaded
        {
            get
            {
                return !IsReleased
                       &&
                       Scene.IsValid()
                       &&
                       Scene.isLoaded;
            }
        }

        internal WorldSceneHandle(
            string scenePath,
            Scene scene)
        {
            ScenePath = scenePath;
            Scene = scene;
        }

        internal void MarkReleased()
        {
            IsReleased = true;
        }
    }
}