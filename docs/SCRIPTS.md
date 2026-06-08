# Scripts

本文档逐个说明 `Assets/Scripts` 下项目脚本的职责、关键入口和主要依赖。第三方兼容层 `SQLite.cs` 只做概要说明，不展开其全部 ORM 内部实现。

## Character/AI/AIChat

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `AIChat.cs` | AI 对话总入口，加载 API Key，注册运行日志，执行常规聊天、主动气泡回复、Bocha 搜索、SiliconFlow LLM 请求 | `GetAIReply`、`GetAIBubbleReply` | `UnityWebRequest`、`Newtonsoft.Json`、`SearchDecisionService`、`SearchCacheService`、`CharacterPromptBuilder`、`ChatContextBuilder` |
| `ChatContextBuilder.cs` | 构建发送给 LLM 的 messages 数组，包含 system、最近历史消息、当前用户输入 | `BuildMessages` | `ChatMessageRepository`、`GlobalSession` |
| `ChatContextTextBuilder.cs` | 将最近聊天记录压缩成文本，供搜索决策 Prompt 使用 | `BuildRecentContextText` | `ChatMessageRepository` |
| `SearchResultFormatter.cs` | 根据搜索决策和搜索结果构造 Prompt 中的实时信息块 | `FormatForPrompt` | `SearchDecision` |
| `SearchDecision.cs` | 搜索决策数据结构，记录是否搜索、搜索词和原因 | `NeedSearch`、`Query`、`Reason` | 无 |
| `SearchDecisionService.cs` | 搜索决策服务，先执行规则判断，必要时调用低温度 LLM 输出 JSON 决策 | `Decide` | `SearchRuleFilter`、`SearchDecisionMode`、回调式 LLM 调用 |
| `SearchCacheEntry.cs` | 搜索缓存数据结构，保存查询、结果、原因和创建时间 | `CreatedAt` | `DateTime` |
| `SearchCacheService.cs` | 内存级搜索缓存，缓存最近搜索结果并根据追问相关性复用 | `TryGetRecent`、`Add` | `SearchCacheEntry` |
| `SearchDecisionMode.cs` | 搜索规则判断结果枚举 | 枚举值 | `SearchRuleFilter` |
| `SearchRuleFilter.cs` | 基于关键词/规则对用户输入做初步搜索判断 | `JudgeByRule` | `SearchDecisionMode` |

### `AIChat.cs`

`AIChat` 是当前 AI 层最重的脚本。它负责读取 `Application.streamingAssetsPath/config.json`、注册 Unity 日志到 `run_log.txt`、构建常规聊天请求、构建主动气泡请求、调用 Bocha 搜索接口、调用 SiliconFlow Chat Completions，并组织搜索决策和 Prompt 构建流程。

维护注意：如果后续要做后端代理、自动化测试或替换模型服务，优先把网络请求从 `AIChat` 拆成独立客户端类。

### `SearchCacheService.cs`

缓存策略：

- 使用静态 `List<SearchCacheEntry>` 保存在内存中。
- 最大缓存数为 `5`。
- 缓存有效期为 `15` 分钟。
- `Add` 会去重同 query 的旧缓存。
- `TryGetRecent` 会先清理过期缓存，再用规范化文本做相关性匹配。

适合缓存“用户围绕同一个热点继续追问”的搜索结果。不适合长期记忆，也不会跨应用重启保留。

## Character/AI/AutoTalk

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `AIContextReactionManager.cs` | 接收桌面窗口变化事件，控制主动气泡触发、冷却、忽略和事件记录 | `OnWindowChanged`、`OnReactionGenerated` | `DesktopContextManager`、`AIChat`、`BubbleUIManager`、`InteractionEventService` |
| `ContextEvaluator.cs` | 判断窗口标题和进程是否值得主动搭话，过滤系统进程和无意义标题 | `IsInteresting` | 进程黑名单、标题黑名单 |

## Character/AI/Prompt

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `CharacterPromptBuilder.cs` | 统一构建常规聊天 Prompt 和主动气泡 Prompt | `BuildChatPrompt`、`BuildBubblePrompt` | `CharacterPromptLoader`、`IrohaPromptBuilder` |
| `CharacterPromptLoader.cs` | 从当前角色数据中加载 Prompt JSON 并反序列化为 `CharacterPromptProfile` | `LoadCurrentProfile` | `GlobalSession`、`CharacterRepository` |
| `CharacterPromptProfile.cs` | 角色 Prompt JSON 的数据结构 | 字段 | 无 |
| `PromptContext.cs` | Prompt 构建时的动态上下文 | 字段 | `EmotionData` |

## Character/AI/Prompt/Emotion

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `EmotionData.cs` | 运行时情绪数据结构，包含当前情绪、强度、持续时间、更新时间 | `GetLastUpdateTime`、`SetLastUpdate` | `EmotionType` |
| `EmotionGenerator.cs` | 生成新的情绪数据 | `GenerateEmotion` | `EmotionType` |
| `EmotionMemory.cs` | 情绪系统门面，初始化存储、读取当前情绪、生成新情绪、重置情绪 | `Initialize`、`GetCurrentEmotion`、`SetEmotion`、`GenerateNewEmotion`、`ResetEmotion` | `SQLiteEmotionStorage`、`EmotionData` |
| `EmotionType.cs` | 情绪类型枚举 | 枚举值 | `EmotionData`、`EmotionGenerator` |
| `IrohaEmotionPromptBuilder.cs` | 将当前情绪转为 Prompt 文本 | `Build` | `EmotionData` |
| `IrohaProhibitedItems.cs` | 默认角色禁止项 Prompt 片段 | `Build` | 无 |
| `IrohaStatusContext.cs` | 构建长期状态或身体状态上下文 | `LongTermMood`、`GetPhysicalStrength` | 时间/规则 |

## Character/AI/Prompt/IrohaPrompt

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `IrohaBubblePrompt.cs` | 默认角色主动气泡规则片段 | `Build` | 无 |
| `IrohaCorePersonality.cs` | 默认角色核心人格片段 | `Build` | 无 |
| `IrohaMemoryContext.cs` | 将用户记忆/最近上下文转为 Prompt 片段 | `Build` | 无 |
| `IrohaPromptBuilder.cs` | 默认角色完整 Prompt 组合器 | `Build`、`BubbleBuild` | 多个 Iroha Prompt 片段 |
| `IrohaPromptJsonExporter.cs` | 将代码内默认 Prompt 导出为 JSON 文件 | `Export` | `CharacterPromptProfile`、文件 IO |
| `IrohaRealtimeContext.cs` | 将搜索结果和当前时间转为实时信息 Prompt | `BuildForBubble`、`BuildForChat` | 搜索结果文本 |
| `IrohaWorldView.cs` | 默认角色世界观片段 | `Build` | 无 |

## Character/UI

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `BubbleUIManager.cs` | 管理主动气泡显示、隐藏和布局刷新 | `ShowBubble`、`SetBubbleText` | TextMeshPro、UGUI Layout |
| `MessageUI.cs` | 单条聊天气泡的文本绑定组件 | `messageText` | TextMeshPro |
| `PetToggleUI.cs` | 控制宠物图标和聊天面板展开/收起，记录展开/收起事件 | `ToggleUI`、`ToggleButton`、`ExitButton` | `WindowSizeController`、`RelationshipService`、`InteractionEventService` |
| `UIManager.cs` | 聊天面板控制器，处理输入、创建气泡、保存消息、调用 AI | `OnInputEndEdit`、`OnSendButtonClick` | `AIChat`、`ChatMessageService`、`RelationshipService` |
| `CustomButtonClicker.cs` | 自定义按钮点击和悬停/按下视觉状态，支持 Inspector 绑定 `UnityEvent` | `PerformClick`、`OnPointerEnter`、`OnPointerExit` | `UnityEvent`、`Image`、`PointerEventData` |

## Character/UI/ChatHistory

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `ChatHistoryItemUI.cs` | 聊天历史列表项，显示发送者、内容、时间、用户、角色，并支持选择状态 | `Init`、`OnClick` | `ChatMessageData`、TextMeshPro、Image |
| `ChatHistoryPanel.cs` | 聊天历史面板，支持打开、关闭、加载最近、条件搜索、清空搜索、删除选中 | `Open`、`Close`、`LoadRecent`、`OnSearchButtonClick`、`OnDeleteSelectedButtonClick` | `ChatMessageService`、`ChatMessageSearchCondition` |

## Character/UI/ControlPanel(UserCharactor)

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `ControlPanelController.cs` | 控制面板入口，切换用户管理、角色管理和宠物面板 | `OnClickUserManage`、`OnClickCharacterManage`、`OnClickBack` | `WindowSizeController` |
| `DesktopPetPanelController.cs` | 桌宠面板进入控制面板入口，根据权限显示管理入口 | `OnClickOpenControlPanel` | `GlobalSession`、控制面板对象 |

## UserManagePanel

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `UserListItem.cs` | 用户列表项，显示用户信息并通知控制器选中用户 | `Init` | `UserData`、`UserManagePanelController` |
| `UserManagePanelController.cs` | 用户管理面板，支持搜索、刷新、选择、新增、修改、删除、返回 | `RefreshList`、`SelectUser`、`OnClickAdd`、`OnClickModify`、`OnClickDelete` | `UserRepository`、`UserModifyPanelController` |
| `UserModifyPanelController.cs` | 用户新增/修改面板，处理用户名、密码、角色输入 | `OpenForAdd`、`OpenForEdit`、`OnClickConfirm` | `UserRepository` |

## CharacterManagePanel

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `CharacterListItem.cs` | 角色列表项，显示角色信息并通知控制器选中角色 | `Init` | `CharacterProfileData`、`CharacterManagePanelController` |
| `CharacterManagePanelController.cs` | 角色管理面板，支持搜索、刷新、选择、新增、修改、删除、返回 | `RefreshList`、`SelectCharacter`、`OnClickAdd`、`OnClickModify`、`OnClickDelete` | `CharacterRepository`、`CharacterModifyPanelController` |
| `CharacterModifyPanelController.cs` | 角色新增/修改面板，支持加载 JSON、预览 Prompt、确认保存 | `OpenForAdd`、`OpenForEdit`、`OnClickLoadJson`、`OnClickConfirm` | `CharacterRepository`、文件 IO |

## Character/UI/Login

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `LoginPanelController.cs` | 登录面板，填入默认账号、调用登录服务、切换面板 | `OnClickLogin` | `AuthService` |

## DataBase

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `AppInitializer.cs` | 应用启动初始化和退出清理 | `Awake`、`OnApplicationQuit` | `DatabaseManager`、`DefaultDataInitializer`、`EmotionMemory` |
| `AuthService.cs` | 登录认证服务，验证用户密码、设置当前角色和会话、触发关系更新 | `Login` | `UserRepository`、`CharacterRepository`、`PasswordHasher`、`GlobalSession` |
| `DatabaseManager.cs` | 数据库连接管理和建表 | `Initialize`、`Close` | `SQLiteConnection`、数据表类 |
| `DefaultDataInitializer.cs` | 创建默认管理员和默认角色 | `Initialize` | `UserRepository`、`CharacterRepository`、`PasswordHasher`、`Application.streamingAssetsPath` |
| `SQLite.cs` | SQLite4Unity3d/SQLite ORM 兼容层，提供连接、建表、查询、事务、属性映射和 SQLite 原生绑定 | 多个 ORM API | SQLite native plugin |

## DataBase/Data

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `GlobalSession.cs` | 当前登录用户和当前角色的全局会话状态 | `SetSession`、`Clear`、`IsAdmin`、`SetCurrentCharacter`、`RefreshCurrentCharacterFromDatabase` | `UserData`、`CharacterProfileData`、`CharacterRepository` |

## UserData

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `UserData.cs` | 用户表结构 | 属性 | SQLite ORM 属性 |
| `UserRepository.cs` | 用户数据访问，支持查询、搜索、新增、修改、删除和默认用户保护 | `GetByUserName`、`GetAll`、`SearchByUserName`、`AddUser`、`UpdateUser`、`DeleteUser`、`DeleteUserByName` | `DatabaseManager`、`PasswordHasher`、`GlobalSession` |

## Character

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `CharacterProfileData.cs` | 角色表结构 | 属性 | SQLite ORM 属性 |
| `CharacterRepository.cs` | 角色数据访问，支持查询、按用户筛选、新增、修改、删除、启用状态管理 | `GetByName`、`GetActiveCharacter`、`AddCharacter`、`UpdateCharacter`、`DeleteCharacter`、`SetActiveCharacter` | `DatabaseManager`、`GlobalSession`、`DefaultDataInitializer` |

## ChatMessage

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `ChatMessageData.cs` | 聊天消息表结构 | 属性 | SQLite ORM 属性 |
| `ChatMessageRepository.cs` | 聊天消息数据访问，支持新增、最近记录、条件搜索、删除、裁剪、计数 | `AddMessage`、`GetRecentMessages`、`SearchMessages`、`DeleteMessages`、`TrimOldMessages`、`Count` | `DatabaseManager` |
| `ChatMessageSearchCondition.cs` | 聊天记录搜索条件对象 | 字段 | `ChatMessageRepository` |
| `ChatMessageService.cs` | 面向 UI/AI 的聊天消息服务，自动使用当前会话保存和查询 | `SaveUserMessage`、`SaveAssistantMessage`、`Search`、`GetRecent`、`DeleteSelected`、`Count` | `GlobalSession`、`ChatMessageRepository`、`EmotionMemory` |

## Emotion

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `EmotionDataMapper.cs` | 在运行时 `EmotionData` 和数据库 `EmotionRecord` 之间转换 | `ToRecord`、`ToData` | `EmotionData`、`EmotionRecord` |
| `EmotionRecord.cs` | 情绪表结构 | 属性 | SQLite ORM 属性 |
| `SQLiteEmotionStorage.cs` | 情绪持久化实现，支持保存、读取最新、读取历史、裁剪、清空 | `Save`、`LoadLatest`、`LoadHistory`、`TrimHistory`、`DeleteAll` | `DatabaseManager`、`EmotionDataMapper` |
| `IEmotionStorage` | 情绪存储接口 | `Save`、`LoadLatest`、`LoadHistory`、`TrimHistory`、`DeleteAll` | `EmotionMemory` |

## InteractionEvent

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `InteractionEventData.cs` | 互动事件表结构 | 属性 | SQLite ORM 属性 |
| `InteractionEventRepository.cs` | 互动事件数据访问，支持添加事件、检查近期同类事件并裁剪旧记录 | `AddEvent`、`HasRecentEvent` | `DatabaseManager` |
| `InteractionEventService.cs` | 互动事件业务服务，负责气泡触发判断和各种事件记录 | `CanTriggerBubble`、`RecordBubbleRequested`、`RecordBubbleShown`、`RecordBubbleIgnored`、`RecordBubbleSuppressed`、`RecordPetDragged`、`RecordPetExpanded`、`RecordPetCollapsed` | `GlobalSession`、`ContextEvaluator`、`InteractionEventRepository`、`UserCharacterStateRepository` |
| `InteractionEventType.cs` | 互动事件类型常量 | 常量 | `InteractionEventService` |

## UserCharacterState

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `RelationshipService.cs` | 关系系统业务门面，把登录、发送消息、AI 回复、展开面板等行为转成好感/信任变化，并构建关系 Prompt | `OnLogin`、`OnUserSendMessage`、`OnAssistantReplyFinished`、`OnAssistantReplyFailed`、`OnOpenPetPanel`、`BuildRelationshipPromptText`、`GetRelationshipLevel` | `GlobalSession`、`UserCharacterStateRepository` |
| `UserCharacterStateData.cs` | 用户-角色情感关系表结构 | 属性 | SQLite ORM 属性 |
| `UserCharacterStateRepository.cs` | 关系状态数据访问和数值规则，支持创建、读取、更新、好感变化、信任变化、互动天数、时间衰减 | `GetOrCreate`、`Get`、`Update`、`ApplyFavorabilityChange`、`ApplyTrustChange`、`UpdateInteractionDays`、`ApplyTimeDecay`、`BuildStateId` | `DatabaseManager` |

## System

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `DesktopContextManager.cs` | Windows 前台窗口检测，获取窗口标题和进程名，用户停留足够久且窗口有意义时触发 `OnWindowChanged` | `OnWindowChanged`、内部 `CheckWindow` | Win32 API、`ContextEvaluator` |
| `ClickThroughController.cs` | 根据鼠标是否悬停 UI 切换窗口点击穿透 | `Update` 内部逻辑 | Win32 API、`GraphicRaycaster`、`EventSystem` |
| `WindowDragHandler.cs` | 让 Windows 接管窗口拖拽，并在拖拽结束后重置 Unity 输入状态 | `OnPointerDown`、`OnDrag`、`OnPointerUp` | Win32 API、`InteractionEventService`、`CustomButtonClicker` |
| `WindowSnapController.cs` | 拖拽释放后吸附屏幕边缘，左右边缘支持半隐藏和鼠标靠边唤出 | `Update` 内部逻辑 | Win32 API、`Screen.currentResolution` |
| `BorderlessWindow.cs` | 移除窗口标题栏和边框 | `Awake` 内部逻辑 | Win32 API |
| `TransparentBackground.cs` | 使用 DWM 扩展客户区实现透明背景 | `Start` 内部逻辑 | `dwmapi.dll` |
| `WindowSizeController.cs` | 根据展开/收起状态调整窗口尺寸并保持置顶 | `ToggleWindow` | Win32 API |

## Test

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `EmotionBuildDebugTest.cs` | 情绪/Prompt 构建调试脚本 | Unity 生命周期 | 情绪和 Prompt 相关类 |

## Tools

| 脚本 | 职责 | 关键入口 | 主要依赖 |
| --- | --- | --- | --- |
| `PasswordHasher.cs` | SHA-256 密码哈希和校验 | `Hash`、`Verify` | `System.Security.Cryptography` |
| `TextMeshProMaxWidth.cs` | 限制 TextMeshPro 文本最大宽度的 UI 工具 | `maxWidth` | TextMeshPro、RectTransform |

## 维护建议

- 新增脚本时优先放入现有层级，不要让 UI 直接操作 SQLite 表。
- 网络 API 后续可从 `AIChat` 拆成独立 Client，便于测试和迁移后端。
- Repository 当前是静态类，重构到服务器或测试环境时可考虑接口化。
- Windows API 脚本保持平台条件编译，避免破坏 Editor 或非 Windows 环境。
- Prompt 文本和 Prompt 构建逻辑尽量分离，避免在 UI 或网络层拼接大段角色设定。
