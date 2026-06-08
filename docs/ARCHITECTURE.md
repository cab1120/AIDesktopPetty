# Architecture

本文档只基于 `Assets/Scripts` 下的脚本描述 AI桌宠 的代码架构，不包含场景、预制体、美术资源或 ProjectSettings。

## 总览

项目可以拆成六个主要层：

| 层级 | 主要目录 | 职责 |
| --- | --- | --- |
| 应用启动层 | `DataBase/AppInitializer.cs` | 初始化数据库、默认数据、情绪系统，并在退出时关闭数据库 |
| 数据与业务层 | `DataBase/**` | 用户、角色、聊天记录、情绪、互动事件、关系状态的本地持久化与业务规则 |
| AI 对话层 | `Character/AI/AIChat/**` | 构建上下文、判断搜索、调用搜索、调用 LLM、格式化返回 |
| Prompt 层 | `Character/AI/Prompt/**` | 角色人格、世界观、情绪、关系、实时信息等 Prompt 拼装 |
| UI 层 | `Character/UI/**` | 登录、聊天、气泡、历史记录、用户管理、角色管理、展开收起 |
| Windows 桌面系统层 | `System/**` | 无边框窗口、透明背景、点击穿透、拖拽、吸附、前台窗口检测 |

常规聊天数据流：

```text
用户输入
  -> UIManager
  -> ChatMessageService.SaveUserMessage
  -> RelationshipService.OnUserSendMessage
  -> AIChat.GetAIReply
  -> ChatContextTextBuilder / ChatContextBuilder
  -> SearchCacheService / SearchDecisionService / SearchWeb
  -> CharacterPromptBuilder
  -> SiliconFlow Chat Completions
  -> UIManager 创建 AI 气泡
  -> ChatMessageService.SaveAssistantMessage
  -> RelationshipService.OnAssistantReplyFinished
```

主动气泡数据流：

```text
Windows 前台窗口变化
  -> DesktopContextManager.OnWindowChanged
  -> AIContextReactionManager.OnWindowChanged
  -> InteractionEventService.CanTriggerBubble
  -> AIChat.GetAIBubbleReply
  -> CharacterPromptBuilder.BuildBubblePrompt
  -> BubbleUIManager.ShowBubble
  -> InteractionEventService.RecordBubbleShown / RecordBubbleIgnored
```

## 启动架构

入口脚本：`Assets/Scripts/DataBase/AppInitializer.cs`

启动时执行：

1. `DatabaseManager.Initialize()` 打开 SQLite 数据库并创建表。
2. `DefaultDataInitializer.Initialize()` 创建默认用户和默认角色。
3. `EmotionMemory.Initialize()` 初始化情绪存储。
4. 应用退出时调用 `DatabaseManager.Close()`。

数据库路径：

```csharp
Path.Combine(Application.persistentDataPath, "iroha_ai.db")
```

默认角色 Prompt 路径：

```csharp
Path.Combine(Application.streamingAssetsPath, "DefaultCharacterPrompt.json")
```

## 数据库架构

数据库连接统一由 `DatabaseManager.Connection` 暴露。脚本使用 SQLite4Unity3d 风格的 ORM 映射，数据类通过 `[Table]`、`[PrimaryKey]`、`[Indexed]`、`[NotNull]`、`[Unique]` 等属性描述表结构。

当前业务表：

| 表 | 数据类 | 主要用途 |
| --- | --- | --- |
| `User` | `UserData` | 用户账号、密码哈希、角色权限 |
| `CharacterProfile` | `CharacterProfileData` | 角色归属、角色名、Prompt JSON、启用状态 |
| `ChatMessage` | `ChatMessageData` | 用户与 AI 消息记录 |
| `EmotionState` | `EmotionRecord` | 角色情绪状态持久化 |
| `InteractionEvent` | `InteractionEventData` | 主动气泡、拖拽、展开收起等事件记录 |
| `UserCharacterState` | `UserCharacterStateData` | 用户与角色之间的好感度、信任值、连续互动天数 |

数据库分层规则：

| 类型 | 示例 | 职责 |
| --- | --- | --- |
| Data | `UserData`、`ChatMessageData` | 表结构，只描述字段 |
| Repository | `UserRepository`、`CharacterRepository` | 直接读写数据库，处理查询、插入、更新、删除 |
| Service | `AuthService`、`ChatMessageService`、`RelationshipService` | 组合多个 Repository 或业务规则，供 UI/AI 层调用 |
| Session | `GlobalSession` | 保存当前登录用户和当前角色上下文 |

## 登录架构

入口：`LoginPanelController.OnClickLogin()`

调用链：

```text
LoginPanelController.OnClickLogin
  -> AuthService.Login
    -> DatabaseManager.Initialize
    -> UserRepository.GetByUserName
    -> PasswordHasher.Verify
    -> CharacterRepository.GetByName
    -> CharacterRepository.SetActiveCharacter
    -> GlobalSession.SetSession
    -> GlobalSession.RefreshCurrentCharacterFromDatabase
    -> UserCharacterStateRepository.GetOrCreate
    -> RelationshipService.OnLogin
```

登录成功后：

- `GlobalSession` 保存当前用户 ID、用户名、角色权限、当前角色 ID 和角色名。
- 当前用户的指定角色被设为启用角色。
- 用户-角色情感状态被创建或读取。
- 登录会触发好感度、信任值、连续互动天数和时间衰减逻辑。

## AI 聊天架构

入口：`UIManager.OnSendButtonClick()`

职责拆分：

| 组件 | 职责 |
| --- | --- |
| `UIManager` | 读取输入、生成气泡、保存聊天记录、启动 AI 协程 |
| `AIChat` | 加载 API Key、构建请求、发起 LLM 和搜索请求 |
| `ChatContextTextBuilder` | 构建用于搜索决策的最近上下文文本 |
| `ChatContextBuilder` | 构建最终发给 LLM 的 messages 数组 |
| `SearchRuleFilter` | 先用规则判断不搜、直接搜或交给 AI 判断 |
| `SearchDecisionService` | 必要时调用低温度 LLM 输出搜索决策 JSON |
| `SearchCacheService` | 复用最近 15 分钟内相关搜索结果 |
| `SearchResultFormatter` | 将搜索结果格式化为 Prompt 片段 |
| `CharacterPromptBuilder` | 将角色 Prompt、情绪、关系、搜索结果组合成 System Prompt |

常规聊天请求使用：

```text
model: Pro/deepseek-ai/DeepSeek-V3
temperature: 0.8
presence_penalty: 0.6
max_tokens: 1024
stream: false
```

搜索决策请求使用：

```text
model: Pro/deepseek-ai/DeepSeek-V3
temperature: 0.1
presence_penalty: 0.0
max_tokens: 256
stream: false
```

## 搜索决策架构

搜索决策分三层：

1. 缓存优先：`SearchCacheService.TryGetRecent()` 判断用户输入是否可复用近期搜索结果。
2. 规则判断：`SearchRuleFilter.JudgeByRule()` 返回 `NoSearch`、`DirectSearch` 或需要 AI 决策的模式。
3. AI 判断：`SearchDecisionService.Decide()` 调用 `CallDeepSeekRaw`，要求模型只返回 JSON。

搜索缓存配置：

| 配置 | 值 |
| --- | --- |
| 最大缓存数 | `5` |
| 有效期 | `15` 分钟 |
| 匹配方式 | 规范化文本包含关系、关键词片段、短追问词 |

## Prompt 架构

Prompt 系统分成“配置加载”和“拼装输出”两部分。

配置结构：`CharacterPromptProfile`

| 字段 | 用途 |
| --- | --- |
| `characterName` | 角色名 |
| `corePersonality` | 核心人格 |
| `worldView` | 世界观 |
| `speechStyle` | 说话风格 |
| `prohibitedItems` | 禁止项 |
| `chatRule` | 常规聊天规则 |
| `bubbleRule` | 主动气泡规则 |
| `realtimeRule` | 实时信息规则 |

运行时上下文：`PromptContext`

| 字段 | 来源 |
| --- | --- |
| `CurrentTime` | `DateTime.Now` |
| `SearchResults` | `SearchResultFormatter` |
| `UserMemory` | 最近聊天文本 |
| `Emotion` | `EmotionMemory.GetCurrentEmotion` |
| `RelationshipText` | `RelationshipService.BuildRelationshipPromptText` |

构建入口：

- `CharacterPromptBuilder.BuildChatPrompt()`：常规聊天 Prompt。
- `CharacterPromptBuilder.BuildBubblePrompt()`：主动气泡 Prompt。
- `IrohaPromptBuilder.Build()`：默认 Iroha 角色 Prompt 组合器。
- `IrohaPromptBuilder.BubbleBuild()`：默认 Iroha 主动气泡 Prompt 组合器。

## 情绪与关系架构

情绪：

```text
EmotionGenerator -> EmotionMemory -> SQLiteEmotionStorage -> EmotionRecord
```

关系：

```text
用户行为
  -> RelationshipService
  -> UserCharacterStateRepository
  -> UserCharacterStateData
  -> RelationshipService.BuildRelationshipPromptText
  -> PromptContext.RelationshipText
```

关系状态包含：

- 好感度 `Favorability`
- 信任值 `TrustValue`
- 连续互动天数 `InteractionDays`
- 最后互动时间 `LastInteractionAtTicks`

关系等级：

| 好感度 | 阶段 |
| --- | --- |
| `< 20` | 疏离 |
| `< 40` | 初识 |
| `< 70` | 熟悉 |
| `< 90` | 信赖 |
| `>= 90` | 亲密 |

## UI 架构

UI 分为四类：

| 类型 | 主要脚本 | 职责 |
| --- | --- | --- |
| 登录 | `LoginPanelController` | 默认账号填充、登录校验、面板切换 |
| 聊天 | `UIManager`、`MessageUI`、`BubbleUIManager` | 输入、消息气泡、AI 回复、主动气泡 |
| 控制面板 | `DesktopPetPanelController`、`ControlPanelController` | 权限入口、用户管理和角色管理入口 |
| 管理面板 | `UserManagePanelController`、`CharacterManagePanelController` 等 | 列表刷新、搜索、新增、修改、删除 |
| 历史记录 | `ChatHistoryPanel`、`ChatHistoryItemUI` | 查询、筛选、选择、删除聊天记录 |

UI 依赖 `GlobalSession` 判断权限和当前上下文。用户管理和角色管理最终落到对应 Repository。

## Windows 桌面系统架构

该层大量依赖 Win32 API，因此主要面向 Windows Standalone。

| 能力 | 脚本 | 实现方式 |
| --- | --- | --- |
| 无边框 | `BorderlessWindow` | 修改窗口样式并刷新 frame |
| 透明背景 | `TransparentBackground` | `DwmExtendFrameIntoClientArea` |
| 点击穿透 | `ClickThroughController` | `WS_EX_TRANSPARENT` + UI Raycast |
| 置顶和尺寸 | `WindowSizeController` | `SetWindowPos` |
| 拖拽 | `WindowDragHandler` | `ReleaseCapture` + `SendMessage(WM_NCLBUTTONDOWN, HTCAPTION)` |
| 吸附/半隐藏 | `WindowSnapController` | 检测窗口矩形、屏幕边界、鼠标全局坐标 |
| 前台窗口检测 | `DesktopContextManager` | `GetForegroundWindow`、`GetWindowText`、`GetWindowThreadProcessId` |

## 主动气泡架构

流程：

```text
DesktopContextManager.CheckWindow
  -> 判断同一窗口停留超过 stayThreshold
  -> ContextEvaluator.IsInteresting
  -> OnWindowChanged(title, processName)
  -> AIContextReactionManager.OnWindowChanged
  -> InteractionEventService.CanTriggerBubble
  -> AIChat.GetAIBubbleReply
  -> BubbleUIManager.ShowBubble
```

防打扰策略：

| 策略 | 位置 | 默认值 |
| --- | --- | --- |
| 同一前台窗口停留阈值 | `DesktopContextManager.stayThreshold` | `5` 秒 |
| 检查频率 | `DesktopContextManager.checkInterval` | `1` 秒 |
| 全局气泡冷却 | `AIContextReactionManager.globalCooldown` | `3` 秒 |
| 同一上下文冷却 | `InteractionEventService.SameContextCooldown` | `10` 分钟 |
| 系统进程/无意义标题过滤 | `ContextEvaluator` | 黑名单规则 |

## 事件记录架构

事件类型定义在 `InteractionEventType`：

- `WindowFocusDetected`
- `BubbleRequested`
- `BubbleShown`
- `BubbleIgnored`
- `BubbleSuppressed`
- `PetWindowDragged`
- `PetExpanded`
- `PetCollapsed`

事件通过 `InteractionEventService` 统一写入 `InteractionEventRepository`，最终保存为 `InteractionEventData`。

## 设计边界

- `AIChat` 当前同时负责配置加载、搜索请求、LLM 请求和日志注册，后续如果继续扩展，可拆分为 `ApiConfigLoader`、`LLMClient`、`SearchClient`。
- Repository 大多是静态类，便于当前项目快速调用，但后续要做测试或服务器迁移时，建议逐步改为可注入接口。
- `GlobalSession` 是全局状态，简单直接，但需要避免在未登录状态下调用依赖当前用户/角色的方法。
- Windows API 脚本应保持平台条件编译，避免非 Windows 平台编译或运行失败。
