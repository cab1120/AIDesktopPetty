# AIDesktopPetty

AIDesktopPetty（中文名：AI桌宠）是一个基于 Unity 的 Windows 桌面 AI 宠物项目。项目将透明/无边框/置顶窗口、桌面窗口上下文感知、聊天 UI、角色 Prompt、SQLite 本地数据持久化、好感度/信任度关系系统，以及联网搜索增强的 AI 对话整合在一起。

当前代码中默认角色为 `DefaultCharacter`，默认用户为 `DefaultUser`。

## 目录

- [项目状态](#项目状态)
- [授权说明](#授权说明)
- [主要功能](#主要功能)
- [技术栈与依赖](#技术栈与依赖)
- [项目结构](#项目结构)
- [运行环境](#运行环境)
- [快速开始](#快速开始)
- [配置说明](#配置说明)
- [默认账号](#默认账号)
- [核心流程](#核心流程)
- [数据库设计](#数据库设计)
- [AI 对话与搜索机制](#ai-对话与搜索机制)
- [桌面宠物窗口能力](#桌面宠物窗口能力)
- [UI 与管理功能](#ui-与管理功能)
- [角色 Prompt 系统](#角色-prompt-系统)
- [日志](#日志)
- [构建说明](#构建说明)
- [开发注意事项](#开发注意事项)
- [已知问题与待确认事项](#已知问题与待确认事项)

## 项目状态

- Unity 项目名：`AIDesktopPetty`
- Unity 版本：`2021.3.21f1c1`
- 当前构建场景：`Assets/Scenes/SampleScene.unity`
- 主要目标平台：Windows Standalone
- 渲染模板来源：Unity 2D Built-in Renderer 模板
- 本地数据库：SQLite，运行时使用 `Application.persistentDataPath/iroha_ai.db`


## 授权说明

当前授权状态：MIT License。



## 主要功能

- AI 桌面宠物聊天：用户可展开聊天面板，向角色发送消息并获得 AI 回复。
- 角色 Prompt 驱动：角色人格、世界观、语言风格、聊天规则、气泡规则和实时信息规则由 JSON Prompt 配置驱动。
- 联网搜索增强：根据用户输入和最近上下文判断是否需要搜索，必要时调用 Bocha 搜索，将搜索摘要注入 AI Prompt。
- 搜索缓存：对最近搜索结果进行短期缓存，减少重复搜索请求。
- 本地聊天记忆：用户消息和 AI 回复保存到 SQLite，用于后续上下文构建和历史查询。
- 多用户与角色管理：支持用户新增、搜索、修改、删除；支持角色新增、搜索、修改、删除、启用状态管理。
- 登录系统：基于本地用户表和密码哈希进行登录验证。
- 关系系统：维护用户与角色之间的好感度、信任值和连续互动天数，并将关系状态写入 Prompt。
- 情绪系统：保存和读取角色情绪状态，并将当前情绪写入 Prompt。
- 桌面上下文感知：在 Windows 下读取当前前台窗口标题和进程名，判断是否适合触发主动气泡对话。
- 主动气泡：当检测到用户停留在有意义窗口上时，AI 可生成短句气泡主动搭话。
- 透明置顶窗口：通过 Win32 API 实现无边框、透明背景、置顶和点击穿透。
- 窗口拖拽与吸附：支持拖动宠物窗口，靠近屏幕边缘时吸附，左右边缘支持半隐藏和鼠标靠边唤出。
- 展开/收起：宠物图标与聊天面板可切换，窗口尺寸随状态改变。

## 技术栈与依赖

### Unity

- Unity Editor：`2021.3.21f1c1`
- UI：UGUI + TextMeshPro
- 网络：`UnityWebRequest`
- JSON：`com.unity.nuget.newtonsoft-json`
- 本地数据库：SQLite4Unity3d + SQLite native plugin
- Windows 桌面能力：`user32.dll`、`dwmapi.dll` P/Invoke

### Unity Package 依赖

主要依赖来自 `Packages/manifest.json`：

- `com.unity.textmeshpro`: `3.0.6`
- `com.unity.ugui`: `1.0.0`
- `com.unity.nuget.newtonsoft-json`: `3.2.2`
- `com.unity.test-framework`: `1.1.31`
- `com.unity.timeline`: `1.6.4`
- `com.unity.visualscripting`: `1.8.0`
- `com.unity.ide.rider`: `3.0.40`
- `com.unity.ide.visualstudio`: `2.0.17`
- `com.unity.ide.vscode`: `1.2.5`

### 外部服务

项目当前代码使用以下外部 API：

- SiliconFlow Chat Completions：`https://api.siliconflow.cn/v1/chat/completions`
- Bocha Web Search：`https://api.bochaai.com/v1/web-search`

AI 模型配置在 `Assets/Scripts/Character/AI/AIChat/AIChat.cs` 中，当前模型为：

```text
Pro/deepseek-ai/DeepSeek-V3
```

## 项目结构

```text
AIDesktopPetty/
├── Assets/
│   ├── Art/                         # 图片、PSD、字体等美术资源
│   ├── Plugins/                     # SQLite native plugins
│   │   ├── Android/libs/            # Android sqlite3 so
│   │   ├── WSA/                     # Windows Store sqlite3 dll
│   │   ├── x64/sqlite3.dll          # Windows x64 sqlite3
│   │   └── x86/sqlite3.dll          # Windows x86 sqlite3
│   ├── Prefeb/                      # UI 预制体，目录名当前为 Prefeb
│   ├── Scenes/
│   │   ├── SampleScene.unity        # 当前构建场景
│   │   └── LargeScene.unity         # 测试场景，未加入当前 Build Settings
│   ├── Scripts/
│   │   ├── Character/               # AI、Prompt、聊天 UI、控制面板
│   │   ├── DataBase/                # 数据库、认证、数据表、仓储与服务
│   │   ├── System/                  # 桌面窗口、拖拽、透明、上下文检测
│   │   ├── Test/                    # 调试脚本
│   │   └── Tools/                   # 密码哈希、TextMeshPro 工具
│   ├── StreamingAssets/
│   │   ├── config.json              # API Key 配置
│   │   ├── DefaultCharacterPrompt.json
│   │   └── iroha_ai.db              # SQLite 数据库文件
│   └── TextMesh Pro/                # TextMeshPro 默认资源
├── Packages/
│   ├── manifest.json
│   └── packages-lock.json
├── ProjectSettings/
│   ├── ProjectVersion.txt
│   ├── ProjectSettings.asset
│   └── EditorBuildSettings.asset
├── AIDesktopPetty.sln
└── run_log.txt                      # 运行日志输出文件
```

## 运行环境

### 必需环境

- Windows 系统：桌面宠物窗口能力依赖 Win32 API。
- Unity `2021.3.21f1c1`：建议使用完全一致版本打开，避免资源或包版本迁移差异。
- 可访问 SiliconFlow 和 Bocha API 的网络环境。
- 有效的 SiliconFlow API Key 和 Bocha API Key。

### 平台说明

项目主功能面向 Windows Standalone，暂不计划支持 macOS、Linux 或 Android。虽然项目中存在 Android 和 WSA 的 SQLite 插件，但核心桌面能力大量依赖 `UNITY_STANDALONE_WIN` 下的 Win32 API。

## 快速开始

1. 使用 Unity Hub 打开项目根目录 `AIDesktopPetty`。
2. 确认 Unity 版本为 `2021.3.21f1c1`。
3. 打开 `Assets/Scenes/SampleScene.unity`。
4. 检查 `Assets/StreamingAssets/config.json`，填入自己的 API Key。
5. 检查 `Assets/StreamingAssets/iroha_ai.db` 是否存在。
6. 点击 Play。
7. 登录界面默认会填入：

```text
用户名：DefaultUser
密码：123456
角色：DefaultCharacter
```

8. 登录成功后进入桌面宠物面板，可展开聊天界面进行对话。

## 配置说明

### `Assets/StreamingAssets/config.json`

配置文件结构如下：

```json
{
  "siliconFlowKey": "YOUR_SILICONFLOW_KEY",
  "bochaApiKey": "YOUR_BOCHA_API_KEY"
}
```

字段说明：

| 字段 | 用途 |
| --- | --- |
| `siliconFlowKey` | SiliconFlow Chat Completions API Key，用于调用 DeepSeek-V3 |
| `bochaApiKey` | Bocha Web Search API Key，用于联网搜索 |

注意：为了正常运行请在指定文件地址创建config.json并且填入自己的apikey，具体模板参照config.example.json

### `Assets/StreamingAssets/DefaultCharacterPrompt.json`

默认角色 Prompt 配置，字段结构由 `CharacterPromptProfile` 定义：

| 字段 | 用途 |
| --- | --- |
| `characterName` | 角色名称 |
| `corePersonality` | 核心人格 |
| `worldView` | 世界观/观察方式 |
| `speechStyle` | 语言风格 |
| `prohibitedItems` | 禁止项与回复格式约束 |
| `chatRule` | 主动聊天时的规则 |
| `bubbleRule` | 桌面气泡主动搭话规则 |
| `realtimeRule` | 实时信息/搜索结果融入规则 |


## 默认账号

默认数据由 `DefaultDataInitializer` 创建。

| 项 | 值 |
| --- | --- |
| 默认用户名 | `DefaultUser` |
| 默认密码 | `123456` |
| 默认角色 | `Admin` |
| 默认角色名 | `DefaultCharacter` |

初始化逻辑：

- 如果不存在 `DefaultUser`，创建默认管理员用户。
- 如果不存在 `DefaultCharacter`，读取默认角色 Prompt JSON 并创建默认角色。
- 密码通过 `PasswordHasher.Hash` 保存为 SHA-256 哈希，不保存明文。

## 核心流程

### 应用启动

启动脚本：`Assets/Scripts/DataBase/AppInitializer.cs`

流程：

1. 初始化数据库连接。
2. 创建数据库表。
3. 初始化默认用户和默认角色。
4. 初始化情绪记忆。
5. 应用退出时关闭数据库连接。

### 登录流程

登录脚本：`Assets/Scripts/DataBase/AuthService.cs`

流程：

1. 根据用户名查找用户。
2. 校验密码哈希。
3. 根据角色名查找角色。
4. 更新用户最后登录时间。
5. 将角色设为当前用户的启用角色。
6. 写入 `GlobalSession`。
7. 创建或读取用户-角色情感关系状态。
8. 触发登录带来的关系变化。

### 聊天流程

聊天入口：`Assets/Scripts/Character/UI/UIManager.cs`

流程：

1. 用户输入文本并点击发送或按回车。
2. UI 创建用户消息气泡。
3. 保存用户消息到 SQLite。
4. 根据用户消息更新好感度/信任值。
5. 调用 `AIChat.GetAIReply`。
6. 构建最近聊天上下文。
7. 判断是否需要联网搜索。
8. 根据搜索结果、时间、记忆、情绪、关系状态构建 System Prompt。
9. 调用 SiliconFlow Chat Completions。
10. 创建 AI 回复气泡。
11. 保存 AI 消息到 SQLite。
12. 更新关系状态。

## 数据库设计

数据库管理入口：`Assets/Scripts/DataBase/DatabaseManager.cs`

当前创建以下表：

- `User`
- `CharacterProfile`
- `UserCharacterState`
- `EmotionState`
- `ChatMessage`
- `InteractionEvent`

### `User`

对应类：`UserData`

| 字段 | 说明 |
| --- | --- |
| `UserId` | 用户 ID，主键 |
| `UserName` | 用户名，唯一且非空 |
| `PasswordHash` | 密码哈希，非空 |
| `Role` | 用户角色，支持 `Admin`、`User`、`Guest` |
| `CreatedAtTicks` | 创建时间 |
| `LastLoginAtTicks` | 最后登录时间 |

### `CharacterProfile`

对应类：`CharacterProfileData`

| 字段 | 说明 |
| --- | --- |
| `CharacterId` | 角色 ID，主键 |
| `UserName` | 所属用户名 |
| `CharacterName` | 角色名称 |
| `PromptJson` | 角色 Prompt JSON |
| `IsActive` | 是否为当前启用角色 |
| `CreatedAtTicks` | 创建时间 |

### `UserCharacterState`

对应类：`UserCharacterStateData`

| 字段 | 说明 |
| --- | --- |
| `StateId` | 状态 ID，主键 |
| `UserId` | 用户 ID |
| `CharacterId` | 角色 ID |
| `Favorability` | 好感度 |
| `TrustValue` | 信任值 |
| `InteractionDays` | 连续互动天数 |
| `LastInteractionAtTicks` | 最后互动时间 |
| `CreatedAtTicks` | 创建时间 |

关系等级由好感度区间决定：

| 好感度区间 | 关系等级 |
| --- | --- |
| `< 20` | 疏离 |
| `< 40` | 初识 |
| `< 70` | 熟悉 |
| `< 90` | 信赖 |
| `>= 90` | 亲密 |

### `EmotionState`

对应类：`EmotionRecord`

| 字段 | 说明 |
| --- | --- |
| `EmotionId` | 情绪 ID，主键 |
| `UserId` | 用户 ID |
| `CharacterId` | 角色 ID |
| `EmotionType` | 情绪类型 |
| `Intensity` | 情绪强度 |
| `RemainingMinutes` | 持续时间 |
| `CreatedAtTicks` | 创建时间 |

### `ChatMessage`

对应类：`ChatMessageData`

| 字段 | 说明 |
| --- | --- |
| `MessageId` | 消息 ID，主键 |
| `UserId` | 用户 ID |
| `UserName` | 用户名 |
| `CharacterId` | 角色 ID |
| `CharacterName` | 角色名称 |
| `EmotionId` | 关联情绪 ID |
| `Sender` | 发送者，`User` 或 `Assistant` |
| `Content` | 消息内容 |
| `CreatedAtTicks` | 创建时间 |

### `InteractionEvent`

对应类：`InteractionEventData`

| 字段 | 说明 |
| --- | --- |
| `EventId` | 事件 ID，主键 |
| `UserId` | 用户 ID |
| `CharacterId` | 角色 ID |
| `EventType` | 事件类型 |
| `EventSource` | 事件来源 |
| `Description` | 事件描述 |
| `ContextKey` | 上下文 Key，用于防重复 |
| `EmotionImpact` | 情绪影响 |
| `FavorabilityImpact` | 好感度影响 |
| `CreatedAtTicks` | 创建时间 |

## AI 对话与搜索机制

入口脚本：`Assets/Scripts/Character/AI/AIChat/AIChat.cs`

### 常规聊天

AI 回复由以下信息共同构建：

- 当前时间。
- 最近 8 条聊天上下文。
- 当前用户与当前角色的关系状态。
- 当前角色情绪状态。
- 当前角色 Prompt 配置。
- 必要时加入联网搜索结果。

请求参数中当前使用：

| 参数 | 值 |
| --- | --- |
| `model` | `Pro/deepseek-ai/DeepSeek-V3` |
| `stream` | `false` |
| `temperature` | `0.8` |
| `presence_penalty` | `0.6` |
| `max_tokens` | `1024` |

### 搜索决策

搜索决策由 `SearchDecisionService` 和 `SearchRuleFilter` 负责。

大致策略：

- 明确涉及近期信息、现实人物、热点、版本、价格、天气、比赛、新闻等内容时搜索。
- 普通陪伴、情绪表达、闲聊、刚聊过的本地上下文一般不搜索。
- 不确定时可调用低温度 LLM 让模型输出 JSON 决策。

搜索决策请求参数中当前使用：

| 参数 | 值 |
| --- | --- |
| `model` | `Pro/deepseek-ai/DeepSeek-V3` |
| `temperature` | `0.1` |
| `presence_penalty` | `0.0` |
| `max_tokens` | `256` |

### 搜索缓存

搜索缓存由 `SearchCacheService` 负责。

| 配置 | 值 |
| --- | --- |
| 最大缓存数量 | `5` |
| 缓存有效期 | `15` 分钟 |

缓存匹配基于查询文本和用户追问文本的简单相关性判断。

### Bocha 搜索

请求地址：

```text
https://api.bochaai.com/v1/web-search
```

当前请求字段：

| 字段 | 值 |
| --- | --- |
| `query` | 搜索关键词 |
| `freshness` | `noLimit` |
| `summary` | `true` |

## 桌面宠物窗口能力

相关脚本位于 `Assets/Scripts/System/`。

### 透明与无边框

- `BorderlessWindow`：移除 Windows 窗口标题栏和边框。
- `TransparentBackground`：调用 DWM 扩展窗口客户区，实现透明背景。
- `ClickThroughController`：在鼠标不悬停 UI 时启用点击穿透，悬停 UI 时关闭点击穿透。

### 置顶与尺寸切换

- `WindowSizeController`：控制展开和收起尺寸。
- 默认展开尺寸：`432 x 768`
- 默认收起尺寸：`180 x 250`

### 拖拽与吸附

- `WindowDragHandler`：使用 Win32 消息让系统接管窗口拖拽。
- `WindowSnapController`：检测窗口靠近屏幕边缘后吸附。
- 左右边缘支持半隐藏，鼠标靠近边缘时再显示。
- 默认吸附距离：`20` 像素。
- 默认半隐藏露出宽度：`80` 像素。

### 桌面上下文检测

- `DesktopContextManager`：定时读取当前前台窗口标题和进程名。
- 默认检查间隔：`1` 秒。
- 默认停留阈值：`5` 秒。
- `ContextEvaluator`：过滤系统进程和无意义窗口标题。
- `AIContextReactionManager`：通过 AI 生成主动气泡回复，并记录事件。
- 主动气泡全局冷却：`3` 秒。
- 同一窗口上下文冷却：`10` 分钟。

## UI 与管理功能

### 登录面板

脚本：`LoginPanelController`

- 启动时显示登录面板，隐藏桌面宠物面板。
- 自动填入默认账号、密码和角色名。
- 登录成功后切换到桌面宠物面板。

### 聊天面板

脚本：`UIManager`

- 支持回车发送。
- 使用用户消息预制体和 AI 消息预制体生成聊天气泡。
- 消息发送后自动滚动到底部。
- 消息会保存到 SQLite。

相关预制体：

- `Assets/Prefeb/Chat/MessageRootUser.prefab`
- `Assets/Prefeb/Chat/MessageRootAI.prefab`

### 气泡提示

脚本：`BubbleUIManager`

- 显示桌面宠物短气泡。
- 默认显示时长：`5` 秒。
- 会强制刷新 TextMeshPro 和 Layout 布局。

### 宠物展开/收起

脚本：`PetToggleUI`

- 收起状态显示宠物图标。
- 展开状态显示聊天面板。
- 展开时记录互动事件并增加关系状态。
- 提供退出按钮调用 `Application.Quit()`。

### 用户管理

相关脚本：

- `UserManagePanelController`
- `UserModifyPanelController`
- `UserListItem`
- `UserRepository`

能力：

- 搜索用户。
- 新增用户。
- 修改用户名、密码、角色。
- 删除用户。
- 阻止删除默认管理员。
- 阻止当前登录用户删除自己。

### 角色管理

相关脚本：

- `CharacterManagePanelController`
- `CharacterModifyPanelController`
- `CharacterListItem`
- `CharacterRepository`

能力：

- 搜索角色。
- 新增角色。
- 修改角色名称、Prompt JSON 和启用状态。
- 删除角色。
- 阻止删除默认角色。
- 保证每个用户至少有一个启用角色。
- 保证同一用户同一时间最多一个启用角色。

### 聊天历史

相关脚本：

- `ChatHistoryPanel`
- `ChatHistoryItemUI`
- `ChatMessageRepository`
- `ChatMessageService`

能力：

- 加载最近聊天记录。
- 按条件搜索聊天记录。
- 删除选中的聊天记录。

## 角色 Prompt 系统

Prompt 相关脚本位于：

```text
Assets/Scripts/Character/AI/Prompt/
```

主要类：

| 类 | 作用 |
| --- | --- |
| `CharacterPromptProfile` | 定义 JSON Prompt 配置结构 |
| `CharacterPromptLoader` | 加载角色 Prompt |
| `CharacterPromptBuilder` | 构建聊天 Prompt 和气泡 Prompt |
| `PromptContext` | Prompt 构建上下文 |
| `IrohaPromptBuilder` | 构建默认角色 Iroha 的完整 Prompt |
| `IrohaPromptJsonExporter` | 将代码中的默认 Prompt 导出为 JSON |
| `IrohaEmotionPromptBuilder` | 将情绪状态转成 Prompt 文本 |
| `IrohaRealtimeContext` | 构建实时信息上下文 |
| `IrohaMemoryContext` | 构建用户记忆上下文 |

当前 `AppInitializer` 中存在 `IrohaPromptJsonExporter.Export()` 调用，但被注释。需要重新生成默认 Prompt JSON 时可手动启用。

## 日志

`AIChat.RunLog()` 会注册 Unity 日志回调，并将日志写入项目根目录下的：

```text
run_log.txt
```

记录内容包括：

- 普通日志。
- 警告。
- 错误。
- 异常堆栈。

## 构建说明

当前 Build Settings 只启用了：

```text
Assets/Scenes/SampleScene.unity
```

建议构建目标：

```text
Windows / x86_64 / Standalone
```

构建前检查：

1. `Assets/StreamingAssets/config.json` 已填写有效 Key。
2. `Assets/StreamingAssets/iroha_ai.db` 存在。
3. `Assets/Plugins/x64/sqlite3.dll` 存在。
4. Build Settings 中启用了 `SampleScene`。
5. Player Settings 中 Windows Standalone 相关设置符合预期。

注意：数据库已迁移到 `Application.persistentDataPath`，默认 Prompt 通过 `Application.streamingAssetsPath` 读取。构建或迁移到其他机器时，需要确保本地 `StreamingAssets` 配置文件和默认 Prompt 文件存在。

## 开发注意事项

### 路径与本地数据

当前路径策略：

- 数据库使用 `Path.Combine(Application.persistentDataPath, "iroha_ai.db")`。
- 默认角色 Prompt 使用 `Path.Combine(Application.streamingAssetsPath, "DefaultCharacterPrompt.json")`。
- API Key 使用本地 `Assets/StreamingAssets/config.json`。

`Application.persistentDataPath` 适合保存运行期数据库和用户数据；`StreamingAssets` 适合保存随项目提供的只读初始配置。当前 `.gitignore` 忽略了整个 `Assets/StreamingAssets/`，因此克隆仓库后需要本地补齐 `config.json` 和 `DefaultCharacterPrompt.json`。



### Windows API 限制

以下功能只在 Windows Standalone 下完整可用：

- 窗口无边框。
- 透明背景。
- 置顶。
- 点击穿透。
- 系统级窗口拖拽。
- 屏幕边缘吸附/隐藏。
- 前台窗口标题和进程名读取。

在 Unity Editor 中，部分逻辑被 `UNITY_STANDALONE_WIN` 或 `!UNITY_EDITOR` 条件限制，不一定完全表现为最终构建效果，需要构建运行之后查看结果。


## 已知问题与待确认事项

### 已知问题

- 项目主功能面向 Windows Standalone，暂不支持 macOS、Linux 或 Android。


## 维护建议

优先级较高的改进：

1. 发布客户端前评估是否改为后端代理，避免把共享 API Key 下发到用户机器。
2. 明确 Windows Standalone 是当前唯一支持平台。
3. 为数据库初始化、登录、角色管理、聊天记录管理添加基础测试。


