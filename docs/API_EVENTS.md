# API Events

本文档列出 `Assets/Scripts` 中项目自有代码的主要公开接口、回调、Unity 事件和业务事件。目的不是替代源码，而是帮助开发时快速定位“哪个方法可以被谁调用”。

## 阅读规则

- `MonoBehaviour` 生命周期方法只在对外理解有价值时列出。
- 私有方法不完整展开，只说明关键内部回调。
- `SQLite.cs` 是第三方兼容层，不列出全部 ORM API。
- “典型调用方”表示当前项目中常见调用来源，不代表唯一调用来源。

## 应用启动与会话

### `AppInitializer`

| 入口 | 类型 | 说明 | 典型调用方 |
| --- | --- | --- | --- |
| `Awake()` | Unity 生命周期 | 初始化数据库、默认数据、情绪记忆 | Unity 场景启动 |
| `OnApplicationQuit()` | Unity 生命周期 | 关闭数据库连接 | Unity 应用退出 |

### `DatabaseManager`

| 入口 | 类型 | 说明 | 典型调用方 |
| --- | --- | --- | --- |
| `Initialize()` | 静态方法 | 打开 SQLite 连接并创建业务表 | `AppInitializer`、Repository |
| `Close()` | 静态方法 | 关闭 SQLite 连接并清空连接引用 | `AppInitializer` |
| `Connection` | 静态属性 | 当前 SQLite 连接 | Repository、初始化器 |

### `DefaultDataInitializer`

| 入口 | 类型 | 说明 | 典型调用方 |
| --- | --- | --- | --- |
| `Initialize()` | 静态方法 | 创建默认用户和默认角色 | `AppInitializer` |
| `DefaultUserName` | 常量 | 默认用户名 `DefaultUser` | `UserRepository`、初始化逻辑 |
| `DefaultCharacterName` | 常量 | 默认角色名 `DefaultCharacter` | `CharacterRepository`、初始化逻辑 |

### `GlobalSession`

| 入口 | 类型 | 说明 | 典型调用方 |
| --- | --- | --- | --- |
| `SetSession(UserData, CharacterProfileData)` | 静态方法 | 设置当前用户和当前角色 | `AuthService` |
| `Clear()` | 静态方法 | 清空登录状态 | 登出逻辑，当前可扩展 |
| `IsAdmin()` | 静态方法 | 判断当前用户是否管理员 | 控制面板权限 |
| `IsUser()` | 静态方法 | 判断当前用户是否普通用户 | 权限逻辑 |
| `IsGuest()` | 静态方法 | 判断当前用户是否访客 | 权限逻辑 |
| `SetCurrentCharacter(CharacterProfileData)` | 静态方法 | 更新当前角色 | `CharacterRepository.SetActiveCharacter` |
| `RefreshCurrentCharacterFromDatabase()` | 静态方法 | 从数据库刷新当前启用角色 | `AuthService` |
| `IsLoggedIn` | 属性 | 是否已登录 | 多个 Service |

## 认证与账号

### `AuthService`

| 入口 | 类型 | 说明 | 返回/输出 | 典型调用方 |
| --- | --- | --- | --- | --- |
| `Login(userName, password, characterName, out error)` | 静态方法 | 验证用户、密码和角色，设置会话，触发关系状态更新 | `bool` + 错误信息 | `LoginPanelController.OnClickLogin` |

### `UserRepository`

| 入口 | 说明 |
| --- | --- |
| `GetByUserName(userName)` | 按用户名查询用户 |
| `GetAll()` | 获取全部用户，按用户名排序 |
| `SearchByUserName(keyword)` | 按用户名关键字搜索 |
| `AddUser(userName, password, role, out error)` | 新增用户，校验用户名、密码、角色和重名 |
| `UpdateUser(userId, newUserName, newPassword, newRole, out error)` | 修改用户，密码为空时不更新密码 |
| `DeleteUser(userId, out error)` | 按 ID 删除用户，保护默认用户和当前用户 |
| `DeleteUserByName(userName, out error)` | 按用户名删除用户，保护默认用户和当前用户 |

## 角色管理

### `CharacterRepository`

| 入口 | 说明 |
| --- | --- |
| `GetByName(characterName)` | 按角色名查询角色 |
| `GetActiveCharacter(userName)` | 查询某用户当前启用角色 |
| `SearchByCharacterName(keyword)` | 全局按角色名搜索 |
| `SearchByCharacterNameForUser(userName, keyword)` | 在指定用户下按角色名搜索 |
| `GetAll()` | 获取全部角色 |
| `GetByUserName(userName)` | 获取指定用户的全部角色 |
| `AddCharacter(userName, characterName, promptJson, isActive, out error)` | 新增角色，必要时禁用同用户其他角色 |
| `UpdateCharacter(characterId, characterName, promptJson, isActive, out error)` | 修改角色信息和启用状态 |
| `UpdateCharacterByName(oldCharacterName, newCharacterName, promptJson, isActive, out error)` | 按角色名修改角色 |
| `DeleteCharacter(characterId, out error)` | 按 ID 删除角色，保护默认角色并保证至少一个角色 |
| `DeleteCharacterByName(characterName, out error)` | 按角色名删除角色 |
| `SetActiveCharacter(userName, characterName, out error)` | 将指定角色设为当前用户启用角色，并更新 `GlobalSession` |
| `GetActiveCharacterCount(userName)` | 统计启用角色数量 |
| `ValidateActiveCharacterState(userName, out error)` | 验证启用角色数量为 1 |

## 聊天消息

### `ChatMessageRepository`

| 入口 | 说明 |
| --- | --- |
| `AddMessage(ChatMessageData message)` | 直接保存消息对象 |
| `AddMessage(...)` | 通过字段保存消息 |
| `GetRecentMessages(userId, characterId, limit)` | 获取指定用户和角色最近消息 |
| `SearchMessages(ChatMessageSearchCondition condition)` | 按用户、角色、内容、发送者、时间范围搜索 |
| `DeleteMessages(messageIds)` | 批量删除消息 |
| `TrimOldMessages(userId, characterId, maxCount)` | 保留最近 N 条，裁剪旧消息 |
| `Count(userId, characterId)` | 统计消息数量 |

### `ChatMessageService`

| 入口 | 说明 | 典型调用方 |
| --- | --- | --- |
| `SaveUserMessage(content)` | 使用当前会话保存用户消息 | `UIManager` |
| `SaveAssistantMessage(content)` | 使用当前会话保存 AI 消息 | `UIManager` |
| `Search(condition)` | 搜索聊天记录 | `ChatHistoryPanel` |
| `GetRecent(limit)` | 获取当前会话最近聊天记录 | `ChatHistoryPanel` |
| `DeleteSelected(messageIds)` | 删除选中的聊天记录 | `ChatHistoryPanel` |
| `Count()` | 当前会话消息计数 | 可扩展 |

## 关系状态

### `RelationshipService`

| 入口 | 说明 | 典型调用方 |
| --- | --- | --- |
| `GetCurrentState()` | 获取当前用户和角色的关系状态 | Prompt/调试 |
| `OnLogin()` | 登录时应用时间衰减、更新互动天数、增加少量好感/信任 | `AuthService` |
| `OnUserSendMessage(message)` | 用户发消息后根据内容长度和无意义词调整好感/信任 | `UIManager` |
| `OnAssistantReplyFinished()` | AI 回复成功后增加少量信任 | `UIManager` |
| `OnAssistantReplyFailed()` | AI 回复失败后降低好感 | 可扩展错误处理 |
| `OnOpenPetPanel()` | 展开宠物面板后增加好感 | `PetToggleUI` |
| `BuildRelationshipPromptText()` | 生成注入 Prompt 的关系状态文本 | `AIChat` / Prompt 构建 |
| `GetRelationshipLevel(state)` | 根据好感度返回关系等级 | Prompt/显示 |

### `UserCharacterStateRepository`

| 入口 | 说明 |
| --- | --- |
| `GetOrCreate(userId, characterId)` | 获取或创建关系状态 |
| `Get(userId, characterId)` | 获取关系状态，不存在则返回空 |
| `Update(state)` | 更新关系状态 |
| `ApplyFavorabilityChange(userId, characterId, delta, updateInteractionTime)` | 应用好感度变化 |
| `ApplyTrustChange(userId, characterId, delta, updateInteractionTime)` | 应用信任值变化 |
| `UpdateInteractionDays(userId, characterId)` | 更新连续互动天数 |
| `ApplyTimeDecay(userId, characterId)` | 应用离线/时间衰减 |
| `BuildStateId(userId, characterId)` | 构造关系状态主键 |

## 情绪系统

### `EmotionMemory`

| 入口 | 说明 | 典型调用方 |
| --- | --- | --- |
| `Initialize()` | 初始化情绪存储 | `AppInitializer` |
| `GetCurrentEmotion(userId, characterId)` | 获取当前情绪，不存在或过期时生成新情绪 | `AIChat` |
| `PeekCurrentEmotion()` | 查看内存中的当前情绪，不触发生成 | 调试 |
| `SetEmotion(data, userId, characterId)` | 设置并保存情绪 | 调试/扩展 |
| `GenerateNewEmotion(userId, characterId)` | 生成并保存新情绪 | 情绪刷新逻辑 |
| `ResetEmotion()` | 重置当前情绪 | 调试 |

### `SQLiteEmotionStorage` / `IEmotionStorage`

| 入口 | 说明 |
| --- | --- |
| `Save(data, userId, characterId)` | 保存情绪记录 |
| `LoadLatest(userId, characterId)` | 读取最新情绪 |
| `LoadHistory(userId, characterId, limit)` | 读取情绪历史 |
| `TrimHistory(userId, characterId, maxCount)` | 裁剪历史 |
| `DeleteAll(userId, characterId)` | 删除指定用户角色的情绪记录 |

## AI 对话接口

### `AIChat`

| 入口 | 类型 | 说明 | 回调 |
| --- | --- | --- | --- |
| `GetAIReply(userMessage, callback)` | 协程 | 常规聊天入口，会处理上下文、搜索、Prompt 和 LLM 调用 | `callback(reply)` |
| `GetAIBubbleReply(context, callback)` | 协程 | 主动气泡入口，会根据窗口上下文生成短回复 | `callback(reply)` |

内部网络接口：

| 内部方法 | 说明 |
| --- | --- |
| `SearchWeb(query, searchCallback)` | 调用 Bocha 搜索接口并返回摘要文本 |
| `CallDeepSeek(systemPrompt, userMessage, callback)` | 调用 SiliconFlow 生成正式回复 |
| `CallDeepSeekRaw(systemPrompt, userMessage, callback)` | 调用 SiliconFlow 生成搜索决策 JSON |

### 搜索相关接口

| 类 | 入口 | 说明 |
| --- | --- | --- |
| `SearchDecisionService` | `Decide(userMessage, recentContext, rawLLMCall, callback)` | 输出 `SearchDecision`，可能直接规则返回，也可能调用 LLM 判断 |
| `SearchCacheService` | `TryGetRecent(userMessage, out entry)` | 尝试根据用户输入命中近期缓存 |
| `SearchCacheService` | `Add(query, results, reason)` | 添加或替换搜索缓存 |
| `SearchRuleFilter` | `JudgeByRule(message)` | 返回 `SearchDecisionMode`，用于判断不搜索、直接搜索或交给 AI 判断 |

## Prompt 接口

| 类 | 入口 | 说明 |
| --- | --- | --- |
| `CharacterPromptBuilder` | `BuildChatPrompt(context)` | 构建常规聊天 System Prompt |
| `CharacterPromptBuilder` | `BuildBubblePrompt(context)` | 构建主动气泡 System Prompt |
| `CharacterPromptLoader` | `LoadCurrentProfile()` | 读取当前角色 Prompt JSON |
| `IrohaPromptBuilder` | `Build(...)` | 组合默认角色常规聊天 Prompt |
| `IrohaPromptBuilder` | `BubbleBuild(...)` | 组合默认角色主动气泡 Prompt |
| `IrohaPromptJsonExporter` | `Export()` | 导出默认角色 Prompt JSON |
| `IrohaRealtimeContext` | `BuildForBubble(currentTime, searchResults)` | 构建主动气泡实时上下文 |
| `IrohaRealtimeContext` | `BuildForChat(searchResults)` | 构建聊天实时上下文 |
| `IrohaMemoryContext` | `Build(userMemory)` | 构建记忆上下文 |
| `IrohaEmotionPromptBuilder` | `Build(emotion)` | 构建情绪上下文 |

## UI 事件接口

### 登录和聊天

| 脚本 | 入口 | 绑定方式 | 说明 |
| --- | --- | --- | --- |
| `LoginPanelController` | `OnClickLogin()` | Button OnClick | 登录并切换面板 |
| `UIManager` | `OnInputEndEdit(string)` | TMP_InputField EndEdit | 回车发送消息 |
| `UIManager` | `OnSendButtonClick()` | Button OnClick | 创建用户气泡、保存消息、请求 AI 回复 |
| `PetToggleUI` | `ToggleUI()` | Button OnClick | 展开/收起宠物窗口 |
| `PetToggleUI` | `ToggleButton()` | Button OnClick | 展开/隐藏功能按钮组 |
| `PetToggleUI` | `ExitButton()` | Button OnClick | 退出应用 |

### 聊天历史

| 脚本 | 入口 | 绑定方式 | 说明 |
| --- | --- | --- | --- |
| `ChatHistoryPanel` | `Open()` | Button/代码 | 打开历史面板 |
| `ChatHistoryPanel` | `Close()` | Button | 关闭历史面板 |
| `ChatHistoryPanel` | `LoadRecent()` | Button/打开时 | 加载最近记录 |
| `ChatHistoryPanel` | `OnSearchButtonClick()` | Button | 按条件搜索 |
| `ChatHistoryPanel` | `OnClearSearchButtonClick()` | Button | 清空搜索条件 |
| `ChatHistoryPanel` | `OnDeleteSelectedButtonClick()` | Button | 删除选中记录 |
| `ChatHistoryItemUI` | `OnClick()` | Button/点击项 | 切换选中状态 |

### 用户管理

| 脚本 | 入口 | 绑定方式 | 说明 |
| --- | --- | --- | --- |
| `UserManagePanelController` | `OnClickSearch()` | Button | 搜索用户 |
| `UserManagePanelController` | `OnClickAdd()` | Button | 打开新增用户面板 |
| `UserManagePanelController` | `OnClickModify()` | Button | 打开修改用户面板 |
| `UserManagePanelController` | `OnClickDelete()` | Button | 删除选中用户 |
| `UserManagePanelController` | `OnClickBack()` | Button | 返回控制面板 |
| `UserModifyPanelController` | `OnClickConfirm()` | Button | 确认新增或修改 |
| `UserModifyPanelController` | `OnClickCancel()` | Button | 取消并关闭 |
| `UserListItem` | `Init(user, controller)` | 代码 | 初始化列表项 |

### 角色管理

| 脚本 | 入口 | 绑定方式 | 说明 |
| --- | --- | --- | --- |
| `CharacterManagePanelController` | `OnClickSearch()` | Button | 搜索角色 |
| `CharacterManagePanelController` | `OnClickAdd()` | Button | 打开新增角色面板 |
| `CharacterManagePanelController` | `OnClickModify()` | Button | 打开修改角色面板 |
| `CharacterManagePanelController` | `OnClickDelete()` | Button | 删除选中角色 |
| `CharacterManagePanelController` | `OnClickBack()` | Button | 返回控制面板 |
| `CharacterModifyPanelController` | `OnClickLoadJson()` | Button | 从路径读取 Prompt JSON 并预览 |
| `CharacterModifyPanelController` | `OnClickConfirm()` | Button | 确认新增或修改 |
| `CharacterModifyPanelController` | `OnClickCancel()` | Button | 取消并关闭 |
| `CharacterListItem` | `Init(character, controller)` | 代码 | 初始化列表项 |

### 控制面板

| 脚本 | 入口 | 绑定方式 | 说明 |
| --- | --- | --- | --- |
| `DesktopPetPanelController` | `OnClickOpenControlPanel()` | Button | 打开控制面板并根据权限显示管理入口 |
| `ControlPanelController` | `OnClickUserManage()` | Button | 进入用户管理 |
| `ControlPanelController` | `OnClickCharacterManage()` | Button | 进入角色管理 |
| `ControlPanelController` | `OnClickBack()` | Button | 返回宠物面板 |

### 自定义按钮和窗口拖拽

| 脚本 | 入口 | 绑定方式 | 说明 |
| --- | --- | --- | --- |
| `CustomButtonClicker` | `onClick` | Inspector UnityEvent | 自定义点击事件 |
| `CustomButtonClicker` | `PerformClick()` | 代码 | 手动触发 `onClick` |
| `CustomButtonClicker` | `OnPointerEnter/Exit` | EventSystem | 悬停视觉反馈 |
| `CustomButtonClicker` | `OnPointerDownVisual/OnPointerUpVisual` | 代码 | 按下/松开视觉反馈 |
| `WindowDragHandler` | `OnPointerDown` | EventSystem | 记录拖拽起点 |
| `WindowDragHandler` | `OnDrag` | EventSystem | 超过阈值后交给 Windows 拖拽窗口 |
| `WindowDragHandler` | `OnPointerUp` | EventSystem | 未拖拽时触发自定义按钮点击 |

## 桌面上下文事件

### `DesktopContextManager.OnWindowChanged`

定义：

```csharp
public Action<string, string> OnWindowChanged;
```

参数：

| 参数 | 含义 |
| --- | --- |
| `title` | 当前前台窗口标题 |
| `processName` | 当前前台窗口进程名 |

触发条件：

1. Windows Standalone 环境。
2. 前台窗口标题存在。
3. 用户在同一窗口停留超过 `stayThreshold`。
4. 该窗口与上次真正触发窗口不同。
5. `ContextEvaluator.IsInteresting(title, processName)` 返回 true。

订阅者：

- `AIContextReactionManager.OnEnable()` 订阅。
- `AIContextReactionManager.OnDisable()` 取消订阅。

### `AIContextReactionManager`

| 内部回调 | 说明 |
| --- | --- |
| `OnWindowChanged(title, processName)` | 收到桌面窗口事件后执行冷却、事件判断、AI 请求 |
| `OnReactionGenerated(reply)` | AI 生成主动气泡后决定显示、忽略或记录事件 |

## 互动事件类型

定义位置：`InteractionEventType`

| 常量 | 含义 | 典型记录方法 |
| --- | --- | --- |
| `WindowFocusDetected` | 检测到窗口聚焦 | 当前预留 |
| `BubbleRequested` | 请求生成主动气泡 | `RecordBubbleRequested` |
| `BubbleShown` | 主动气泡已显示 | `RecordBubbleShown` |
| `BubbleIgnored` | AI 返回空或 `[IGNORE]`，气泡被忽略 | `RecordBubbleIgnored` |
| `BubbleSuppressed` | 因冷却、未登录或窗口无意义等原因被抑制 | `RecordBubbleSuppressed` |
| `PetWindowDragged` | 用户拖拽宠物窗口 | `RecordPetDragged` |
| `PetExpanded` | 用户展开宠物面板 | `RecordPetExpanded` |
| `PetCollapsed` | 用户收起宠物面板 | `RecordPetCollapsed` |

### `InteractionEventService`

| 入口 | 说明 |
| --- | --- |
| `BuildContextKey(title, processName)` | 标准化窗口上下文 Key |
| `CanTriggerBubble(title, processName, out contextKey, out reason)` | 判断是否允许生成主动气泡 |
| `RecordBubbleRequested(title, processName)` | 记录请求主动气泡 |
| `RecordBubbleShown(title, processName, reply)` | 记录气泡显示，并增加少量信任 |
| `RecordBubbleIgnored(title, processName)` | 记录气泡被忽略 |
| `RecordBubbleSuppressed(title, processName, reason)` | 记录气泡被抑制 |
| `RecordPetDragged()` | 记录拖拽 |
| `RecordPetExpanded()` | 记录展开，并增加好感 |
| `RecordPetCollapsed()` | 记录收起 |

## Windows 系统接口

这些接口多由 Unity 生命周期自动驱动，核心公开入口较少。

| 脚本 | 公开入口/字段 | 说明 |
| --- | --- | --- |
| `WindowSizeController` | `ToggleWindow(bool isExpanded)` | 展开时调整到 `expandedWidth/expandedHeight`，收起时调整到 `collapsedWidth/collapsedHeight` |
| `ClickThroughController` | `raycaster` | 用于判断鼠标是否位于 UI 上，从而开关点击穿透 |
| `WindowSnapController` | `snapDistance`、`hideOffset` | 控制吸附距离和半隐藏露出宽度 |
| `WindowDragHandler` | `dragThreshold` | 拖拽阈值，超过后触发系统窗口拖拽 |
| `BorderlessWindow` | Unity 生命周期 | 启动时移除窗口边框 |
| `TransparentBackground` | Unity 生命周期 | 启动时开启透明背景 |

## 工具接口

| 类 | 入口 | 说明 |
| --- | --- | --- |
| `PasswordHasher` | `Hash(rawPassword)` | 返回 SHA-256 哈希字符串 |
| `PasswordHasher` | `Verify(rawPassword, passwordHash)` | 校验明文密码是否匹配哈希 |
| `TextMeshProMaxWidth` | `maxWidth` | 文本最大宽度配置 |

## 接入建议

- UI 按钮优先绑定 Controller 的 `OnClick...` 方法，不直接绑定 Repository。
- 新增会影响关系状态的行为时，优先在 `RelationshipService` 中加方法，而不是散落到 UI 里。
- 新增会影响事件历史的行为时，优先在 `InteractionEventService` 中加 `Record...` 方法。
- 新增 AI 相关网络能力时，先考虑是否应从 `AIChat` 拆出独立 Client，避免 `AIChat` 继续膨胀。
- 新增 Prompt 变量时，优先扩展 `PromptContext`，再由 `CharacterPromptBuilder` 注入。
