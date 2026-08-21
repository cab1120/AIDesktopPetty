namespace Platform.Windows
{
    /// <summary>
    /// Unity 业务层能够使用的桌面窗口能力。
    ///
    /// 该接口不得暴露 HWND、
    /// Win32 常量或 Native Plugin 细节。
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// 平台窗口服务是否已经成功初始化。
        /// </summary>
        bool IsInitialized { get; }


        /// <summary>
        /// 开启或关闭无边框窗口。
        /// </summary>
        bool SetBorderless(
            bool enabled);
        
        /// <summary>
        /// 设置窗口是否启用透明背景所需的
        /// Windows DWM 支持。
        /// </summary>
        bool SetTransparentBackground(
            bool enabled);
        
        /// <summary>
        /// 按 Windows logical size 调整窗口。
        /// </summary>
        bool SetLogicalSize(
            WindowLogicalSize size);


        /// <summary>
        /// 设置是否保持为 TopMost。
        /// </summary>
        bool SetTopMost(
            bool enabled);


        /// <summary>
        /// 获取窗口当前所在显示器。
        /// </summary>
        bool TryGetCurrentMonitorInfo(
            out WindowMonitorInfo info);
    }
}