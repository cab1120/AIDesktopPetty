using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 一个“世界类型”的静态配置。
    ///
    /// 注意：
    /// WorldDefinition 描述“樱町是什么”，
    /// WorldScope 描述“这一次进入樱町的实例”。
    /// </summary>
    [CreateAssetMenu(
        menuName = "AI Desktop Petty/World/World Definition",
        fileName = "WorldDefinition")]
    public sealed class WorldDefinition : ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        private string worldId;

        [SerializeField]
        private string displayName;

        [Header("Local Resource")]

        [Tooltip(
            "Unity Build Settings 中的场景路径，例如 " +
            "Assets/Scenes/3DScene.unity")]
        [SerializeField]
        private string scenePath;

        public string WorldId => worldId;

        public string DisplayName => displayName;

        public string ScenePath => scenePath;

        /// <summary>
        /// 检查这个 Definition 是否具备最基本的运行条件。
        ///
        ///这里只检查配置本身，
        ///不检查场景是否真的已加入 Build Settings。
        ///那是 ResourceService 的责任。
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(worldId))
            {
                error = "WorldId 不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = $"World '{worldId}' 的 DisplayName 不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                error = $"World '{worldId}' 的 ScenePath 不能为空。";
                return false;
            }

            if (!scenePath.StartsWith("Assets/"))
            {
                error =
                    $"World '{worldId}' 的 ScenePath 必须从 Assets/ 开始。";

                return false;
            }

            if (!scenePath.EndsWith(".unity"))
            {
                error =
                    $"World '{worldId}' 的 ScenePath 必须指向 .unity 场景。";

                return false;
            }

            error = null;
            return true;
        }
    }
}