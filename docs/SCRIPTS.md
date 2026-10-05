# 逐文件脚本索引

核对日期：2026-10-05；代码基线 Git fbe7f08。共 143 份 C# 文件，范围是 Assets/Scripts 与 Assets/Dev/SakuraWeather；不逐项解释 Unity Package 或外部美术包自带代码。文件链接相对本目录，方法签名以源码为准。

先读 [架构](ARCHITECTURE.md)理解数据流，再用本表定位；具体接口在 [API_EVENTS.md](API_EVENTS.md)，后续任务拆分在 [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)。

## 怎样判断脚本是否在运行

SampleScene 序列化挂载了 AppInitializer、WindowsPlatformBootstrap、AIChat、ConversationService、UI、窗口表现、前台/气泡和 EmotionBuildDebugTest。Data/Repository/Service/Prompt 静态类由调用链使用，不能因为未挂场景就认定无效。SampleScene 还接入 M1 协调/资源/表现后端及入口导航；3DScene 已在构建列表，挂载 WorldRuntimeBindings、SakuramachiWorldBindings、循环、天空和 SakuraWeather，由本地后端 Additive 加载。Prefab 位于 Assets/Prefab。Editor 工具不进入 Player；默认脚本程序集仍使用，未发现自有 asmdef。

旧 IrohaPromptBuilder 未发现当前主链直接调用；IrohaPromptJsonExporter 未发现自动调用，调用会覆盖默认文件。情绪名称键与原文日志缺口见 [架构 E01–E04](ARCHITECTURE.md)，不要把本表的职责描述理解为全链已满足所有不变量。

## M1 世界生命周期、表现与验收

职责与设计原因详见 [M1 移交](M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md)。以下 24 份脚本为本阶段新增；SkyboxRotator、SceneLoop 和应用日志的调整也分别反映在原索引。

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Application/World/WorldState.cs](<../Assets/Scripts/Application/World/WorldState.cs>) | Desktop/Entering/Explore/Exiting；世界寿命与桌面 UI 布局状态分离 |
| [Application/World/WorldScope.cs](<../Assets/Scripts/Application/World/WorldScope.cs>) | 一次进入的 InstanceId、Definition、ExitRequested、句柄、桌面快照、运行绑定与原 Active Scene；退出仍需知道自己拥有何物 |
| [Application/World/WorldCoordinator.cs](<../Assets/Scripts/Application/World/WorldCoordinator.cs>) | 唯一进入/退出协调、状态迁移、过期结果处理、分阶段清理/重试与 StateChanged；UI 不直接加载场景 |
| [Application/World/WorldContentBindingsBehaviour.cs](<../Assets/Scripts/Application/World/WorldContentBindingsBehaviour.cs>) | 内容侧校验/激活/停用抽象；世界层不硬编码樱町内容 |
| [Application/World/Definition/WorldDefinition.cs](<../Assets/Scripts/Application/World/Definition/WorldDefinition.cs>) | 静态 WorldId、展示名、完整 ScenePath 与配置校验；不存运行句柄 |
| [Application/World/Definition/WorldCatalog.cs](<../Assets/Scripts/Application/World/Definition/WorldCatalog.cs>) | 世界配置集合与查询/校验；入口按 ID 选择世界 |
| [Application/World/Resource/IResourceService.cs](<../Assets/Scripts/Application/World/Resource/IResourceService.cs>) | 可加载检查、加载和释放协程契约；协调器不依赖具体资源来源 |
| [Application/World/Resource/WorldResourceServiceBehaviour.cs](<../Assets/Scripts/Application/World/Resource/WorldResourceServiceBehaviour.cs>) | MonoBehaviour 抽象实现接口，供 Inspector 序列化后端引用 |
| [Application/World/Resource/LocalSceneResourceService.cs](<../Assets/Scripts/Application/World/Resource/LocalSceneResourceService.cs>) | Build Settings 检查、Additive Load/Unload、释放幂等；当前后端是 Unity 本地场景，不是 YooAsset |
| [Application/World/Resource/WorldSceneHandle.cs](<../Assets/Scripts/Application/World/Resource/WorldSceneHandle.cs>) | 已加载场景及释放标记；退出必须释放原句柄而非猜场景名 |
| [Application/World/Resource/WorldSceneLoadResult.cs](<../Assets/Scripts/Application/World/Resource/WorldSceneLoadResult.cs>) | 成功句柄/失败原因；加载完成不能只靠协程结束猜测 |
| [Application/World/Resource/WorldSceneReleaseResult.cs](<../Assets/Scripts/Application/World/Resource/WorldSceneReleaseResult.cs>) | 释放成功/失败；明确失败后仍保留 Scope 所有权 |
| [Application/World/Presentation/DesktopPresentationSnapshot.cs](<../Assets/Scripts/Application/World/Presentation/DesktopPresentationSnapshot.cs>) | 进入前布局、窗口/显示器信息、相机/监听器及桌面行为启用状态；恢复实际进入前状态 |
| [Application/World/Presentation/WorldPresentationProfile.cs](<../Assets/Scripts/Application/World/Presentation/WorldPresentationProfile.cs>) | 世界窗口尺寸和当前显示器居中配置；不把 UI Layout 当世界状态 |
| [Application/World/Presentation/WorldPresentationController.cs](<../Assets/Scripts/Application/World/Presentation/WorldPresentationController.cs>) | Capture/Suspend/Bind/Activate/PrepareExit/Restore；统一窗口、输入、相机、音频控制权交接 |
| [Application/World/Presentation/WorldRuntimeBindings.cs](<../Assets/Scripts/Application/World/Presentation/WorldRuntimeBindings.cs>) | 场景提供相机、监听器、输入行为与内容入口；Additive 加载初期避免自动抢控制权 |
| [Application/World/Presentation/WorldRuntimeBindingsResolver.cs](<../Assets/Scripts/Application/World/Presentation/WorldRuntimeBindingsResolver.cs>) | 在目标 Scene 根对象中定位并验证绑定；不跨世界全局 Find |
| [Application/World/Presentation/WorldEntryButton.cs](<../Assets/Scripts/Application/World/Presentation/WorldEntryButton.cs>) | 只请求进入，按世界状态切换按钮可用性；不持有资源 |
| [Application/World/Presentation/WorldNavigationPanel.cs](<../Assets/Scripts/Application/World/Presentation/WorldNavigationPanel.cs>) | 世界返回导航及状态订阅/解绑；清理重试仍经协调器 |
| [Art/Scenes/SakuramachiWorldBindings.cs](<../Assets/Scripts/Art/Scenes/SakuramachiWorldBindings.cs>) | 校验并接入 Loop、Director、SakuraWeather、Skybox；内容停用时停止音频/演出并恢复 |
| [Test/World/ResourceServiceDebugHarness.cs](<../Assets/Scripts/Test/World/ResourceServiceDebugHarness.cs>) | 资源服务手动加载/释放验证入口；测试工具不作为业务所有者 |
| [Test/World/WorldCoordinatorDebugHarness.cs](<../Assets/Scripts/Test/World/WorldCoordinatorDebugHarness.cs>) | 手动验证状态机、进入/退出与异常路径 |
| [Test/World/FaultInjectingResourceService.cs](<../Assets/Scripts/Test/World/FaultInjectingResourceService.cs>) | 包装真实后端，注入加载/释放失败与完成延迟；证明失败路径不是靠运气执行 |
| [Test/World/WorldM1StressRunner.cs](<../Assets/Scripts/Test/World/WorldM1StressRunner.cs>) | 20 次往返、基线比较、耗时与内存采样、诊断清理；仅 Editor/Development 编译 |

## 数据库、身份与业务数据

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [DataBase/AppInitializer.cs](<../Assets/Scripts/DataBase/AppInitializer.cs>) | 应用启动协调：启动日志、数据库迁移、默认数据和情绪；StartupError 汇总初始化异常，退出关闭连接与日志。 |
| [DataBase/AppLogService.cs](<../Assets/Scripts/DataBase/AppLogService.cs>) | 应用级唯一订阅/解绑；恢复旧 AIChat.RunLog 的本地时间、类型、Debug 原文与 Error/Exception 堆栈；追加到项目根目录/Player exe 旁的 run_log.txt，无指纹/轮转。 |
| [DataBase/AuthService.cs](<../Assets/Scripts/DataBase/AuthService.cs>) | 检查启动错误、账号密码和角色归属，设置激活角色及 GlobalSession，初始化关系；不负责网络聊天。 |
| [DataBase/Data/Character/CharacterProfileData.cs](<../Assets/Scripts/DataBase/Data/Character/CharacterProfileData.cs>) | CharacterProfile 模型：CharacterId/UserId、展示名、PromptJson、启用和时间。 |
| [DataBase/Data/Character/CharacterRepository.cs](<../Assets/Scripts/DataBase/Data/Character/CharacterRepository.cs>) | 稳定 UserId 角色查询；有效归属过滤、隔离隐藏；新增/编辑/激活/事务级联与保留角色保护。 |
| [DataBase/Data/ChatMessage/ChatMessageData.cs](<../Assets/Scripts/DataBase/Data/ChatMessage/ChatMessageData.cs>) | ChatMessage 持久化模型：消息 ID、归属、发送方、内容和实际保存时间。 |
| [DataBase/Data/ChatMessage/ChatMessageRepository.cs](<../Assets/Scripts/DataBase/Data/ChatMessage/ChatMessageRepository.cs>) | 底层插入、最近/排除指定消息查询、搜索、批量删除与历史裁剪；不管理 Turn 生命周期。 |
| [DataBase/Data/ChatMessage/ChatMessageSearchCondition.cs](<../Assets/Scripts/DataBase/Data/ChatMessage/ChatMessageSearchCondition.cs>) | 历史筛选 DTO：归属、发送方、关键词及时间等条件。 |
| [DataBase/Data/ChatMessage/ChatMessageService.cs](<../Assets/Scripts/DataBase/Data/ChatMessage/ChatMessageService.cs>) | 捕获/接受快照的消息用例；提交前 IsCurrent，插入与 100 条裁剪事务；旧字符串写入入口仅兼容。 |
| [DataBase/Data/Emotion/EmotionDataMapper.cs](<../Assets/Scripts/DataBase/Data/Emotion/EmotionDataMapper.cs>) | EmotionData 与 EmotionRecord 的双向转换。 |
| [DataBase/Data/Emotion/EmotionRecord.cs](<../Assets/Scripts/DataBase/Data/Emotion/EmotionRecord.cs>) | EmotionState 表模型：归属与情绪/生理/强度/时间等持久化字段。 |
| [DataBase/Data/Emotion/SQLiteEmotionStorage.cs](<../Assets/Scripts/DataBase/Data/Emotion/SQLiteEmotionStorage.cs>) | IEmotionStorage 及 SQLite 实现；读最新/历史、保存、默认保留 20 条、按归属删除；调用者仍有名称键缺口。 |
| [DataBase/Data/GlobalSession.cs](<../Assets/Scripts/DataBase/Data/GlobalSession.cs>) | 私有可变当前身份；CaptureSnapshot/IsCurrent；登录、退出、身份资料变化推进版本并广播失效。 |
| [DataBase/Data/InteractionEvent/InteractionEventData.cs](<../Assets/Scripts/DataBase/Data/InteractionEvent/InteractionEventData.cs>) | InteractionEvent 表模型：类型、来源、上下文键、描述、影响与时间。 |
| [DataBase/Data/InteractionEvent/InteractionEventRepository.cs](<../Assets/Scripts/DataBase/Data/InteractionEvent/InteractionEventRepository.cs>) | 事件插入、近期同上下文判断、每用户/角色最多 300 条裁剪。 |
| [DataBase/Data/InteractionEvent/InteractionEventService.cs](<../Assets/Scripts/DataBase/Data/InteractionEvent/InteractionEventService.cs>) | 同上下文 10 分钟冷却、前台过滤和事件用例；快照校验、部分信任/关系变化；原始标题/回复可入库。 |
| [DataBase/Data/InteractionEvent/InteractionEventType.cs](<../Assets/Scripts/DataBase/Data/InteractionEvent/InteractionEventType.cs>) | 主动气泡与桌宠展开/收起/拖拽的事件类型常量。 |
| [DataBase/Data/SessionSnapshot.cs](<../Assets/Scripts/DataBase/Data/SessionSnapshot.cs>) | readonly struct：请求时用户/角色 ID、名称、权限、SessionVersion；不可变身份参数。 |
| [DataBase/Data/UserCharacterState/RelationshipService.cs](<../Assets/Scripts/DataBase/Data/UserCharacterState/RelationshipService.cs>) | 登录、发送、回复成功、展开等业务规则；向 Prompt 提供关系描述；异步使用快照重载。 |
| [DataBase/Data/UserCharacterState/UserCharacterStateData.cs](<../Assets/Scripts/DataBase/Data/UserCharacterState/UserCharacterStateData.cs>) | 用户-角色关系状态模型：关系值、信任、互动天数与衰减时间等。 |
| [DataBase/Data/UserCharacterState/UserCharacterStateRepository.cs](<../Assets/Scripts/DataBase/Data/UserCharacterState/UserCharacterStateRepository.cs>) | 按 ID 获取/创建关系，更新关系/信任/天数、衰减、上下限和组合状态 ID。 |
| [DataBase/Data/UserData/UserData.cs](<../Assets/Scripts/DataBase/Data/UserData/UserData.cs>) | User 表模型：稳定用户 ID、名称、密码哈希、角色/权限和时间。 |
| [DataBase/Data/UserData/UserRepository.cs](<../Assets/Scripts/DataBase/Data/UserData/UserRepository.cs>) | 用户查询/增改删；创建与默认角色事务、改名展示字段同步、事务级联及当前/默认用户保护。 |
| [DataBase/DatabaseManager.cs](<../Assets/Scripts/DataBase/DatabaseManager.cs>) | 持有应用 SQLiteConnection；连接 persistentDataPath/iroha_ai.db，先备份/迁移再 CreateTable；退出关闭。 |
| [DataBase/DatabaseSchemaMigrator.cs](<../Assets/Scripts/DataBase/DatabaseSchemaMigrator.cs>) | schema v0→v1：回填角色 UserId、标记无法认领的角色、校验、事务提交版本；旧库备份及 WAL 防误备份。 |
| [DataBase/DefaultDataInitializer.cs](<../Assets/Scripts/DataBase/DefaultDataInitializer.cs>) | 验证默认 Prompt 文件；事务创建 DefaultUser/DefaultCharacter，已有角色不会被文件覆盖。 |
| [DataBase/SQLite.cs](<../Assets/Scripts/DataBase/SQLite.cs>) | SQLite4Unity3d/SQLite-net 兼容与 ORM 实现，包括 Connection、表映射、事务、底层 sqlite3 ABI；第三方基础设施，不是业务规则。 |

## 普通聊天用例

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Character/Conversation/ChatTurn.cs](<../Assets/Scripts/Character/Conversation/ChatTurn.cs>) | 不可变一次输入：TurnId、UserMessageId、输入、快照、创建时间；不是可变聊天历史。 |
| [Character/Conversation/ConversationService.cs](<../Assets/Scripts/Character/Conversation/ConversationService.cs>) | 普通聊天 TrySend/FIFO；轮到执行才存输入；调用 AIChat、提交成功结果，广播开始/成功/失败/取消并清队列。 |

## 模型请求与搜索

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Character/AI/AIChat/AIChat.cs](<../Assets/Scripts/Character/AI/AIChat/AIChat.cs>) | 加载本地配置，编排搜索/Prompt/模型；统一 HTTP 超时/Abort/Dispose；普通 ChatReplyResult 与独立主动气泡请求。 |
| [Character/AI/AIChat/ChatContextBuilder.cs](<../Assets/Scripts/Character/AI/AIChat/ChatContextBuilder.cs>) | 模型 messages：system + 最近 8 条历史（排除本轮 UserMessageId）+ 当前输入一次。 |
| [Character/AI/AIChat/ChatContextTextBuilder.cs](<../Assets/Scripts/Character/AI/AIChat/ChatContextTextBuilder.cs>) | 将最近历史转为搜索决策/Prompt 的文本，可排除本轮消息。 |
| [Character/AI/AIChat/ChatReplyParser.cs](<../Assets/Scripts/Character/AI/AIChat/ChatReplyParser.cs>) | 将成功 HTTP 的 choices[0].message.content 转为结果，空内容/坏 JSON 失败；本身不判 HTTP 状态。 |
| [Character/AI/AIChat/ChatReplyResult.cs](<../Assets/Scripts/Character/AI/AIChat/ChatReplyResult.cs>) | 普通回复 Success/Failure/Cancelled 与失败原因的数据契约；文本只属于成功结果。 |
| [Character/AI/AIChat/SearchDecison/SearchDecision.cs](<../Assets/Scripts/Character/AI/AIChat/SearchDecison/SearchDecision.cs>) | 搜索决策 DTO：是否搜索、关键词、原因。 |
| [Character/AI/AIChat/SearchDecison/SearchDecisionService.cs](<../Assets/Scripts/Character/AI/AIChat/SearchDecison/SearchDecisionService.cs>) | 规则优先，必要时调用模型判断并解析 JSON；无效决策返回 null，不能降级为明确不搜索。 |
| [Character/AI/AIChat/SearchDecisonMode/SearchCacheEntry.cs](<../Assets/Scripts/Character/AI/AIChat/SearchDecisonMode/SearchCacheEntry.cs>) | 搜索缓存 DTO：用户/角色/版本、关键词、结果、原因和时间。 |
| [Character/AI/AIChat/SearchDecisonMode/SearchCacheService.cs](<../Assets/Scripts/Character/AI/AIChat/SearchDecisonMode/SearchCacheService.cs>) | 内存搜索缓存：按身份/版本隔离，最多 5 条、15 分钟；话题关联匹配、非空写入及清理。 |
| [Character/AI/AIChat/SearchDecisonMode/SearchDecisionMode.cs](<../Assets/Scripts/Character/AI/AIChat/SearchDecisonMode/SearchDecisionMode.cs>) | 规则判断枚举：不搜索、直接搜索、交由模型判断。 |
| [Character/AI/AIChat/SearchDecisonMode/SearchRuleFilter.cs](<../Assets/Scripts/Character/AI/AIChat/SearchDecisonMode/SearchRuleFilter.cs>) | 识别日常陪伴、显式查询和疑似实时/未知事实输入。 |
| [Character/AI/AIChat/SearchResultFormatter.cs](<../Assets/Scripts/Character/AI/AIChat/SearchResultFormatter.cs>) | 将搜索结果和决策整理为 Prompt 的实时信息块。 |

## 角色 Prompt 与情绪

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Character/AI/Prompt/CharacterPromptBuilder.cs](<../Assets/Scripts/Character/AI/Prompt/CharacterPromptBuilder.cs>) | 当前通用 Prompt 主链：Profile + 快照 + 运行上下文；区分普通聊天和主动气泡任务。 |
| [Character/AI/Prompt/CharacterPromptLoader.cs](<../Assets/Scripts/Character/AI/Prompt/CharacterPromptLoader.cs>) | 按快照 CharacterId 从数据库加载角色 PromptJson；解析为 Profile 并补默认值。 |
| [Character/AI/Prompt/CharacterPromptProfile.cs](<../Assets/Scripts/Character/AI/Prompt/CharacterPromptProfile.cs>) | 角色 JSON 配置结构：人格、世界观、说话风格、禁止项、聊天/气泡/实时规则。 |
| [Character/AI/Prompt/Emotion/EmotionData.cs](<../Assets/Scripts/Character/AI/Prompt/Emotion/EmotionData.cs>) | 运行时情绪/生理/强度/持续时间/归属 DTO，包含日期转换。 |
| [Character/AI/Prompt/Emotion/EmotionGenerator.cs](<../Assets/Scripts/Character/AI/Prompt/Emotion/EmotionGenerator.cs>) | 按时间、工作日/周末和生理状态权重生成情绪；不是模型情绪分析。 |
| [Character/AI/Prompt/Emotion/EmotionMemory.cs](<../Assets/Scripts/Character/AI/Prompt/Emotion/EmotionMemory.cs>) | 静态当前情绪缓存、加载/过期/再生成/保存/调试重置；默认与调用仍混用名称键，见 E01。 |
| [Character/AI/Prompt/Emotion/EmotionType.cs](<../Assets/Scripts/Character/AI/Prompt/Emotion/EmotionType.cs>) | 情绪类型枚举。 |
| [Character/AI/Prompt/Emotion/IrohaEmotionPromptBuilder.cs](<../Assets/Scripts/Character/AI/Prompt/Emotion/IrohaEmotionPromptBuilder.cs>) | 把 EmotionData 转为角色表达与情绪强度约束文字。 |
| [Character/AI/Prompt/Emotion/IrohaProhibitedItems.cs](<../Assets/Scripts/Character/AI/Prompt/Emotion/IrohaProhibitedItems.cs>) | 默认角色禁止行为/表达的文字模板。 |
| [Character/AI/Prompt/Emotion/IrohaStatusContext.cs](<../Assets/Scripts/Character/AI/Prompt/Emotion/IrohaStatusContext.cs>) | 按日期和时间计算长期心境与生理强度。 |
| [Character/AI/Prompt/IrohaPrompt/IrohaBubblePrompt.cs](<../Assets/Scripts/Character/AI/Prompt/IrohaPrompt/IrohaBubblePrompt.cs>) | 默认主动搭话规则模板。 |
| [Character/AI/Prompt/IrohaPrompt/IrohaCorePersonality.cs](<../Assets/Scripts/Character/AI/Prompt/IrohaPrompt/IrohaCorePersonality.cs>) | 默认彩叶人格文字模板，供默认 JSON 导出/旧构建器使用。 |
| [Character/AI/Prompt/IrohaPrompt/IrohaMemoryContext.cs](<../Assets/Scripts/Character/AI/Prompt/IrohaPrompt/IrohaMemoryContext.cs>) | 把输入记忆文本格式化为旧角色 Prompt 区块；不提供向量/长期记忆检索。 |
| [Character/AI/Prompt/IrohaPrompt/IrohaPromptBuilder.cs](<../Assets/Scripts/Character/AI/Prompt/IrohaPrompt/IrohaPromptBuilder.cs>) | 旧专用角色 Prompt 拼装器；当前 AIChat 主链用 CharacterPromptBuilder，未发现其直接调用。 |
| [Character/AI/Prompt/IrohaPrompt/IrohaPromptJsonExporter.cs](<../Assets/Scripts/Character/AI/Prompt/IrohaPrompt/IrohaPromptJsonExporter.cs>) | 将默认模板写到 StreamingAssets/DefaultCharacterPrompt.json；调用会覆盖文件，当前未发现自动调用。 |
| [Character/AI/Prompt/IrohaPrompt/IrohaRealtimeContext.cs](<../Assets/Scripts/Character/AI/Prompt/IrohaPrompt/IrohaRealtimeContext.cs>) | 旧构建器的聊天/气泡实时信息模板。 |
| [Character/AI/Prompt/IrohaPrompt/IrohaWorldView.cs](<../Assets/Scripts/Character/AI/Prompt/IrohaPrompt/IrohaWorldView.cs>) | 默认世界观文字模板。 |
| [Character/AI/Prompt/PromptContext.cs](<../Assets/Scripts/Character/AI/Prompt/PromptContext.cs>) | 一次 Prompt 的运行上下文 DTO：时间、搜索、记忆文本、情绪与关系。 |

## 主动气泡

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Character/AI/AutoTalk/AIContextReactionManager.cs](<../Assets/Scripts/Character/AI/AutoTalk/AIContextReactionManager.cs>) | 订阅前台事件，快照、3 秒默认全局冷却、请求锁；独立气泡协程、IGNORE 处理、显示与事件；停用取消请求。 |
| [Character/AI/AutoTalk/ContextEvaluator.cs](<../Assets/Scripts/Character/AI/AutoTalk/ContextEvaluator.cs>) | 前台进程/标题黑名单过滤。 |

## 界面与布局

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Character/UI/BubbleUIManager.cs](<../Assets/Scripts/Character/UI/BubbleUIManager.cs>) | 主动气泡文字/布局/默认 5 秒隐藏；会话失效隐藏旧气泡。 |
| [Character/UI/Button/CustomButtonClicker.cs](<../Assets/Scripts/Character/UI/Button/CustomButtonClicker.cs>) | 按钮悬停/按压视觉与供拖拽脚本调用的 PerformClick。 |
| [Character/UI/ChatHistory/ChatHistoryItemUI.cs](<../Assets/Scripts/Character/UI/ChatHistory/ChatHistoryItemUI.cs>) | 单条历史文本/选择状态/点击显示。 |
| [Character/UI/ChatHistory/ChatHistoryPanel.cs](<../Assets/Scripts/Character/UI/ChatHistory/ChatHistoryPanel.cs>) | 历史列表、最近记录、条件搜索、选择与删除；用当前用户/角色查询。 |
| [Character/UI/ControlPanel(UserCharactor)/CharacterManagePanel/CharacterListItem.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/CharacterManagePanel/CharacterListItem.cs>) | 角色列表 Prefab 的展示和选择回传。 |
| [Character/UI/ControlPanel(UserCharactor)/CharacterManagePanel/CharacterManagePanelController.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/CharacterManagePanel/CharacterManagePanelController.cs>) | 按权限全局或用户角色列表，筛选/选择/增改删；隔离角色由仓储隐藏。 |
| [Character/UI/ControlPanel(UserCharactor)/CharacterManagePanel/CharacterModifyPanelController.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/CharacterManagePanel/CharacterModifyPanelController.cs>) | 新增/编辑角色、载入 JSON 与仓储提交。 |
| [Character/UI/ControlPanel(UserCharactor)/ControlPanelController.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/ControlPanelController.cs>) | 控制面板在用户/角色管理与返回之间切换。 |
| [Character/UI/ControlPanel(UserCharactor)/DesktopPetPanelController.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/DesktopPetPanelController.cs>) | 桌宠管理入口、按权限显示按钮与切换管理布局。 |
| [Character/UI/ControlPanel(UserCharactor)/UserManagePanel/UserListItem.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/UserManagePanel/UserListItem.cs>) | 用户列表 Prefab 的数据展示和选择回传。 |
| [Character/UI/ControlPanel(UserCharactor)/UserManagePanel/UserManagePanelController.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/UserManagePanel/UserManagePanelController.cs>) | 用户列表、筛选、选中、增改删入口与刷新。 |
| [Character/UI/ControlPanel(UserCharactor)/UserManagePanel/UserModifyPanelController.cs](<../Assets/Scripts/Character/UI/ControlPanel(UserCharactor)/UserManagePanel/UserModifyPanelController.cs>) | 新增/编辑用户表单与仓储调用。 |
| [Character/UI/Layout/DesktopPetLayoutController.cs](<../Assets/Scripts/Character/UI/Layout/DesktopPetLayoutController.cs>) | 布局 Profile 字典、CanvasScaler 与窗口逻辑尺寸协调；ApplyLayout 返回 bool。 |
| [Character/UI/Layout/DesktopPetLayoutMode.cs](<../Assets/Scripts/Character/UI/Layout/DesktopPetLayoutMode.cs>) | 登录、折叠、聊天和管理布局模式枚举，不是世界状态。 |
| [Character/UI/Layout/DesktopPetLayoutProfile.cs](<../Assets/Scripts/Character/UI/Layout/DesktopPetLayoutProfile.cs>) | 每种模式的 ScriptableObject：逻辑尺寸、Canvas/缩放等布局参数。 |
| [Character/UI/Login/LoginPanelController.cs](<../Assets/Scripts/Character/UI/Login/LoginPanelController.cs>) | 启动登录布局、预填开发账号；显示 StartupError，调用 AuthService，成功切桌宠 UI。 |
| [Character/UI/MessageUI.cs](<../Assets/Scripts/Character/UI/MessageUI.cs>) | 聊天消息 Prefab 的 TMP 文本引用。 |
| [Character/UI/PetToggleUI.cs](<../Assets/Scripts/Character/UI/PetToggleUI.cs>) | 折叠/展开聊天与功能按钮、退出；应用布局，记录展开/收起和关系事件。 |
| [Character/UI/UIManager.cs](<../Assets/Scripts/Character/UI/UIManager.cs>) | 输入交给 ConversationService；消息气泡、队列气泡表、终态监听、切会话历史重载及布局滚动。 |

## 桌面表现与前台检测

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Presentation/DesktopContextManager.cs](<../Assets/Scripts/Presentation/DesktopContextManager.cs>) | 定期从平台读取前台标题/进程；默认 1 秒检查、5 秒停留与重复抑制后发 OnWindowChanged。 |
| [Presentation/Window/BorderlessWindow.cs](<../Assets/Scripts/Presentation/Window/BorderlessWindow.cs>) | 经 IWindowService 开关无边框。 |
| [Presentation/Window/ClickThroughController.cs](<../Assets/Scripts/Presentation/Window/ClickThroughController.cs>) | 按鼠标/界面判定经服务开关整个窗口点击穿透。 |
| [Presentation/Window/TransparentBackground.cs](<../Assets/Scripts/Presentation/Window/TransparentBackground.cs>) | 经服务设置 DWM 透明支持及相机背景；实际透明需 Player。 |
| [Presentation/Window/WindowDragHandler.cs](<../Assets/Scripts/Presentation/Window/WindowDragHandler.cs>) | 区分点击/拖拽，调用原生 BeginWindowDrag；结束通知吸附并记录互动。 |
| [Presentation/Window/WindowSizeController.cs](<../Assets/Scripts/Presentation/Window/WindowSizeController.cs>) | 启动经平台服务调整窗口尺寸/位置；需要初始化成功。 |
| [Presentation/Window/WindowSnapController.cs](<../Assets/Scripts/Presentation/Window/WindowSnapController.cs>) | 拖拽完成后按当前显示器工作区吸附，管理边缘隐藏/回到可见区域。 |

## Windows 平台与 ABI

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Platform/Windows/IWindowService.cs](<../Assets/Scripts/Platform/Windows/IWindowService.cs>) | 业务窗口能力契约：bool 写入、Try 查询；不暴露 HWND/Win32，区分逻辑尺寸与物理边界。 |
| [Platform/Windows/Models/WindowLogicalSize.cs](<../Assets/Scripts/Platform/Windows/Models/WindowLogicalSize.cs>) | 业务逻辑窗口宽高；由服务做 DPI 换算。 |
| [Platform/Windows/Models/WindowMonitorInfo.cs](<../Assets/Scripts/Platform/Windows/Models/WindowMonitorInfo.cs>) | 业务显示器 bounds/work area/DPI/DpiScale/主屏信息。 |
| [Platform/Windows/Models/WindowPoint.cs](<../Assets/Scripts/Platform/Windows/Models/WindowPoint.cs>) | 业务桌面物理整数坐标。 |
| [Platform/Windows/Models/WindowRect.cs](<../Assets/Scripts/Platform/Windows/Models/WindowRect.cs>) | 业务物理矩形与计算宽高。 |
| [Platform/Windows/Native/NativeApiVersion.cs](<../Assets/Scripts/Platform/Windows/Native/NativeApiVersion.cs>) | 原生 Major/Minor/Patch 的顺序布局 DTO。 |
| [Platform/Windows/Native/NativeCapability.cs](<../Assets/Scripts/Platform/Windows/Native/NativeCapability.cs>) | 原生窗口能力位枚举。 |
| [Platform/Windows/Native/NativeErrorDomain.cs](<../Assets/Scripts/Platform/Windows/Native/NativeErrorDomain.cs>) | 原生失败来源/错误域枚举。 |
| [Platform/Windows/Native/NativeMonitorInfo.cs](<../Assets/Scripts/Platform/Windows/Native/NativeMonitorInfo.cs>) | ABI 显示器 bounds、work area、DPI、主屏标识的顺序布局。 |
| [Platform/Windows/Native/NativePoint.cs](<../Assets/Scripts/Platform/Windows/Native/NativePoint.cs>) | ABI 整数坐标。 |
| [Platform/Windows/Native/NativeRect.cs](<../Assets/Scripts/Platform/Windows/Native/NativeRect.cs>) | ABI Left/Top/Right/Bottom 矩形及尺寸。 |
| [Platform/Windows/Native/WindowsForegroundNativeMethods.cs](<../Assets/Scripts/Platform/Windows/Native/WindowsForegroundNativeMethods.cs>) | user32 前台窗口/标题/进程 ID 的 P/Invoke 边界。 |
| [Platform/Windows/Native/WindowsNativeMethods.cs](<../Assets/Scripts/Platform/Windows/Native/WindowsNativeMethods.cs>) | DesktopPet.Native.Windows DLL 的 DP_* ABI 导入，窗口 HWND 只留在平台实现。 |
| [Platform/Windows/WindowsForegroundContextService.cs](<../Assets/Scripts/Platform/Windows/WindowsForegroundContextService.cs>) | Windows 前台标题/进程服务及 ForegroundContext；业务只拿普通字符串。 |
| [Platform/Windows/WindowsPlatformBootstrap.cs](<../Assets/Scripts/Platform/Windows/WindowsPlatformBootstrap.cs>) | 执行顺序 -10000；初始化并常驻单一窗口服务，Start 输出 ABI/能力/错误诊断。 |
| [Platform/Windows/WindowsWindowService.cs](<../Assets/Scripts/Platform/Windows/WindowsWindowService.cs>) | Windows Player 实现；ABI/能力校验、DLL 调用、尺寸/DPI/工作区处理和错误域诊断。 |

## 站台、天空与材质

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Art/Scenes/Editor/SakuramachiLoopSetup.cs](<../Assets/Scripts/Art/Scenes/Editor/SakuramachiLoopSetup.cs>) | Editor 配置场景循环与生成 Timeline/资源；可能保存/覆盖资源，先核对现有绑定。 |
| [Art/Scenes/Editor/SakuramachiLoopValidation.cs](<../Assets/Scripts/Art/Scenes/Editor/SakuramachiLoopValidation.cs>) | 隔离项目的 Editor/batch 检查和播放验证入口；路径/写入行为需先核对，历史结果不代表当前场景通过。 |
| [Art/Scenes/SakuramachiLoopClip.cs](<../Assets/Scripts/Art/Scenes/SakuramachiLoopClip.cs>) | Timeline PlayableAsset 与片段能力，创建循环 Playable。 |
| [Art/Scenes/SakuramachiLoopTrack.cs](<../Assets/Scripts/Art/Scenes/SakuramachiLoopTrack.cs>) | Timeline Track 与 Mixer，将时间/速度交给场景循环，处理播放与恢复。 |
| [Art/Scenes/SakuramachiSceneLoop.cs](<../Assets/Scripts/Art/Scenes/SakuramachiSceneLoop.cs>) | 以统一时间直接求列车/栏杆/六灯状态，裁剪、曲线、运动音频和原状态恢复；现行 180 秒循环所有者；增加 Editor/Development 验收观察。 |
| [Art/Skybox/SkyboxRotator.cs](<../Assets/Scripts/Art/Skybox/SkyboxRotator.cs>) | 世界内容显式 TryActivate/Deactivate；实例化并旋转天空材质，停用/销毁恢复并释放，防止 Additive 未提交就改全局天空。 |
| [Shaders/Basic/SetFaceMaterialHeadVector.cs](<../Assets/Scripts/Shaders/Basic/SetFaceMaterialHeadVector.cs>) | 把角色头部方向传给面部 Shader 以计算阴影；不能假设所有当前材质都使用它。 |
| [Shaders/Improved2.0/Editor/CharacterStyleAMaterials.cs](<../Assets/Scripts/Shaders/Improved2.0/Editor/CharacterStyleAMaterials.cs>) | Editor A 版材质创建、切换/恢复工具；保留当前 Ramp/纹理/GUID。 |

## 樱花 VFX

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Assets/Dev/SakuraWeather/Runtime/Scripts/Controllers/SakuraWeatherController.cs](<../Assets/Dev/SakuraWeather/Runtime/Scripts/Controllers/SakuraWeatherController.cs>) | 三层樱花 VFX：引用初始化、接口校验、Profile 应用、随相机更新区域和密度。 |
| [Assets/Dev/SakuraWeather/Runtime/Scripts/Data/SakuraWeatherProfile.cs](<../Assets/Dev/SakuraWeather/Runtime/Scripts/Data/SakuraWeatherProfile.cs>) | ScriptableObject 与 SakuraLayerSettings：分层密度、范围、偏移、VFX 参数/质量配置。 |
| [Assets/Dev/SakuraWeather/Runtime/Scripts/Debug/SakuraOffsetConverter.cs](<../Assets/Dev/SakuraWeather/Runtime/Scripts/Debug/SakuraOffsetConverter.cs>) | 调试工具：将目标相对相机的位置转为层偏移，打印配置值。 |
| [Assets/Dev/SakuraWeather/Runtime/Scripts/Debug/SakuraWeatherController.Debug.cs](<../Assets/Dev/SakuraWeather/Runtime/Scripts/Debug/SakuraWeatherController.Debug.cs>) | 同一 partial Controller 的 Gizmo、运行信息与质量调试入口；不是另一运行控制器。 |

## 工具、调试与自动测试

| 文件 | 含义、调用关系与限制 |
| --- | --- |
| [Editor/M0DatabaseTests.cs](<../Assets/Scripts/Editor/M0DatabaseTests.cs>) | Editor NUnit 数据/会话与模型协议测试，临时数据库、迁移/隔离/改名/级联/回滚/会话失效；共 12 项历史验证。 |
| [Test/EmotionBuildDebugTest.cs](<../Assets/Scripts/Test/EmotionBuildDebugTest.cs>) | SampleScene 挂载的 F9–F12 情绪读写/删除调试；使用默认名称键，不是隔离自动测试，勿在真实库调试数据清理。 |
| [Tools/PasswordHasher.cs](<../Assets/Scripts/Tools/PasswordHasher.cs>) | 当前密码 SHA256 哈希与比较，未加盐/成本参数；安全升级须兼容旧哈希。 |
| [Tools/TextMeshProMaxWidth.cs](<../Assets/Scripts/Tools/TextMeshProMaxWidth.cs>) | 限制 TMP 文本布局最大宽度，配合消息气泡自适应。 |

## 反向定位常见需求

修改输入/回复顺序先看 ConversationService；修改角色设定先看 CharacterPromptProfile/Loader/Builder；修改归属与删除先看 UserRepository/CharacterRepository/Migrator；修改情绪先核对 E01；修改窗口先看 IWindowService，再找 Presentation 消费者；修改站台先看循环说明和已实现 WorldScope 与内容绑定。不要把旧字符串重载或专用 Iroha 构建器作为新异步功能的入口。
