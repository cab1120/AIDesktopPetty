# 架构现状与扩展风险

2026-09-21 静态审计；未执行编译/Player。里程碑见 PROJECT_PLAN.md。

## 已有责任

| 入口（相对 Assets/Scripts） | 当前职责和寿命 |
| --- | --- |
| Platform/Windows/WindowsPlatformBootstrap | 执行顺序 -10000，DontDestroyOnLoad，暴露静态 IWindowService |
| Platform/Windows/WindowsWindowService | 检查主 ABI 版本 1、能力位、错误域，原生调用限 Windows Player |
| DataBase/AppInitializer | Awake 初始化库/默认数据/情绪，退出关闭库 |
| DataBase/Data/GlobalSession | 静态当前用户/角色，多个组件即时读取 |
| Character/UI/UIManager → Character/AI/AIChat/AIChat | UI 协程、搜索/模型请求、回调写库耦合 |
| Presentation/DesktopContextManager → AIContextReactionManager | 停留检测、冷却、主动气泡、事件持久化 |
| Character/UI/Layout/DesktopPetLayoutController | Profile 驱动逻辑窗口尺寸和 Canvas，不是 World 状态机 |
| Art/Scenes/SakuramachiSceneLoop + Timeline | 世界局部 180 秒确定性演出，不是世界加载服务 |
| Shaders/Improved2.0；Assets/Dev/SakuraWeather | 人物材质和分层樱花，已被 3DScene 引用 |

## 当前数据流

聊天：保存用户消息 → 更新关系 → 取上下文 → 搜索缓存/规则/模型决策 → 搜索（必要时）→ Prompt → LLM → 显示 → 保存 AI 回复 → 更新关系。

登录：按 UserName 查用户/校验 → 按 UserName + CharacterName 查角色 → 设启用 → GlobalSession → 关系初始化。不是旧文档中的全局 GetByName。

窗口：Presentation → IWindowService → WindowsWindowService → Native DTO/ABI → DLL。前台检测仍例外地直接调用 user32。

场景：SampleScene 是桌宠主链；3DScene 是美术/循环/VFX 演示。未发现产品级世界加载入口。

## 扩展风险与修正

| ID | 证据/触发 | 影响与建议 |
| --- | --- | --- |
| R01 | DesktopContextManager 3 处 user32 DllImport，GetProcessName 在平台宏外 | 违反窗口隔离，非 Windows 编译也有风险；移入平台层，只向业务返回 DTO |
| R02 | UIManager 先 SaveUserMessage，ChatContextBuilder 读历史又追加当前输入 | 同一轮输入可能重复；按 messageId/turnId 排除当前历史项，不按文本去重 |
| R03 | 可同时发送，多处异步阶段读取 GlobalSession | 乱序、迟到回复串角色；请求捕获固定身份/sessionVersion，定义队列和过期结果策略 |
| R04 | 请求错误以 string 回调，UI 无状态判断仍落库/加关系 | 失败被当成功；引入 Success/Failure/Cancelled 类型结果 |
| R05 | 请求缺显式超时/统一释放，气泡锁仅回调释放 | 销毁/取消时无法闭环；明确所有者、Abort/Dispose、幂等清理 |
| R06 | AIChat 匿名订阅全局日志无解绑，每条同步写盘 | 跨场景重复订阅/残留；应用级唯一日志服务、解绑、轮转、脱敏 |
| R07 | UpdateUser 只改 User；CharacterProfile 用 UserName 归属；删除未见级联事务 | 改名可导致角色查不到；稳定 ID、迁移、事务与故障回滚 |
| R08 | DatabaseManager 只有 CreateTable，未见显式 schema 版本 | 世界表升级缺迁移控制；先建立版本/备份/迁移验证 |
| R09 | WorldService/资源句柄/应用模式缺失 | LoadScene 丢 UI/请求，Additive 易双输入/相机；引入 WorldScope、失败补偿与窗口快照 |
| R10 | 旧计划 15 秒 Intro 控列车，已有循环 20 秒停稳 | 两时钟争写；保留循环独占，Intro 仅控制角色/镜头 |
| R11 | SearchCache 全局静态，无会话维度；搜索错误可成为缓存文本 | 跨会话复用或失败污染；只缓存成功结果，按会话隔离/清理 |
| R12 | 原生 DLL 已有，仓库未见对应 C/C++ 源码；Importer 多平台项启用 | 无法独立重建或确认 ABI；补源码版本、构建方法、哈希、x64 冒烟 |

这些是静态可推导问题，不冒充现场复现。本次没有修改运行代码。

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
