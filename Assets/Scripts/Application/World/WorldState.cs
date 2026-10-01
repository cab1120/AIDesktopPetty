namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 整个应用相对于 3D 世界所处的生命周期状态。
    /// 注意：这不是桌宠 UI Layout 状态。
    /// </summary>
    public enum WorldState
    {
        /// <summary>
        /// 当前没有活动的 3D 世界，处于桌面模式。
        /// </summary>
        Desktop,

        /// <summary>
        /// 正在创建/加载一个世界。
        /// </summary>
        Entering,

        /// <summary>
        /// 世界已经准备完毕，可以正常交互。
        /// </summary>
        Explore,

        /// <summary>
        /// 正在退出、清理和释放当前世界。
        /// </summary>
        Exiting
    }
}