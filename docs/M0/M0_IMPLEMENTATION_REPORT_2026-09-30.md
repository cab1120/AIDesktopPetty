# M0 修改报告：会话、请求终态、数据库与平台边界

> 2026-10-05 导航更新：本文件保留 M0 历史事实。用户已完成并验收 M1，当前世界实现与内存记录见 [M1 报告](../M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md)。M1 通过不代表本文件所列 M0 缺口自动关闭。

> 日志策略后续更新（2026-10-04）：按用户要求，AppLogService 已恢复原 AIChat.RunLog 的本地时间/类型/Debug 原文及错误堆栈，追加到项目根目录或 Player exe 旁的 run_log.txt；以下指纹/轮转描述保留为当时重构记录，不作为当前行为。

> 历史报告说明（2026-10-01）：正文保留 2026-09-30 的改动来源、实现过程和当时验证状态；结果现已包含在 Git 6853f6f。用户今天确认原库已成功迁移并登录，因此正文“实际旧库仍待迁移”是当时状态。最新源码审计另发现情绪名称键、前台原文边界与成功提交后的关系异常，不从本报告推断已解决。当前事实和验收请读 [架构](../ARCHITECTURE.md)、[M0 移交](M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md)、[逐文件脚本](../SCRIPTS.md)与 [实现指南](../IMPLEMENTATION_GUIDE.md)。

核对日期：2026-09-30。本文给开发者和后续 AI 共同使用。证据以本仓库源码、Git `8473655` 和本次工作区改动为准。计划 PDF 是需求来源，具体实现与验证状态以本文及 `M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md` 为准。

## 一、要解决的问题

旧聊天路径在协程中可能重新读取可变化的全局身份。A 角色发起请求后切到 B，旧结果可能写入 B 的上下文、关系或气泡。并发输入还可能让 B 条消息提前进入 A 的历史，或让当前输入在请求体里出现两次。数据层用可改名的 `UserName` 关联 `CharacterProfile`，改名会使角色失联。删除用户/角色若只删主表，会留下孤儿数据。Windows 前台感知在 Presentation 中直接 P/Invoke，窗口自检失败诊断也没有实际调用。日志订阅跟着聊天组件，可能重复写入原始敏感内容。

核心模型：一次普通输入生成不可变 `ChatTurn`，其中有 `TurnId`、该用户消息的 `MessageId`、文本和不可变 `SessionSnapshot`。`SessionVersion` 是代数令牌；登录、退出、切角色或当前用户身份资料变化会让旧代失效。每个 Turn 只在轮到自己时保存输入，然后构造模型上下文、请求回复；提交回复前再查 `IsCurrent`。结果区分成功、失败、取消。数据库所有权由稳定 ID 表达。

## 二、改动归属和脚本清单

### A. 之前已经提交：`8473655`

以下是本次开始前已在 Git 中的会话/回合制改动。`README.md`、`docs/ARCHITECTURE.md`、`docs/PROJECT_PLAN.md`、`docs/SCRIPTS.md`、`docs/API_EVENTS.md` 与 `docs/SESSION_REFACTOR_REPORT_2026-09-30.md` 当时也同步过，不能算作本轮新代码。

| 脚本或资源 | 已提交的改动及原因 |
| --- | --- |
| `DataBase/Data/SessionSnapshot.cs` | 固定用户/角色 ID、展示名、角色与会话版本；异步参数有自己的生命周期。 |
| `DataBase/Data/GlobalSession.cs` | 建立/清除会话、切角色时推进版本，提供 `CaptureSnapshot` 和 `IsCurrent`。全局对象只负责当前状态与失效通知。 |
| `Character/Conversation/ChatTurn.cs` | 一个输入一个 `TurnId` 和预先生成的用户消息 ID；排队、日志和请求体可以指向同一轮。 |
| `Character/Conversation/ConversationService.cs` | FIFO 队列集中管理用户消息落库、AI 调用、回复提交和取消；防止后续输入提前进入前一轮历史。 |
| `Character/AI/AIChat/AIChat.cs` | 主聊天/主动气泡接收快照；跟踪活动请求并在会话变更时 Abort；搜索与模型阶段检查旧会话。 |
| `Character/AI/AIChat/ChatContextBuilder.cs` | 组装请求消息时排除本轮已落库的 `UserMessageId`，再追加当前输入一次。 |
| `Character/AI/AIChat/ChatContextTextBuilder.cs` | 搜索决策文本也排除本轮消息，避免当前输入在决策上下文中重复。 |
| `Character/AI/AIChat/SearchDecisonMode/SearchCacheEntry.cs`、`SearchCacheService.cs` | 搜索缓存按用户 ID、角色 ID、版本隔离，切会话清空，避免旧角色搜索结果混用。 |
| `Character/AI/AutoTalk/AIContextReactionManager.cs` | 捕获触发时快照和窗口上下文；旧主动气泡不得显示或写事件。 |
| `Character/AI/Prompt/CharacterPromptBuilder.cs`、`CharacterPromptLoader.cs` | 用快照角色读取和构建 Prompt，避免请求中途换角色后拿到新角色设定。 |
| `Character/UI/BubbleUIManager.cs` | 气泡显示与主动请求的会话版本绑定，切会话隐藏旧气泡。 |
| `Character/UI/UIManager.cs` | 输入经 `ConversationService.TrySend`；排队气泡由 `TurnId` 跟踪，切角色重建可见历史。 |
| `Character/UI/ControlPanel(UserCharactor)/CharacterManagePanel/CharacterManagePanelController.cs`、`CharacterModifyPanelController.cs` | 管理界面的当前用户/角色读取改用快照，防止直接依赖公开全局属性。 |
| `DataBase/AuthService.cs` | 登录后建立用户与角色会话并刷新当前角色。 |
| `DataBase/Data/Character/CharacterRepository.cs` | 原有管理查询和切角色接入快照判定，但当时所有权仍基于 `UserName`。 |
| `DataBase/Data/ChatMessage/ChatMessageRepository.cs` | 按用户/角色 ID 检索和排除指定用户消息 ID。 |
| `DataBase/Data/ChatMessage/ChatMessageService.cs` | 保存消息接收快照，落库前检查 `IsCurrent`。 |
| `DataBase/Data/InteractionEvent/InteractionEventService.cs` | 主动事件写入携带触发时身份；过期气泡不得写事件。 |
| `DataBase/Data/UserCharacterState/RelationshipService.cs` | 关系状态计算接收快照；旧角色回复不增加新角色关系值。 |
| `DataBase/Data/UserData/UserRepository.cs` | 删除当前用户的保护改为比较稳定 `UserId`。 |
| `Assets/Scenes/SampleScene.unity` | 场景把 `ConversationService` 接入聊天 UI 与 AIChat；此引用是实际运行链入口。 |

### B. 你在本次开始前尚未提交的改动

| 文件 | 已有内容与本轮保留方式 |
| --- | --- |
| `Character/AI/AIChat/ChatReplyResult.cs` 及 `.meta` | 增加 Success/Failure/Cancelled 终态和 Timeout、Network、HttpError、InvalidResponse、EmptyResponse、SearchDecisionFailed、SearchFailed 等失败原因；本轮保留该协议。 |
| `Character/AI/AIChat/AIChat.cs` | `RequestExecutionResult` 分类网络请求；手动超时、Abort/Dispose；空内容和无效 JSON 转失败；本轮在此基础上接入可配置超时/端点/模型并处理请求启动异常。 |
| `Character/AI/AIChat/SearchDecison/SearchDecisionService.cs` | 无效搜索决策不再静默当作“不搜索”，让上层能给出 SearchDecisionFailed。 |
| `Character/Conversation/ConversationService.cs` | 成功、失败、取消分别完成，失败不再保存正常 Assistant 回复；本轮修复 UI 登记气泡与写库事件时序。 |

### C. 本轮补齐的文件

| 文件 | 本轮修改及“为什么” |
| --- | --- |
| `Character/Conversation/ConversationService.cs` | 队列首帧让 UI 先登记 `TurnId`；用户消息保存成功后才通知 `TurnStarted`；关系写入异常走失败终态，后续 Turn 可以继续。 |
| `Character/UI/UIManager.cs` | 若用户消息在真正落库前失败，移除待执行气泡并给出未落库的失败提示；避免 UI 显示不存在的历史。 |
| `DataBase/Data/ChatMessage/ChatMessageService.cs` | 消息插入与历史修剪在一个事务中；提交前再次核对快照，数据库失败返回 `null` 而不是把异常留在队列中。 |
| `Character/AI/AIChat/AIChat.cs` | 移除组件级日志订阅；读取本地端点、模型、超时，超时限制在 1–120 秒；配置缺失以失败结果终结 Turn；`SendWebRequest` 启动异常也回调失败并释放请求。 |
| `Character/AI/AIChat/ChatReplyParser.cs` | 把模型 JSON 转为 Success、EmptyResponse 或 InvalidResponse；错误体和空文本不再可能被当作正常 Assistant 内容。 |
| `Assets/StreamingAssets/config.example.json` | 给出配置字段示例，真实 `config.json` 未改动、未纳入 Git。 |
| `DataBase/AppLogService.cs`、`AppInitializer.cs` | 应用级单次日志订阅与停止；只记录时间、类型和消息指纹并轮转，不写原始聊天/窗口标题/密钥。初始化失败保留明确错误。 |
| `DataBase/DefaultDataInitializer.cs` | 先验证默认 Prompt 存在且非空，再在事务内建立默认用户和角色；失败不留下半个默认账号。 |
| `Character/UI/Login/LoginPanelController.cs`、`DataBase/AuthService.cs` | 初始化失败在登录页显示并阻断登录，避免半初始化后继续读写数据库。 |
| `DataBase/DatabaseSchemaMigrator.cs`、`DatabaseManager.cs` | 旧库先备份，`PRAGMA user_version=1` 管理迁移；事务增加并回填可证明归属的 `CharacterProfile.UserId`。无法映射的角色原样保留，由 `LegacyUnclaimedCharacter` 登记 ID，版本 1 校验要求每个无效归属都有且只有对应隔离标记；新库直接建表。拒绝不完整 WAL 主文件备份。最初采用“任何孤儿都阻断”的策略，实际妨碍原账号登录，后按用户新决定改为隔离。 |
| `DataBase/Data/Character/CharacterProfileData.cs` | 增加稳定 `UserId` 索引字段。旧库的物理列因 SQLite `ALTER TABLE` 兼容性保持可空；隔离的历史角色可缺少有效归属，正常仓储写入必须有现存用户所有权。 |
| `DataBase/Data/Character/CharacterRepository.cs` | 查询归属先把 `UserName` 解析为 `UserId`，再以 ID 查询；用户名不存在时立即返回空，避免空 ID 匹配隔离角色。全局列表/按 ID 查询只返回有现存用户归属的角色，直接编辑隔离角色也被拒绝。角色新增、编辑、切换在事务中维护唯一激活；修复编辑当前角色误报“不能删除”；经授权后删除角色事务清理聊天、关系、情绪、事件，当前登录角色需先切换。 |
| `DataBase/Data/UserData/UserRepository.cs` | 新增用户与默认角色同一事务；改名同时更新角色资料的展示名，归属 ID 保持不变；经授权后删除用户事务清理六张表，触发器/SQL 异常时整体回滚并返回错误。 |
| `DataBase/Data/GlobalSession.cs` | 原 `Current*`、`SessionVersion` 和 `IsLoggedIn` 读取属性收为私有，外部只能捕获快照；角色所有权比较 `UserId`；当前用户身份资料或当前角色名称变化后推进版本，使旧快照失效。 |
| `Platform/Windows/Native/WindowsForegroundNativeMethods.cs`、`Platform/Windows/WindowsForegroundContextService.cs`、`Presentation/DesktopContextManager.cs` | Win32 前台句柄和进程查询搬入 Platform；Presentation 只接收标题和进程名组成的结果，停止记录原始标题日志。 |
| `Platform/Windows/WindowsPlatformBootstrap.cs`、`Assets/Plugins/x64/DesktopPet.Native.Windows.dll.meta` | 自检成功/失败都记录诊断；插件仅给 Windows x64 Player 和 Windows Editor 导入，排除 Linux/Mac/Win32。 |
| `Assets/Scripts/Editor/M0DatabaseTests.cs` | 合成旧库、孤儿隔离及隐藏、无效用户名不得读到隔离角色、改名、用户级联及注入故障回滚、角色级联/当前角色保护，以及模型返回格式的成功/空内容/坏 JSON；临时文件，不接触运行数据库。 |
| `AGENTS.md`、本报告和 M0 移交文档 | 将代码不变量与验证状态写成未来开发可读的约束。 |

## 三、面试时要能解释的因果链

1. **为何快照还要版本？** 快照保证参数在协程中不变；版本保证“原来正确的参数”在切角色后已失效。仅比较用户名不够，名字可以改；比较稳定 ID 加版本才能辨别重新登录同一角色形成的新会话。
2. **为何队列在执行时才写用户消息？** 假设快速发 A、B，A 的模型请求需要读取最近历史。若 B 先落库，A 会看到未来输入。队列让顺序成为 A 用户消息、A 终态、B 用户消息、B 终态。
3. **为何有 `UserMessageId`？** 用户消息已为历史检索持久化，但当前请求体还必须把输入放入最后一条 user 消息；按该 ID 排除历史中的本轮消息，便不会重复。`TurnId` 标识业务轮次，不能与数据库消息 ID 混用。
4. **为何失败是结果而不是一段错误文字？** 若把 HTTP/解析错误当回复存入 Assistant 历史，后续上下文、关系值和 UI 都会误判。终态类型让保存和关系更新只接受非空成功结果；取消属于会话失效，语义不同于网络故障。
5. **为何 UI 气泡和数据库分两步？** 立即显示提供输入反馈，但排队消息尚未执行；因此 UI 必须按 `TurnId` 管理暂态气泡。写库成功再转正，失败/切会话时删除，历史重载只读数据库事实。
6. **为何迁移用 `UserId` 且先备份？** `UserName` 允许编辑，不能当外键。能证明归属的旧角色回填稳定 ID；不能证明的角色保留原记录并登记隔离标记，所有查询必须排除它，不能猜测归属。备份是跨版本恢复点，事务保证角色回填、标记和版本号一起提交或回滚。
7. **为何删除需要事务？** 用户/角色对应多张表；第 3 张表删除失败时，前两张若已提交就会留下破碎状态。`RunInTransaction` 让成功全成、失败全不成；触发器注入故障是回归证据。
8. **为何 Windows 接口隔离？** UI 不应该知道 HWND 或直接调用 user32；平台实现集中处理 Windows-only 编译和失败降级，未来替换前台感知或跨平台时不会改业务 UI。插件导入目标也要与原生二进制架构匹配。
9. **为何日志由应用管理？** Chat 组件可能销毁/重建，组件级订阅容易重复；日志原文还可能含提示词、聊天或窗口标题。单次订阅、轮转、内容指纹和生命周期清理降低暴露面。

### 用变化场景检验是否真正理解

现有开发者知识档案把“协程请求终结与取消”“身份/授权边界”“SQLite 事务和版本迁移”“Windows ABI 验证”列为待验证知识，不能仅因本次代码出现就推断已经掌握。面试时应能独立解释下面的变体：

| 追问 | 应答重点 |
| --- | --- |
| A、B 连续发送；A 正在搜索时又切到另一个角色，哪些记录能留下？ | A 若已执行，用户消息可留在旧角色；B 尚未执行则不落库且气泡删除；A 的搜索/回复取消，旧 Assistant/关系/气泡事件不得落到新角色。按 TurnId 和快照逐边界推导，不能只说“加锁”。 |
| HTTP 500 后错误体恰好长得像模型 JSON，能否保存为回复？ | 先判传输状态为 HttpError，成功状态才交给 `ChatReplyParser`；不能仅靠“JSON 能解析”判业务成功。完成失败终态、释放请求，队列继续。 |
| 旧库某角色的 UserName 找不到用户，是否可以给默认管理员？ | 不可以。保留原角色及关联记录，以 `LegacyUnclaimedCharacter` 标记；正常角色继续迁移并可登录。全局列表和按 ID 查询必须排除隔离角色。之后若有可信证据确认归属，应设计专门的认领事务与回归测试。 |
| 删除用户时 ChatMessage 已删，EmotionState 上触发器报错，会怎样？ | `RunInTransaction` 回滚先前的删除，六张表保持原样；仓储给调用者失败结果，UI 不应刷新成“已删除”。这正是注入故障用例覆盖的场景。 |
| 当前角色只改了显示名，UserId/CharacterId 都没变，旧请求还有效吗？ | 当前角色名属于快照中的 Prompt/历史展示参数，因此刷新角色时推进 SessionVersion。旧请求携带旧版本，不允许提交。 |
| Windows x64 构建成功但隐藏启动自检报 `Internal/1168`，能否宣称平台已修复？ | 不能。隐藏进程主窗口句柄为 0，原生 API 无目标窗口；这只能证明失败诊断路径运行。需要可见 Player、自检成功、窗口行为和多屏/DPI 实测。 |

## 四、验证与尚存风险

- C# 运行时代码：使用临时工程文件包含新脚本、临时输出目录执行 `dotnet msbuild`，编译通过；这不是 Unity Editor 或 Player 证明。
- Unity EditMode：隔离 worktree 的最终 12/12 项通过（9 项数据库/会话，3 项模型响应协议）；使用临时数据库，没有接触真实运行库。最终 Windows x64 Release Player 构建成功（0 错误、1 警告）。
- Windows x64 Player：隔离 worktree 的 Release 构建成功（0 错误、1 警告）。隐藏窗口测试的 `MainWindowHandle=0`，原生自检返回 Internal/1168；随后可见 Development Player（临时数据重定向）取得有效句柄，原生 API 1.0.0、所需能力位和 DPI=96 主显示器自检通过。用户反馈“其他功能都正常”，但未逐项记录多屏/DPI/主动气泡与失败注入。真实旧数据库仍存在，只读检查后没有对其执行更新或迁移。没有真实密钥时，联网聊天只能验证缺配置失败路径。
- 旧库副本迁移：只读检查显示原库是版本 0，含 1 个无现存用户归属的角色，关联 1 条关系和 2 条事件，记录里的历史 UserId 也已不存在。最初“遇孤儿回滚”的实现使正常账号无法登录；用户随后同意保留孤儿并隔离。新 C# 迁移器在另一份完整匿名副本上升到版本 1：4 个角色均保留，其中 3 个有可用所有权、1 个进入隔离标记；关系状态 4 条和事件 311 条数量不变。该验证没有更改实际运行数据库，实际旧库迁移仍待新构建首次运行。
- 默认管理员初始密码仍为 `123456`，登录页仍预填。用户明确选择保留并列入后续安全阶段；公开发布前必须解决。
- 旧库孤儿仍待以原始业务证据人工确认，当前隔离策略仅允许其他正常角色继续使用，不代表已修复孤儿。不能凭角色名称或已失效 UserId 猜测归属。`CharacterProfile.UserId` 在旧库物理层可空，未来若允许外部 SQL 写入，必须保留隔离不变量或重建约束。
- SQLite 迁移备份只支持没有活跃 WAL 的旧库；检测到 `-wal` 即拒绝迁移，不能把单个主文件误称为完整备份。
- 已保存的旧用户消息在模型请求取消后仍归旧角色历史；只有尚未执行、未落库的气泡被移除。这是用户确认的语义。

## 五、交给下一位 AI 的检查顺序

先读 `AGENTS.md` 与 `M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md`，再查本报告涉及的源码。修改普通聊天时沿 `UIManager → ConversationService → AIChat → ChatMessageService/RelationshipService` 追踪 `ChatTurn`。修改角色/用户管理时沿 `AuthService → GlobalSession → CharacterRepository/UserRepository → DatabaseSchemaMigrator` 追踪稳定 ID 与版本。每次改动都检查旧请求是否可能显示/落库、新输入是否恰好一次、失败是否释放队列、迁移和删除是否在副本中回滚。不要把历史报告中的“待验证”改成“已通过”，除非有本次构建和运行证据。
