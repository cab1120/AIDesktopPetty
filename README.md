# AIDesktopPetty · AI 桌面陪伴与樱町站台

这是一个 Unity Windows 桌面 AI 陪伴项目。当前可运行入口提供登录、角色管理、聊天、历史记录和基于前台窗口的主动气泡；可从桌面进入樱町 3D 站台并返回。求职作品主线是 **桌宠 → 站台 → 交互 → 返回桌宠**；M1 已接通世界进入/退出与失败恢复，欢迎演出和业务交互仍需后续开发。

核对日期：2026-10-05；代码基线为 Git fbe7f08。**M1 已通过用户最终验收**，20 次 World 往返的结构计数恢复基线；仍记录可回收资源造成的内存高水位增长。详见 [M1 实现与验收报告](docs/M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md)。本次只更新文档，没有重新运行 Unity 或读取实际数据库/日志。M0 历史验收及已知缺口继续保留。

## 阅读入口

| 读者/任务 | 建议顺序 |
| --- | --- |
| 了解项目和运行方式 | 本 README → [架构与当前缺口](docs/ARCHITECTURE.md) |
| 找到脚本及其职责 | [逐文件脚本索引](docs/SCRIPTS.md)，覆盖项目自有 C# 脚本及 SQLite 兼容层 |
| 修改世界、资源或站台；理解 M1 验收 | [M1 实现与验收报告](docs/M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md) → 架构与接口 → 源码 |
| 修改聊天、账户数据或窗口 | [M0 不变量与验收状态](docs/M0/M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md) → [接口与事件](docs/API_EVENTS.md) → 源码 |
| 将原计划拆为实现任务 | 本 README + 原计划 → [实现定位与拆任务指南](docs/IMPLEMENTATION_GUIDE.md) |
| 理解重构原因、准备面试 | [逐脚本重构报告](docs/M0/M0_IMPLEMENTATION_REPORT_2026-09-30.md)；[第一阶段报告](docs/M0/SESSION_REFACTOR_REPORT_2026-09-30.md)是历史记录 |

[原计划 PDF](output/pdf/AI_Desktop_Companion_Updated_Plan_2026-09-21.pdf)本轮不更新。原 docs/PROJECT_PLAN.md 已在用户提交中移除，当前任务定位由实现指南承接。遇到其中的旧接口、旧列车时序或旧验证状态，以当前源码和上述文档核对后的事实为准。AI 协作规则在 [AGENTS.md](AGENTS.md)。

## 已实现内容与边界

| 能力 | 当前实现 | 尚未证明/尚未实现 |
| --- | --- | --- |
| 登录与管理 | 本地密码验证、用户/角色增删改查、权限 UI、角色切换；改名按稳定 UserId 保持角色关联 | 默认管理员和密码策略尚未用于公开发布加固 |
| 普通聊天 | 协程、非流式；SessionSnapshot + ChatTurn；FIFO 队列；成功/失败/取消；当前输入只追加一次 | 无流式输出、自动重试、长期语义记忆；切角色及网络故障还需逐项运行验收 |
| 搜索与 Prompt | SiliconFlow 兼容聊天接口、Bocha 搜索；缓存 → 规则 → 必要时模型判断；角色 Prompt 来自数据库 JSON | 模型权限/可用性依赖本机账号；主动气泡仍用字符串回调 |
| 主动气泡 | 前台标题/进程检测、停留阈值、冷却、过滤、快照与旧结果检查、独立气泡显示 | 不与普通聊天共用队列；前台信息可能进入网络请求、本地事件和 Unity 日志 |
| 本地数据 | SQLite 六张业务表；schema 版本 1；先备份、角色 UserId 回填、孤儿隔离；用户/角色事务级联 | 情绪调用仍以名称填充 ID 参数，属于已发现的归属缺口 |
| Windows 窗口 | IWindowService → WindowsWindowService → Native ABI → x64 DLL；无边框、透明、置顶、拖拽、吸附、穿透和 DPI/显示器查询 | 自检通过不代表全部跨屏/DPI/窗口交互通过 |
| UI 布局 | DesktopPetLayoutController + Profile 统一登录、折叠、聊天、管理布局 | 世界状态由 WorldCoordinator 独立管理；布局模式不是 World 状态 |
| 樱町站台 | 3DScene 引用 180 秒列车/栏杆/六灯循环、Timeline 音频、樱花 VFX、天空旋转、八份 A 版人物材质 | 已支持 Additive 进入/退出、绑定、控制权交接和失败重试；尚无欢迎/POI/AI Action 产品交互 |
| 后续技术 | 当前使用 Unity 协程、内置场景和资源能力 | 未安装 YooAsset、UniTask、Cinemachine、HybridCLR；无自有 asmdef；AI Action、房间、多人是规划 |

## 环境、配置与启动

- 编辑器：**Unity 2021.3.21f1c1**，以 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 为准。
- 渲染：URP / VFX Graph **12.1.10**；Timeline 1.6.4、TMP 3.0.6、Newtonsoft JSON 3.2.2，见 [manifest.json](Packages/manifest.json)。
- 目标：Windows x64。[Build Settings](ProjectSettings/EditorBuildSettings.asset)启用 `Assets/Scenes/SampleScene.unity`（桌面启动壳）和 `Assets/Scenes/3DScene.unity`（Additive 世界）。

1. 用上述编辑器打开项目并打开 [SampleScene](Assets/Scenes/SampleScene.unity)，保留脚本和序列化绑定。
2. 若本机尚无配置，将 [config.example.json](Assets/StreamingAssets/config.example.json)复制为同目录 config.json，填写自己的密钥。已有配置保留，真实配置不进入 Git。
3. 保留 [DefaultCharacterPrompt.json](Assets/StreamingAssets/DefaultCharacterPrompt.json)。启动时验证文件存在且非空；初次创建默认角色时将 JSON 写入库，修改文件不会自动覆盖已存在角色。
4. 首次启动默认账号为 DefaultUser / 123456，角色 DefaultCharacter，权限 Admin。登录页会预填；使用已有库时输入已有账号和角色。这是开发默认行为，用户决定后续安全阶段加固。
5. 登录后通过世界入口进入樱町，返回操作经 WorldCoordinator；WorldCatalog/Definition/Presentation 配置位于 Assets/Config/Worlds，当前 worldId 为 sakuramachi、世界窗口 1280×720。进入前暂停主动气泡并保存桌面表现，退出成功后恢复。
6. Editor 检查 UI/数据；构建 Windows x86_64 Player 检查真正的桌面窗口。Editor 不能替代 Windows Player 原生窗口验证。

| 配置项 | 含义与默认值 |
| --- | --- |
| siliconFlowKey | 普通回复、搜索模型决策和主动气泡的模型密钥；缺失使 AI 请求失败 |
| bochaApiKey | 搜索密钥；缺失时需要搜索的轮次会失败 |
| siliconFlowUrl | 默认 https://api.siliconflow.cn/v1/chat/completions |
| bochaUrl | 默认 https://api.bochaai.com/v1/web-search |
| model | 默认 Pro/deepseek-ai/DeepSeek-V3，可由本地配置覆盖 |
| timeoutSeconds | 单次 HTTP 超时默认 30 秒，有效正值限制到 1–120 秒；一轮可能有多次请求，不是整轮 30 秒 |

缺 config.json 影响 AI 请求；数据库/默认 Prompt 初始化失败才会通过 AppInitializer.StartupError 阻断登录。Windows Player 配置位于输出的 `<程序名>_Data/StreamingAssets`。默认值不证明服务端账号已开通对应模型。

## 普通聊天核心链路（M0 基线）

普通聊天沿以下现有脚本执行：

```mermaid
flowchart LR
  UI[UIManager] -->|TrySend| C[ConversationService / FIFO]
  C --> T[ChatTurn + SessionSnapshot]
  C -->|轮到执行才保存用户输入| DB[ChatMessageService]
  C --> AI[AIChat]
  AI --> S[缓存 / 规则 / 模型判断 / Bocha]
  S --> P[CharacterPromptBuilder + ChatContextBuilder]
  P --> H[模型 HTTP 请求]
  H --> R[ChatReplyResult]
  R --> C
  C -->|有效会话且成功| DB
  C -->|已保存的成功回复| UI
```

SessionSnapshot 固定请求时的用户/角色 ID、名称、权限和会话版本；ChatTurn 再固定 TurnId、用户消息 ID、输入及创建时间。登录、退出、切角色、当前角色改名或当前用户资料变化推进版本，使旧请求失效。UI 先显示待执行气泡，数据库在轮到该 Turn 时保存输入；模型消息排除这条消息，再追加当前输入一次。

失败/取消不写 Assistant 正常消息、不增加回复成功关系值。已保存用户消息留在旧角色历史；未执行气泡取消时移除。切会话清空聊天窗并加载新角色历史。普通聊天生命周期由 ConversationService 持有，新增功能不要从 UI 直接发模型请求或写聊天记录。

主动气泡沿 DesktopContextManager → AIContextReactionManager → AIChat.GetAIBubbleReply → BubbleUIManager / InteractionEventService，使用快照与独立请求锁，**不进入普通聊天 FIFO 或 ChatMessage 历史**。两条链路应分别做取消和生命周期回归。

## 数据库、迁移和日志

运行库为 Application.persistentDataPath/iroha_ai.db，Company/Product 为 Cab/AIDesktopPetty；Windows 通常位于 %USERPROFILE%/AppData/LocalLow/Cab/AIDesktopPetty。构建目录变化不会自动换库，Company/Product 变化则可能改变路径。

| 表 | 用途 |
| --- | --- |
| User | 用户、密码哈希、权限和登录时间 |
| CharacterProfile | 稳定 CharacterId/UserId、展示名称、启用状态和角色 Prompt JSON |
| UserCharacterState | 用户与角色的关系值、信任及互动状态 |
| EmotionState | 情绪历史；当前调用仍有名称/ID 混用，不能视为已统一归属 |
| ChatMessage | 用户与 Assistant 聊天记录，每用户/角色上限 100 条；模型取最近 8 条历史 |
| InteractionEvent | 主动气泡与桌宠操作事件，每用户/角色上限 300 条；可含窗口标题与气泡文本 |

版本 0 旧库升级前在同目录生成 `.before-m0-v1-<UTC时间>.bak`，再事务回填 CharacterProfile.UserId、校验并提交 PRAGMA user_version=1。检测到 WAL 则拒绝简单主文件备份。无法证明归属的角色和关联记录留在原表，以 LegacyUnclaimedCharacter 辅助表标记；正常查询/登录/编辑/删除不开放它们，未来认领需专门证据与事务。该表是旧角色迁移标记，不是所有新库必有的第七张业务表。

日志格式恢复于 2026-10-04：AppLogService 在应用寿命只订阅一次，追加写入 `Application.dataPath/../run_log.txt`。Editor 位于项目根目录；Windows Player 位于 exe 所在目录。格式沿用旧 AIChat.RunLog：`[本地时间] [类型] Debug 原文`，Error/Exception 另附堆栈，不再使用指纹或 2 MB 轮转。已有文件保留并继续追加；旧 m0_diagnostics.log 不再更新。该服务只记录 Unity 日志回调，不包含 Player.log 的全部引擎启动信息。

## 验证状态与当前缺口

- **M1 最终验收通过（2026-10-05 用户反馈）**：20 次 World 往返后 Scene、WorldRuntimeBindings、SakuramachiSceneLoop、Camera、AudioListener、播放 AudioSource 与 StateChanged 订阅数量恢复基线，未观察到结构性生命周期泄漏。
- 已知内存现象：重复加载有 native/asset 高水位增长；诊断清理后 Managed 约 631.8 → 550.0 MB、Unity Allocated 约 865.4 → 285.1 MB，支持主要为可回收 unused assets/native resources 的判断。源码清理菜单同时执行 UnloadUnusedAssets 和 GC，不能归因于单一步骤；也不宣称所有内存完全回到基线。生产退出未自动逐次清理 unused assets。完整口径与限制见 [M1 报告](docs/M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md)。

- 2026-09-30：隔离 Unity EditMode 12/12 通过；Windows x64 Release 构建成功（0 错误、1 警告）；可见 Development Player 原生 API 1.0.0、所需能力位和主显示器 DPI=96 自检通过。
- 旧库匿名副本保留 4 个角色（3 个可用、1 个隔离），关系状态/事件数量未变。2026-10-01 用户确认原库已成功迁移并登录；本次文档工作未读取原库或日志。
- 仍需逐项验证：连续输入与切角色/退出取消、网络超时/HTTP/断网、主动气泡销毁/停用、多屏/DPI和窗口交互。完整矩阵见 [M0 移交](docs/M0/M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md)。这些 M0 证据缺口不因 M1 验收自动关闭。

此前 M0 审计发现：AIChat.AIPrompt/AIBubblePrompt 向 EmotionMemory.GetCurrentEmotion 传入名称，初始化和场景挂载的 EmotionBuildDebugTest 也使用名称常量，可能使改名后的情绪失联、ID 级联无法清理这些记录；调试组件 F9–F12 可读写/删除这类情绪数据。另有前台标题的 Unity 日志、无盐 SHA256 密码、默认管理员，以及成功回复落库后关系更新异常的终态风险。证据与入口见 [架构缺口 E01–E04](docs/ARCHITECTURE.md)。这些是待处理项，本轮没有修改功能代码。

## 世界生命周期与交给其他模型的事实

M1 当前链路：WorldEntryButton → WorldCoordinator → WorldScope + IResourceService（LocalSceneResourceService）→ Additive 3DScene → WorldRuntimeBindings + SakuramachiWorldBindings。WorldPresentationController 保存/恢复 DesktopPresentationSnapshot，交接窗口、相机、AudioListener、输入和环境内容。WorldNavigationPanel 请求返回。

状态为 Desktop → Entering → Explore → Exiting → Desktop。加载中退出为逻辑取消，等待加载返回后释放资源；释放或桌面恢复失败保留 Exiting 与 Scope，可 RequestExit 重试，不能假报 Desktop。WorldScope.InstanceId 区分世界实例，不替代会话 SessionVersion。详见 [架构](docs/ARCHITECTURE.md)、[接口](docs/API_EVENTS.md)和 [逐脚本 M1 报告](docs/M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md)。

IResourceService、WorldScope 已实现；IWorldService、统一 Action 执行器和业务级 OnTrainStop 仍未实现。当前资源后端是 Build Settings 本地场景，未接 YooAsset。

SakuramachiSceneLoop 独占列车/栏杆/灯：0 秒预警、3 秒红灯、5 秒落杆完成、10 秒出洞、20 秒停稳并抬杆、23 秒绿灯、25 秒抬杆完成、35 秒发车、45 秒隐藏、180 秒循环。欢迎演出只控制角色/镜头并消费循环阶段；跳过欢迎不改变列车时间。Timeline 音轨、movementLoop 已有资源，八份 A 材质保留当前配色/纹理，见 [列车说明](Assets/Scripts/Art/Scenes/README.md)和 [材质说明](Assets/Scripts/Shaders/Improved2.0/README.md)。

将本 README 与原计划交给 LLM 时，要求它：

1. 列出计划与当前事实的差异，把已有 SessionSnapshot/ChatTurn/迁移/窗口服务与已验收 M1 世界生命周期作为基线，避免重复建设。
2. 每项任务写清触发、脚本/场景路径、已有接口、待新增接口、数据/寿命归属、取消与失败补偿、验收证据。
3. 将 M0 已知缺口、M1 已知内存现象与下一阶段功能分别列出；复用已完成的世界进入/退出和表现恢复，按依赖拆欢迎、交互、Action，资源后端扩展另行决定。
4. 拟议接口标注“待新增”；改结构前核对引用图，保留 .meta GUID，不先全仓库搬目录或统一 DontDestroyOnLoad。
5. 给出具体实现步骤和仍需用户决定的问题。原计划是方向输入，当前代码是实现证据；本轮更新文档不授权后续功能实施。

即使只提供本 README 与原计划，也可按下面的现有入口定位任务；方法签名和序列化引用仍需源码确认。以下前三个路径前缀为 `Assets/Scripts/`。

| 计划中的工作 | 现有修改入口 | 必须额外设计的内容 |
| --- | --- | --- |
| 聊天、搜索、失败/重试 | Character/Conversation/ConversationService.cs；Character/AI/AIChat 目录的 AIChat.cs、ChatReplyResult.cs、ChatContextBuilder.cs；DataBase/Data/ChatMessage/ChatMessageService.cs | 故障注入、回复保存后的关系异常语义、重试幂等性 |
| 数据、角色与情绪 | DataBase/DatabaseSchemaMigrator.cs、DataBase/Data/Character/CharacterRepository.cs、DataBase/Data/UserData/UserRepository.cs；Character/AI/Prompt/Emotion/EmotionMemory.cs、DataBase/Data/Emotion/SQLiteEmotionStorage.cs | 情绪稳定 ID 与历史映射/隔离；认领事务与旧数据兼容 |
| 进入/退出桌面状态 | Character/UI/Layout/DesktopPetLayoutController.cs、Platform/Windows/IWindowService.cs、Presentation/Window/*、Character/AI/AutoTalk/AIContextReactionManager.cs | 复用 Application/World 下协调器、表现快照与恢复；新功能明确普通聊天策略和世界异步失效 |
| 站台与欢迎 | Assets/Scenes/3DScene.unity；Assets/Scripts/Art/Scenes 目录的 SakuramachiSceneLoop.cs、SakuramachiLoopTrack.cs、SakuramachiLoopClip.cs | 加载入口已接通；阶段消费接口、角色/镜头欢迎和可取消交互仍待实现 |
| 资源、材质、VFX | Assets/Dev/SakuraWeather/Runtime/Scripts、Assets/Scripts/Shaders/Improved2.0、当前场景声源/音轨 | 复用 IResourceService/WorldScope 所有权；引入新后端前测失败补偿、内存与闭环回归 |

完整入口与任务模板在 [IMPLEMENTATION_GUIDE.md](docs/IMPLEMENTATION_GUIDE.md)。保留 Assets/Scripts、Assets/Dev/SakuraWeather 和实际 UI 预制体目录 **Assets/Prefab**。

## 授权与素材

当前根目录未发现 LICENSE。旧 README 的 MIT 字样不能证明代码及模型、贴图、动作、音频均可再分发；公开发布前补齐代码许可和素材来源清单。
