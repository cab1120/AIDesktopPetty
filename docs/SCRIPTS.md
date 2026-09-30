# 脚本导航

2026-09-30。路径默认相对 Assets/Scripts；签名以源码为准。

| 区域 | 入口 | 责任 |
| --- | --- | --- |
| DataBase | AppInitializer、AppLogService、DatabaseManager、DatabaseSchemaMigrator、DefaultDataInitializer | 启动、日志、六表版本迁移/备份、默认数据 |
| DataBase | AuthService、Data/GlobalSession、Data/SessionSnapshot | 登录、会话版本和请求身份快照 |
| Character/Conversation | ChatTurn、ConversationService | 一轮聊天的身份/消息 ID、串行队列、取消和结果提交 |
| DataBase/Data | UserData、Character、ChatMessage、Emotion、InteractionEvent、UserCharacterState | Data/Repository/Service，数据与业务规则 |
| Character/AI/AIChat | AIChat、ChatReplyResult | 配置、日志、活动请求取消、搜索/模型请求与普通回复结果 |
| 同上 | ChatContextBuilder、ChatContextTextBuilder | 消息数组与搜索上下文 |
| 同上 | SearchDecison/SearchDecisionService、SearchDecisonMode/SearchRuleFilter、SearchCacheService、SearchResultFormatter | 搜索决策与缓存；Decison 为现有拼写 |
| Character/AI/Prompt | CharacterPromptLoader、CharacterPromptBuilder、PromptContext、Emotion、IrohaPrompt | 配置驱动 Prompt、情绪和文本 |
| Character/AI/AutoTalk | AIContextReactionManager、ContextEvaluator | 主动气泡和筛选 |
| Character/UI | UIManager、MessageUI、BubbleUIManager、PetToggleUI | 聊天、排队气泡、会话历史重载、主动气泡、展开 |
| Character/UI/Layout | DesktopPetLayoutController、DesktopPetLayoutProfile、DesktopPetLayoutMode | 布局和窗口尺寸 |
| Character/UI/Login、ChatHistory、ControlPanel(UserCharactor) | PanelController / ListItem | 登录、历史、用户/角色管理 |
| Presentation/Window | BorderlessWindow、TransparentBackground、ClickThroughController、WindowDragHandler、WindowSnapController、WindowSizeController | 窗口表现、拖拽、吸附、尺寸 |
| Presentation | DesktopContextManager | 前台停留检测；从 Platform 获取上下文，不直接调用 user32 |
| Platform/Windows | WindowsPlatformBootstrap、IWindowService、WindowsWindowService、WindowsForegroundContextService、Native、Models | 窗口与前台平台服务/ABI/值类型 |
| Art/Scenes | SakuramachiSceneLoop、SakuramachiLoopTrack、SakuramachiLoopClip | 时间求值、Timeline |
| Art/Scenes/Editor | SakuramachiLoopSetup、SakuramachiLoopValidation | 配置和历史隔离工程验证；运行前检查路径和保存行为 |
| Shaders/Improved2.0/Editor | CharacterStyleAMaterials | A 材质切换/恢复 |
| Art/Skybox | SkyboxRotator | 天空旋转 |
| Assets/Dev/SakuraWeather/Runtime/Scripts | SakuraWeatherController（含 Debug partial）、SakuraWeatherProfile、SakuraOffsetConverter | VFX 配置与调试 |
| Tools、Test | PasswordHasher、TextMeshProMaxWidth、EmotionBuildDebugTest | 工具与调试；不是自动测试套件 |

SampleScene 挂载桌宠主链；3DScene 挂载循环/天空/樱花。Assets 下当前未发现 asmdef，安装 Test Framework 不等于已有完整测试。
