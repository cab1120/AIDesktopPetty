# 从当前代码与原计划拆出实现任务

核对日期：2026-10-01，代码基线 Git 6853f6f。这是实现定位指南，不替代或更新 [PROJECT_PLAN.md](PROJECT_PLAN.md)与用户的原 PDF，也不表示后续功能已获实施授权。先读 [README](../README.md)、[架构](ARCHITECTURE.md)、[M0 不变量](M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md)与 [接口](API_EVENTS.md)。

## 用计划时先校准事实

| 原计划可能提出的工作 | 当前事实 | 拆任务时怎样处理 |
| --- | --- | --- |
| 捕获身份、串行、旧请求失效 | SessionSnapshot、ChatTurn、ConversationService 和 AIChat 已实现 | 核对提交边界并补运行验收，不另建普通聊天入口 |
| 数据归属与删除 | CharacterProfile 有 UserId、schema v1、事务删除和旧孤儿隔离；用户确认原库登录恢复 | 先处理情绪仍传名称的缺口；新表/认领需独立迁移，不自动清理原数据 |
| Windows 能力隔离 | IWindowService、WindowsWindowService、Native ABI 和前台平台服务存在 | 扩展接口或适配消费者，禁止把 Win32 放回业务层 |
| 站台/欢迎演出 | 3DScene 已有循环、Timeline 音频、材质和樱花 | 复用场景，补产品进入/退出，不重建列车轨道 |
| 旧列车时间或 OnTrainStop | 20 秒停稳、35 秒发车、45 秒隐藏；无业务 OnTrainStop | 阶段消费接口待新增，要定义跳转/重复行为 |
| YooAsset/UniTask/Cinemachine/HybridCLR | 未安装，当前使用协程和 Unity 场景/资源能力 | 先证明闭环，再按原计划与用户决定引入 |
| asmdef、World/Resource/Action | 自有脚本在默认程序集；相关服务不存在 | 先画引用图和最小消费者，逐模块拆分并保留 GUID |

## 按功能定位修改入口

路径相对仓库；以下 Character、DataBase、Presentation、Platform、Art 均位于 Assets/Scripts 下。逐文件职责见 [SCRIPTS.md](SCRIPTS.md)。

| 工作 | 入口与必须追踪的链路 | 实现/验收应包含 |
| --- | --- | --- |
| 普通输入/回复/重试 | Character/UI/UIManager.cs → Character/Conversation/ConversationService.cs → Character/AI/AIChat/AIChat.cs → DataBase/Data/ChatMessage/ChatMessageService.cs | 保留 TurnId/消息 ID；定义重试是否复用已保存输入；跨 yield 校验快照；FIFO、三态终结与防重复落库 |
| 模型/搜索协议 | AIChat/ChatReplyResult.cs、ChatReplyParser.cs、SearchDecison/SearchDecisionService.cs、SearchDecisonMode/SearchRuleFilter.cs、SearchCacheService.cs | 配置端点/模型/单请求超时；搜索失败不能当“不搜索”；HTTP 错误体不能保存为 Assistant；假服务故障注入 |
| 角色 Prompt | Character/AI/Prompt/CharacterPromptLoader.cs → CharacterPromptBuilder.cs；DataBase/Data/Character/CharacterProfileData.cs | 区分角色 JSON 和运行上下文；用快照；默认文件仅初次创建入库；定义编辑对版本的影响 |
| 情绪归属缺口 E01 | AIChat.AIPrompt/AIBubblePrompt、Prompt/Emotion/EmotionMemory.cs、DataBase/Data/Emotion/SQLiteEmotionStorage.cs、Test/EmotionBuildDebugTest.cs | 改用稳定 ID；明确登录前是否生成情绪；历史名称键映射前检查歧义；无法认领者保留隔离；新/旧库、改名、级联用副本回归 |
| 账户/角色/数据库 | DataBase/AuthService.cs、DatabaseManager.cs、DatabaseSchemaMigrator.cs、Data/UserData/UserRepository.cs、Data/Character/CharacterRepository.cs | 启动错误门槛；备份、版本迁移；隔离标记一致性；删除事务成功后刷新；不凭名称猜测归属 |
| 主动搭话 | Presentation/DesktopContextManager.cs → Character/AI/AutoTalk/AIContextReactionManager.cs → AIChat.GetAIBubbleReply → Character/UI/BubbleUIManager.cs、DataBase/Data/InteractionEvent/InteractionEventService.cs | 停留/过滤/冷却、锁、停用/销毁/换角色、旧气泡隐藏；当前不具备普通聊天的结构化结果协议 |
| 前台/窗口/布局 | Platform/Windows/IWindowService.cs、WindowsWindowService.cs、WindowsForegroundContextService.cs；Presentation/Window/*；Character/UI/Layout/* | logical size 与 physical bounds；服务失败时 UI/窗口恢复；可见 Player、多屏/不同 DPI |
| 日志/安全 | DataBase/AppLogService.cs、AIContextReactionManager.cs、InteractionEventService.cs、Tools/PasswordHasher.cs、登录与默认初始化 | 区分自定义日志、Unity 日志、事件库、网络发送；密码升级涉及旧哈希兼容；默认管理员加固被用户延期 |
| 世界进入/退出 | **待新增应用/世界协调层**；现有 SampleScene、3DScene、DesktopPetLayoutController、对话/气泡、窗口接口 | 加载失败恢复、重复进入/退出、输入与音频单一所有者、会话与世界取消边界 |
| 欢迎/站台交互 | Art/Scenes/SakuramachiSceneLoop.cs、SakuramachiLoopTrack.cs、SakuramachiLoopClip.cs、3DScene 的 Director 和角色/相机引用 | 循环独占列车/杆/灯；欢迎只写角色/镜头；跳过保留列车时间；阶段事件待新增 |
| 樱花/材质/音频 | Assets/Dev/SakuraWeather/Runtime/Scripts/*、Shaders/Improved2.0/Editor/CharacterStyleAMaterials.cs、Art/Skybox/SkyboxRotator.cs、已有音轨 | 保留 A 材质配色/纹理和音轨；配置工具可能保存/覆盖资源，先查目标；当前 Player 性能实测 |

## 站台闭环怎样拆成可实现步骤

这是将原计划转为工程任务的建议顺序；协调层、接口和阶段事件均**待新增**，不是已有实现。

1. **冻结桌面基线与入口。** 定义进入按钮、应用模式、活动聊天完成或取消、主动气泡暂停策略。检查 SampleScene 引用，记录窗口 bounds、布局、透明、置顶、穿透及 UI 状态。
2. **定义世界寿命。** 区分应用、用户会话、站台世界、UI 视图。用最小协调层建立“桌面 → 加载 → 站台 → 返回”与失败状态。WindowsPlatformBootstrap 已常驻，数据库由 AppInitializer 管理，不把所有 Manager 都常驻化。
3. **加载一次并具备补偿。** 常驻桌面壳 + Additive 是当前建议，最终方式需按原计划/用户决定。等待加载与有效绑定后再转输入/窗口；异常、取消、重复点击、中途退出都恢复桌面快照。保证单一有效输入、相机和 AudioListener。
4. **接入现有 3DScene。** 核对 Loop、Director、相机、角色、声源、VFX、材质；完成切换方案后才改构建列表。保留循环时间与轨道，避免双写列车。
5. **欢迎与最小交互。** 先做可完成/可跳过的角色和相机演出，再接一个可取消 POI。阶段通知需定义跳时、回卷、停止、多次订阅；Timeline stopped 不代表欢迎成功。
6. **退出和故障恢复。** 停世界请求/动作/Timeline/音频/VFX，解绑、卸载，再恢复桌面窗口/输入。退出会话和退出世界不同；旧世界结果需独立有效性判断。
7. **闭环验收后扩展。** 测进入、交互、跳过、取消、反复进出、换角色、失焦、加载失败和不同 DPI；再按原计划考虑资源框架、AI Action 或房间。

未来 Action 经白名单与可取消执行器，不执行 LLM 返回的脚本或对象路径。若引入 worldInstanceId/requestId，须定义创建者、失效条件和提交校验，不能仅用 sessionVersion 替代。

## 实现任务的交付模板

- **触发与行为：** 用户做什么，成功/失败/取消后看到什么。
- **当前证据：** 现有文件、方法、场景/Prefab 绑定；规划接口不算证据。
- **修改清单：** 逐文件责任变化、依赖方向、序列化与 GUID 影响。
- **身份与寿命：** App/Session/World/View 谁拥有状态，跨 yield 捕获什么，谁取消和清理。
- **持久化：** 归属 ID、事务、版本、备份与历史兼容。
- **失败补偿：** 每个中间步骤怎样释放请求、队列、订阅、场景、输入和窗口。
- **验收：** 可重复输入、故障注入、期望记录/事件/UI；说明 Editor/Player/网络/硬件/副本条件。
- **待用户决定：** 只列无法从代码获取且影响产品语义的分支。

## 可直接交给其他 LLM 的请求

> 请结合本仓库 README 与原计划制定下一阶段具体实现步骤。校准已实现内容、验证证据和已知缺口，按依赖排列任务。每项给出文件/方法、待新增接口、状态与寿命归属、取消/失败恢复、数据兼容和验收。不要另建普通聊天队列，不把世界/资源/Action 规划写成已有实现，不改列车时序。指出必须由开发者决定的问题。本请求用于制定步骤，实施另行授权。

若仅拿到 README 与原计划而不能读仓库，把文件/方法作为定位线索，列明需核对的源码，不虚构签名、绑定或测试。可访问仓库时按 [AGENTS.md](../AGENTS.md)先验证再落地。
