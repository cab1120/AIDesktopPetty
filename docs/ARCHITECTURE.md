# 架构现状与扩展风险

2026-09-30 核对；M0 代码及验证状态见 [M0 架构移交](M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md)，逐脚本原因见 [修改报告](M0_IMPLEMENTATION_REPORT_2026-09-30.md)。里程碑见 [PROJECT_PLAN.md](PROJECT_PLAN.md)。

## 已有责任

| 入口（相对 Assets/Scripts） | 当前职责和寿命 |
| --- | --- |
| Platform/Windows/WindowsPlatformBootstrap | 执行顺序 -10000，DontDestroyOnLoad，暴露静态 IWindowService |
| Platform/Windows/WindowsWindowService | 检查主 ABI 版本 1、能力位、错误域，原生调用限 Windows Player |
| DataBase/AppInitializer + AppLogService | Awake 初始化日志/数据库/默认数据/情绪，错误呈现到登录页；退出释放日志和数据库 |
| DataBase/Data/GlobalSession + SessionSnapshot | 静态当前用户/角色、会话版本与请求时不可变快照；管理 UI 仍有同步当前值读取 |
| Character/UI/UIManager → Character/Conversation/ConversationService → AIChat | 普通聊天按 Turn 串行，结果经会话校验后写库；主动气泡是独立请求 |
| Presentation/DesktopContextManager → AIContextReactionManager | 停留检测、冷却、主动气泡、事件持久化 |
| Character/UI/Layout/DesktopPetLayoutController | Profile 驱动逻辑窗口尺寸和 Canvas，不是 World 状态机 |
| Art/Scenes/SakuramachiSceneLoop + Timeline | 世界局部 180 秒确定性演出，不是世界加载服务 |
| Shaders/Improved2.0；Assets/Dev/SakuraWeather | 人物材质和分层樱花，已被 3DScene 引用 |

## 当前数据流

聊天：创建 `ChatTurn` 并排队 → 执行时保存用户消息 → 更新关系 → 取排除当前消息的上下文 → 搜索缓存/规则/模型决策 → 搜索（必要时）→ Prompt → LLM → 校验会话 → 保存成功回复 → 更新关系 → 显示。失败只显示临时重试提示，不存 Assistant 消息。

登录：按 UserName 查用户/校验 → 解析稳定 UserId 后按 CharacterName 查角色 → 事务设启用 → GlobalSession → 关系初始化。

窗口：Presentation → IWindowService → WindowsWindowService → Native DTO/ABI → DLL。前台检测由 Presentation 调用 Platform/Windows/WindowsForegroundContextService，user32 ABI 在 Native。

场景：SampleScene 是桌宠主链；3DScene 是美术/循环/VFX 演示。未发现产品级世界加载入口。

## 扩展风险与修正

| ID | 证据/触发 | 影响与建议 |
| --- | --- | --- |
| R01 | DesktopContextManager 曾直接 user32 DllImport | 已移至 Platform/Windows/Native；仍需 Player 平台回归 |
| R02 | 曾经把当前输入从历史和参数各加入一次 | 当前按 `UserMessageId` 排除，待联网请求体验证 |
| R03 | 曾经允许并发发送且异步阶段读当前会话 | 当前普通聊天串行并传递快照，旧请求 Abort；待切角色/退出登录运行回归 |
| R04 | 曾经把错误字符串当回复落库 | 当前普通回复有 Success/Failure/Cancelled 与超时等失败原因；运行故障注入待验证 |
| R05 | 曾经缺超时/释放，气泡锁可能残留 | 当前设 30 秒超时、Abort/Dispose 与气泡 `finally` 释放；待 Unity 销毁和停用回归 |
| R06 | AIChat 曾每条同步写原文 | 已改应用级唯一订阅、轮转和原文指纹；日志写盘失败场景待验证 |
| R07 | UpdateUser 曾只改 User；CharacterProfile 曾用 UserName 归属 | 已加 UserId 迁移、改名同步和事务级联；合成库回归通过，旧库副本待验 |
| R08 | DatabaseManager 曾只有 CreateTable | 现有数据表有版本 1、备份/迁移；未来世界表仍需独立迁移设计 |
| R09 | WorldService/资源句柄/应用模式缺失 | LoadScene 丢 UI/请求，Additive 易双输入/相机；引入 WorldScope、失败补偿与窗口快照 |
| R10 | 旧计划 15 秒 Intro 控列车，已有循环 20 秒停稳 | 两时钟争写；保留循环独占，Intro 仅控制角色/镜头 |
| R11 | SearchCache 曾全局静态且无会话维度 | 当前按用户/角色/版本隔离、换会话清理且不缓存空或失败结果；待运行回归 |
| R12 | 原生 DLL 已有，仓库未见对应 C/C++ 源码 | Importer 已限 Windows x64/Editor；仍需补原生源码、构建方法、哈希、x64 冒烟 |

这些状态来自源码和静态编译，不冒充现场复现。

## 建议的增量结构（尚未实现）

保留现有目录，新增薄应用协调层、World/Resource 接口，让旧模块通过适配接入。不要先迁移全仓库。

- AppScope：配置、日志、平台、数据库、资源服务。
- SessionScope：固定身份/sessionVersion、对话队列和取消。
- WorldScope：场景句柄、角色、POI、动作、Timeline、音频/VFX、订阅。
- ViewScope：UI 显示与订阅，不持有长期会话状态。

依赖方向：Presentation/World → 窄接口/数据契约 → 服务实现。避免无消费者的通用 EventBus 和过大的 IDataService。asmdef 逐步引入；自定义程序集不能直接依赖仍留在 Assembly-CSharp 的类型，先抽底层契约。

初版建议常驻桌面壳 + Additive 世界。进入时暂停桌面交互/主动气泡、保持单一输入和 AudioListener，保存窗口 bounds/布局/透明/置顶/穿透；失败和退出都恢复快照。该行为仍是建议。

## 验证界限

.ai 中 2026-09-15 的 Shader/循环验证是历史证据；时序、音频、材质现已变化，需要重新做当前 Player 验证。SakuraWeather Benchmark 属于局部实验，设备与多个指标为空，不能作为整机 60 FPS 证明。
