using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    [CreateAssetMenu(
        menuName =
            "AI Desktop Petty/World/" +
            "World Presentation Profile",
        fileName =
            "WorldPresentationProfile")]
    public sealed class WorldPresentationProfile
        : ScriptableObject
    {
        [Header("Window Logical Size")]

        [SerializeField, Min(1)]
        private int windowWidth =
            1280;

        [SerializeField, Min(1)]
        private int windowHeight =
            720;

        [Header("Placement")]

        [Tooltip(
            "进入 World 后是否把窗口居中到" +
            "进入前所在显示器的工作区。")]
        [SerializeField]
        private bool centerOnCurrentMonitor =
            true;

        public int WindowWidth =>
            windowWidth;

        public int WindowHeight =>
            windowHeight;

        public bool CenterOnCurrentMonitor =>
            centerOnCurrentMonitor;
    }
}