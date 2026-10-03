using Platform.Windows.Models;
using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 进入某一次 World 前，
    /// 桌面表现层真实运行状态的快照。
    ///
    /// 注意：
    /// 这是 Runtime Snapshot，
    /// 不是桌面默认配置。
    /// </summary>
    public sealed class DesktopPresentationSnapshot
    {
        public DesktopPetLayoutMode LayoutMode
        {
            get;
        }

        public bool HasWindowRect
        {
            get;
        }

        public WindowRect WindowRect
        {
            get;
        }

        public bool HasMonitorInfo
        {
            get;
        }

        public WindowMonitorInfo MonitorInfo
        {
            get;
        }

        public bool DesktopCameraEnabled
        {
            get;
        }

        public bool DesktopAudioListenerEnabled
        {
            get;
        }

        public bool ClickThroughControllerEnabled
        {
            get;
        }

        public bool WindowSnapControllerEnabled
        {
            get;
        }

        public bool AIReactionManagerEnabled
        {
            get;
        }

        public BehaviourState[] DesktopBehaviourStates
        {
            get;
        }

        public DesktopPresentationSnapshot(
            DesktopPetLayoutMode layoutMode,
            bool hasWindowRect,
            WindowRect windowRect,
            bool hasMonitorInfo,
            WindowMonitorInfo monitorInfo,
            bool desktopCameraEnabled,
            bool desktopAudioListenerEnabled,
            bool clickThroughControllerEnabled,
            bool windowSnapControllerEnabled,
            bool aiReactionManagerEnabled,
            BehaviourState[] desktopBehaviourStates)
        {
            LayoutMode =
                layoutMode;

            HasWindowRect =
                hasWindowRect;

            WindowRect =
                windowRect;

            HasMonitorInfo =
                hasMonitorInfo;

            MonitorInfo =
                monitorInfo;

            DesktopCameraEnabled =
                desktopCameraEnabled;

            DesktopAudioListenerEnabled =
                desktopAudioListenerEnabled;

            ClickThroughControllerEnabled =
                clickThroughControllerEnabled;

            WindowSnapControllerEnabled =
                windowSnapControllerEnabled;

            AIReactionManagerEnabled =
                aiReactionManagerEnabled;

            DesktopBehaviourStates =
                desktopBehaviourStates
                ?? new BehaviourState[0];
        }
    }

    public readonly struct BehaviourState
    {
        public Behaviour Target
        {
            get;
        }

        public bool WasEnabled
        {
            get;
        }

        public BehaviourState(
            Behaviour target,
            bool wasEnabled)
        {
            Target =
                target;

            WasEnabled =
                wasEnabled;
        }
    }
}