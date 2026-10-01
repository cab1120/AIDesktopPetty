# 架构、数据流与当前扩展边界

核对日期：2026-10-01，代码基线 Git 6853f6f。本文件描述当前代码，建议接口另行标注。[逐脚本索引](SCRIPTS.md)、[现有接口](API_EVENTS.md)、[M0 验收](M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md)、[实现指南](IMPLEMENTATION_GUIDE.md)分别提供定位、契约、证据和拆任务方法。

## 运行入口与场景

- SampleScene 是唯一启用的构建场景，挂载桌宠初始化、登录/管理/聊天、ConversationService、AIChat、前台/主动气泡和 Windows 表现组件。
- 3DScene 是独立站台内容，已有循环/Timeline 音频/樱花/天空/角色材质，不在构建列表，也没有产品级进入与返回服务。
- Assets/Prefab 是 UI Prefab 目录；目录拼写以实际路径为准。Test、LargeScene、樱花 Demo 等不作为桌宠启动契约。
- 自有脚本无 asmdef，当前按默认程序集编译。拆分前需分析静态服务、UI、平台和 Editor 的引用，不能直接从自定义程序集引用仍在 Assembly-CSharp 的类型。

## 当前责任与寿命

| 组件 | 所有的状态/责任 | 寿命与释放 |
| --- | --- | --- |
| WindowsPlatformBootstrap | 唯一静态 IWindowService，启动 ABI/能力诊断 | 执行顺序 -10000，DontDestroyOnLoad；不是世界加载器 |
| WindowsWindowService | Windows Player 窗口实现、Native 状态/错误 | 由 Bootstrap 创建；Native 细节不流入 UI |
| AppInitializer | 应用日志、数据库/默认数据/情绪初始化，StartupError | Awake 到 OnApplicationQuit；它并未实现跨场景 AppScope |
| DatabaseManager | 静态 SQLiteConnection，迁移与建表 | 初始化后供业务共享，退出关闭；不因离开世界随意关闭 |
| GlobalSession | 私有当前身份和递增版本，公开快照/角色与权限操作 | 静态；登录、退出、切角色和相关资料变化使旧快照失效 |
| ConversationService | pendingTurns、currentTurn、队列协程及终态事件 | MonoBehaviour；OnDisable 解绑、取消、清队列，普通聊天用例所有者 |
| AIChat | 配置、活动 HTTP 集合、气泡锁和搜索缓存清理 | 会话变化 Abort/清理；OnDisable 取消请求；HTTP 执行器负责 Dispose |
| UIManager | 气泡对象、待执行 TurnId 字典和滚动 | 身份/开始/取消监听在 Awake/OnDestroy；成功/失败监听在 OnEnable/OnDisable |
| DesktopContextManager | 前台停留与重复标题检测 | 定期从平台获取上下文，OnWindowChanged 是 Action 字段 |
| AIContextReactionManager | 主动气泡冷却/请求锁/协程 | OnEnable 订阅，OnDisable 解绑、取消气泡请求并停协程 |
| BubbleUIManager | 单个主动气泡及布局/隐藏协程 | 会话变更隐藏，默认显示 5 秒 |
| DesktopPetLayoutController | LayoutProfile、Canvas 和 CurrentMode | 模式是 UI/窗口布局，不是应用/世界状态 |
| SakuramachiSceneLoop + Director | 列车/杆/灯、裁剪与运行音频，原状态快照 | 3DScene 局部；统一 180 秒时间求值与停止恢复 |
| SakuraWeather / Skybox | VFX 分层配置/相机关系与天空材质 | 3DScene 局部，不作为应用常驻 Manager |

实际控制来自 MonoBehaviour 与静态服务的组合。App/Session/World/View 的划分是未来整理方向；当前只有部分寿命边界，不能说完整 Scope 框架已经存在。

## 启动、登录和会话变化

启动：AppInitializer.Awake → AppLogService.Start → DatabaseManager.Initialize（旧库备份 → schema 迁移/校验 → CreateTables）→ DefaultDataInitializer（默认文件校验与事务创建）→ EmotionMemory.Initialize。异常设置 StartupError，登录页与 AuthService 都拦截。

登录：启动成功 → UserName 查用户 → PasswordHasher 验证 → 用户解析到 UserId 后查 CharacterName → 更新登录时间/设激活 → GlobalSession.SetSession → 关系初始化。登录时间、激活、关系并非整个登录一次统一事务。

SessionSnapshot 捕获 ID、名称、权限、版本。IsCurrent 比较登录有效性、UserId、CharacterId 和版本。SetSession 即使同一用户/角色重新登录也推进版本；Clear、当前角色 ID/名称变化、当前用户名/权限变化也可失效。GlobalSession.Current* 已私有，新增异步入口只能显式传快照；同步兼容重载仍存在，不作为异步边界。

会话变化后 ConversationService 清队列并取消活动 Turn，AIChat 中止旧 HTTP/清缓存，UIManager 重载新角色历史，BubbleUIManager 隐藏旧气泡。取消旧请求与提交前 IsCurrent 都需要，不能只做其中一个。

## 普通聊天：顺序、身份与提交边界

1. UIManager 调 TrySend，创建不可变 ChatTurn 并入 FIFO，UI 立即显示暂存用户气泡。
2. 队列先 yield 一帧供 UI 登记；轮到 Turn 时校验快照，ChatMessageService 保存用户消息并事务裁剪到 100 条。
3. 保存成功才发 TurnStarted；之后执行发送关系规则。排队的下一条尚未落库，不会成为前一条的“未来历史”。
4. AIChat 先查会话搜索缓存，否则规则判不搜索/直接搜索/模型决策；需要时调 Bocha。缓存最多 5 条、15 分钟，按 UserId/CharacterId/SessionVersion 匹配。
5. CharacterPromptLoader 按快照 CharacterId 读角色 JSON；Builder 组合设定、时间、最近历史、关系和情绪。ChatContextBuilder 取最近 8 条，排除本轮 UserMessageId，再追加当前输入一次。
6. 每次 HTTP 由 ExecuteRequest 处理单请求超时、HTTP/网络、Abort 和 Dispose；普通回复转为 ChatReplyResult，成功 HTTP 后解析内容。
7. ConversationService 再检查会话/结果；成功且非空才存 Assistant，执行回复关系规则并发 AssistantReplyReady；失败发 TurnFailed，取消发 TurnCancelled。

用户消息在模型失败/取消后可留在旧角色历史。尚未持久化气泡取消/保存失败时删除；UI 的“回复失败，请重试”是临时显示，不写 Assistant。该输入的关系效果发生在请求之前，失败不会自动回滚此前发送关系。

用户消息、Assistant 消息、关系更新、UI 事件不是跨网络的大事务。“三态终结”已有普通路径实现，但仍有 E04 所述异常边界缺口，不等于所有异常都已保证一个终态。

## 主动气泡与普通聊天的区别

链路为 DesktopContextManager → AIContextReactionManager → AIChat.GetAIBubbleReply → BubbleUIManager / InteractionEventService。默认每秒检查、同标题停留 5 秒后触发；全局冷却默认 3 秒，同上下文近期已显示气泡冷却 10 分钟。实际场景序列化值可能覆盖字段默认值。

前台上下文传入请求时快照、标题与进程，不在协程后重新获取当前窗口作为旧请求参数。返回后再次检查快照和组件状态；包含 [IGNORE] 则记录忽略而不显示。停用组件取消气泡请求并清锁。

它是独立单次请求链：不建立 ChatTurn、不进普通 FIFO、不保存到 ChatMessage，而是写 InteractionEvent。GetAIBubbleReply 仍是 Action<string>，忙或会话失效可不回调；普通聊天的结构化终态契约不能直接套用。两种请求可以同时存在，共同在会话变化时失效。

## 数据持久化与隔离

| 业务表 | 稳定归属/用途 | 当前边界 |
| --- | --- | --- |
| User | UserId，账号/权限/密码 | 当前密码为无盐 SHA256，开发默认管理员仍存在 |
| CharacterProfile | CharacterId + UserId，名称/Prompt/启用 | UserName 为兼容镜像；角色查询过滤现存用户归属 |
| UserCharacterState | UserId + CharacterId，关系/信任 | 组合状态 ID；事件与关系变化不是统一事务 |
| ChatMessage | UserId + CharacterId，消息 ID/发送方/内容 | SaveMessage 会话校验，插入/100 条裁剪事务 |
| InteractionEvent | UserId + CharacterId，事件/上下文/描述 | 最多 300 条；描述与键可含前台原文 |
| EmotionState | 字段为 UserId + CharacterId | **现有情绪调用传名称，见 E01；不能宣称全部归属已统一** |

schema v1 迁移增加/回填 CharacterProfile.UserId，并事务校验/提交版本。升级前备份旧库，WAL 主文件简单复制被拒绝。LegacyUnclaimedCharacter 辅助表记录无法认领的角色 ID，原角色及子记录留在原表，不给默认管理员。角色列表/按 ID 查询通过现存用户归属过滤；无用户的名称查询立即返回空；编辑与删除不能访问隔离角色。新库或已有全有效 v1 库可以没有辅助表。

用户改名更新 User 与角色展示镜像，并失效当前会话；用户/角色删除经授权后事务级联聊天、关系、情绪、事件和主记录。这只覆盖实际以稳定 ID 写入的数据；E01 名称键情绪不在该保证内。隔离数据认领尚无 UI/API，必须单独设计事务和副本验收。

## Windows、日志与资源边界

窗口表现 → IWindowService → WindowsWindowService → Native DTO/DP_* ABI → DesktopPet.Native.Windows.dll。业务使用 WindowLogicalSize 和物理 WindowRect/WindowMonitorInfo，不读 HWND/Win32 常量。前台标题/进程由 WindowsForegroundContextService 提供，user32 导入留在 Native。

原生 ABI 要求主版本 1 与所需能力位；已有自检成功/失败诊断。Importer 明确 Win64/Windows Editor x64 启用、Linux/Mac/Win32 禁用，但发布仍要在 Unity Importer 与实际输出核对，不从 DLL 文件存在推断跨平台兼容。仓库未发现原生 C/C++ 源码和可复现构建说明。

AppLogService 仅保证自定义 m0_diagnostics.log 的指纹和约 2 MB 轮转；Unity 常规日志、网络 payload、事件数据库是不同边界。真实 config.json 只留本地。UI/Unity 对象操作保持主线程。

## 已发现缺口与下一步证据

| ID | 源码证据/触发 | 影响 | 修复定位与验收 |
| --- | --- | --- | --- |
| E01 情绪名称键 | AIChat.AIPrompt/AIBubblePrompt 传 session.UserName/CharacterName；EmotionMemory.Initialize/ResetEmotion 及 SampleScene 的 EmotionBuildDebugTest 用默认名称常量 | 情绪归属与稳定 ID 分裂；改名失联，级联可能残留；F9–F12 可修改运行库 | AIChat、EmotionMemory、SQLiteEmotionStorage、调试组件；定义登录前初始化/历史映射歧义与隔离；副本测改名、切角色、级联，不直接改真实库 |
| E02 前台原文边界 | AIContextReactionManager Debug.Log 标题；InteractionEventService 将标题/回复入键/描述；主动气泡将前台上下文发网络 | 自定义日志脱敏不代表全系统隐私保证 | 逐项确定 Unity 日志、事件保留、网络发送策略；本轮只记录事实 |
| E03 账号安全 | DefaultDataInitializer 创建固定 Admin；LoginPanel 预填；PasswordHasher 无盐 SHA256 | 公开发布身份验证不足 | 用户已选择默认管理员后续阶段处理；密码升级需旧库兼容与备份 |
| E04 成功提交后异常 | ConversationService 保存 Assistant 后，OnAssistantReplyFinished 未被异常边界包裹 | 关系写入抛异常时队列协程退出，回复已入库但没有成功 UI/终态；队列等待项也可能暂时停住 | 定义保存成功/关系失败的产品语义，再包围各提交阶段；用故障注入验证终态一次和下一 Turn 继续 |

另需运行验收：真实请求体输入去重、连续输入/换角色/退出、HTTP/超时/断网、停用/销毁锁释放、布局失败恢复、多屏/DPI/透明/穿透、日志写盘失败。12 项 EditMode 不覆盖这些全部场景。2026-10-01 用户确认原库迁移/登录成功；本轮不重跑 Unity，也不读取真实库或日志。

## 待新增的世界/资源结构

常驻桌面壳 + Additive 世界是建议；IWorldService、IResourceService、WorldScope、ActionQueue、worldInstanceId 均未实现。App/Session/World/View 应分别管理配置/数据、身份请求、场景交互、界面订阅；不建没有消费者的通用 EventBus 或巨型 IDataService。

进入世界需保存窗口/布局状态、暂停桌面气泡并定义活动聊天策略、保证单输入/相机/AudioListener；加载失败、取消和退出都恢复。世界失效与会话失效不同，未来结果需相应边界。

SakuramachiSceneLoop 独占列车/杆/灯。当前时序为 0/3/5/10/20/23/25/35/45/180 秒；欢迎只写角色/相机且跳过不改列车时间。Timeline.stopped 不是剧情成功事件；阶段事件需新增，定义时间跳跃和回卷。现有音轨、movementLoop、A 材质与樱花应复用。具体拆法见 [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)。
