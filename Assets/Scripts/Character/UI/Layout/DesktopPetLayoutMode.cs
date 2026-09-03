public enum DesktopPetLayoutMode
{
    /// <summary>
    /// 登录状态。
    /// UI 按 180x180 设计。
    /// </summary>
    Login,
    
    /// <summary>
    /// 桌宠头像折叠状态。
    /// UI 按 180x250 设计。
    /// </summary>
    PetCollapsed,


    /// <summary>
    /// 主聊天界面。
    ///
    /// Window:
    /// 432x768
    ///
    /// UI Design:
    /// 720x1280
    /// </summary>
    Chat,


    /// <summary>
    /// 小型控制面板。
    /// UI 按 180x250 设计。
    /// </summary>
    ControlPanel,


    /// <summary>
    /// 用户 / 角色等管理界面。
    /// UI 按 432x768 设计。
    /// </summary>
    Management,


    /// <summary>
    /// 聊天记录管理界面。
    /// UI 按 432x768 设计。
    /// </summary>
    ChatHistory
}