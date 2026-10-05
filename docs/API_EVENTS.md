# 当前接口、事件与调用约束

核对日期：2026-10-05，代码基线 Git fbe7f08。这里只列现有契约；参数/返回以源码为准。文件定位见 [SCRIPTS.md](SCRIPTS.md)，责任/异常缺口见 [ARCHITECTURE.md](ARCHITECTURE.md)。IResourceService、WorldScope.InstanceId 已实现；IWorldService、ActionQueue 和 OnTrainStop 仍是待设计内容。

## 启动、数据与身份

| 接口 | 输入/输出与责任 | 调用约束 |
| --- | --- | --- |
| AppInitializer.StartupError | 初始化异常消息，成功为 null | 登录页/AuthService 据此拦截；缺模型配置和初始化错误不是同一门槛 |
| DatabaseManager.Initialize / Close | 连接 persistentDataPath/iroha_ai.db；先备份旧版本、迁移/校验、建六表；关闭清空 Connection | Initialize 可重复；应用级共享连接，离开世界不能随意 Close |
| DatabaseSchemaMigrator.BackupBeforeMigration(path) / Migrate(db) | 当前版本 1；备份旧库，事务回填角色 UserId、登记孤儿隔离、校验版本 | WAL 文件使简单备份失败；更高版本拒绝；不拿实际运行库实验 |
| DefaultDataInitializer.Initialize | 验证默认 Prompt 非空，事务创建开发默认数据 | 不覆盖已有角色 Prompt；固定管理员是用户延期的风险 |
| AuthService.Login(userName,password,characterName,out error) | bool；查用户/密码、稳定 UserId 查角色、激活、建立会话/关系 | 无法登录隔离角色；整个登录并非单事务 |
| GlobalSession.CaptureSnapshot() | 返回 readonly SessionSnapshot | 跨 yield 的请求入口捕获并传递，不能改用兼容接口重新捕获 |
| GlobalSession.IsCurrent(snapshot) | bool，验证有效登录、UserId/CharacterId/SessionVersion | 提交与显示前验证；名字是快照内容，版本变更保证名字过期失效 |
| GlobalSession.SetSession / Clear | 建立或退出会话 | 同账号重新登录也使旧请求失效 |
| GlobalSession.SetCurrentCharacter / RefreshCurrentUser / RefreshCurrentCharacterFromDatabase | 当前登录身份匹配才刷新角色/资料，必要时推进版本 | 管理其他用户角色不能改变当前登录会话；Current* 读取字段已私有 |
| GlobalSession.IsAdmin / IsUser / IsGuest | 同步权限判断 | 不把 UI 隐藏按钮当成完整安全/授权体系 |
| UserRepository.UpdateUser / DeleteUser 等 | 用户仓储；改名同步角色镜像，删除事务级联 | 默认/当前用户有保护；error 用于失败展示，事务成功后刷新 |
| CharacterRepository.GetByUserAndName / GetByUserName / GetActiveCharacter | 从名字解析现存 UserId，再查询 | 无用户立即为空，不能以空 ID 匹配孤儿 |
| CharacterRepository.GetAll / SearchByCharacterName / GetById | 只返回与现存用户归属关联的角色 | 隔离记录留在原表但不可展示/编辑/删除；无认领 API |
| CharacterRepository.SetActiveCharacter / AddCharacter / UpdateCharacter / DeleteCharacter | bool/out error；激活/删除在事务内维护角色约束 | 至少保留角色/激活角色；当前角色先切换再删除 |
| ChatMessageService.SaveUserMessage(turn) / SaveAssistantMessage(snapshot,text) | 返回已保存 ChatMessageData，失败/无效快照为 null | 写入+上限 100 条裁剪事务；旧字符串重载仅同步兼容 |
| ChatMessageRepository.GetRecentMessagesExcluding | 按 UserId/CharacterId，排除 MessageId 后取最近历史 | 当前输入去重依赖该契约；历史排序/排重由 Builder 整理 |
| RelationshipService 的快照重载 | 按请求身份写关系或构建描述 | 同步重载可捕获当前身份；异步不得用它替代已捕获快照 |
| EmotionMemory.GetCurrentEmotion / SQLiteEmotionStorage | 参数名为 userId/characterId，返回/持久化情绪 | **现有调用仍传名称，属于 E01；不可宣称已满足稳定归属** |

LegacyUnclaimedCharacter 是迁移辅助 SQL 表，没有独立 C# Data 或认领服务。维护时不能只移除标记或给角色补一个猜测 ID；归属、关联记录和标记需要可信证据与专门事务。

## 普通聊天的命令与观察事件

| 接口/事件 | 参数与时机 | 消费者责任 |
| --- | --- | --- |
| ConversationService.TrySend(input,out turn) | bool 表示已接受；创建 ChatTurn 并排队 | UI 可以显示暂存气泡，true 不代表消息已入库或回复成功 |
| IsProcessing / PendingCount / IsPending(turn) | 当前 Turn 状态与等待数量 | 仅观察队列；UI 不直接执行请求或持久化 |
| TurnStarted | Action<ChatTurn>；用户消息持久化成功后 | UI 将暂存气泡变正式，开始事件不是单纯“已出队” |
| AssistantReplyReady | Action<ChatTurn,string>；有效会话、Assistant 保存和成功关系步骤后 | UI 显示文本；E04 的关系异常仍可能阻断该事件 |
| TurnFailed | Action<ChatTurn,string>；正常失败分支，内部先清 currentTurn | UI 移除尚未落库气泡、显示临时失败提示；不保存错误为 Assistant |
| TurnCancelled | Action<ChatTurn>；换会话、停用或取消 | 移除暂存气泡；已落库用户消息仍属于旧角色 |
| GlobalSession.SessionVersionChanged | Action<long>；先更新身份/版本再广播 | 各订阅者取消/清缓存/重载；同步事件，不要让一个监听异常破坏其他消费者 |

ChatTurn 是不可变契约：TurnId、UserMessageId、Input、Session、CreatedAtTicks。当前队列每个正常完成路径通过 currentTurn 引用判定一次终态；没有独立持久化 Turn 状态机，也没有重试协议。将来新增重试须定义已保存输入复用和 Assistant 去重。

## 网络、搜索与主动气泡

| 接口 | 结果 | 约束 |
| --- | --- | --- |
| AIChat.GetAIReply(turn,Action<ChatReplyResult>) | IEnumerator；成功、失败或取消 | ConversationService 调用；turn.Session 是请求身份；配置、搜索、模型阶段失败分流 |
| ChatReplyResult | Status、FailureReason、Text、Error，及 IsSuccess/IsFailure/IsCancelled | Success 才有业务文本；Error 是诊断，不直接写历史；失败原因含超时/HTTP/网络/解析/空内容/搜索/无效请求 |
| ChatReplyParser.Parse(responseBody) | 解析 choices[0].message.content；空文本/坏 JSON 失败 | 先由 HTTP 执行器判成功，再解析；Parser 不验证 HTTP |
| AIChat.CancelActiveRequests / CancelBubbleRequests | Abort 活动请求，气泡取消可独立调用 | 请求 Dispose 由执行器负责；会话变化同时使结果失效 |
| SearchDecisionService.Decide(userMessage,recentContext,rawLLMCall,callback) | IEnumerator，返回 SearchDecision 或 null | 不搜索/直接搜索走规则；模糊输入调用模型；null 是决策失败 |
| SearchCacheService.TryGetRecent / Add / Clear | 内存缓存；身份+版本匹配，最多 5 条/15 分钟 | 空查询/结果不缓存，切会话清理，不是长期记忆 |
| ChatContextBuilder.BuildMessages | JArray：system、排除当前消息后的 8 条历史、当前输入一次 | 新功能不能同时把本轮消息当历史和输入追加 |
| CharacterPromptBuilder 的快照重载 | 普通聊天/气泡 system Prompt | 按快照加载角色资料；无快照重载仅同步兼容 |
| AIChat.GetAIBubbleReply(snapshot,context,Action<string>) | IEnumerator，文本、null，或某些分支不回调 | 独立气泡锁；不保证普通聊天的结构化终态，不创建 ChatTurn |
| DesktopContextManager.OnWindowChanged | 公开 Action<string,string> 字段：标题、进程 | 不是 C# event 或全局总线；订阅和解绑对应组件寿命 |
| AIContextReactionManager.OnEnable / OnDisable | 订阅/解绑前台变化；停用 CancelBubbleRequests + StopAllCoroutines + 清锁 | 用请求快照；不要在返回后读取新前台当旧请求上下文 |
| InteractionEventService.CanTriggerBubble / RecordBubble* | 快照、标题/进程；冷却与事件记录 | 显示前会话检查；事件键/描述可能有原文，不等于脱敏日志 |
| BubbleUIManager.ShowBubble(message,duration=5) | 显示、布局与定时隐藏 | 会话变化隐藏旧气泡；不能持有模型请求所有权 |

单次 HTTP timeoutSeconds 默认 30，正值 clamp 到 1–120。一次普通 Turn 可包含决策、搜索和最终回复多个请求；超时设置不是整轮截止时间。当前没有流式、自动重试或请求恢复服务。

## 窗口、布局与前台接口

| 接口 | 数据/返回 | 限制 |
| --- | --- | --- |
| WindowsPlatformBootstrap.WindowService | IWindowService | 先检查实例和 IsInitialized；Editor 不替代 Player |
| IWindowService.SetBorderless / SetTransparentBackground / SetTopMost / SetClickThrough | bool | 全窗口能力，失败不能假定已生效 |
| SetLogicalSize(WindowLogicalSize) | bool；逻辑尺寸，平台处理 DPI | 不直接按 Screen.currentResolution 估工作区 |
| SetPhysicalBounds(x,y,width,height) | bool；桌面物理像素 | 保留负坐标显示器和工作区边界 |
| TryGetCurrentMonitorInfo / TryGetWindowRect / TryGetCursorPosition | bool/out 普通业务值类型 | 查询失败不读取 out 默认值作为有效坐标 |
| IsPointOnAnyMonitor / BeginWindowDrag | bool；后者进入原生系统拖动，结束返回 | HWND 与 Win32 常量留在 Platform/Native |
| WindowsForegroundContextService.TryGetCurrent(out ForegroundContext) | bool；Title/ProcessName | 业务不接收原生句柄或自行查 Win32 进程 |
| DesktopPetLayoutController.ApplyLayout(mode) | bool；Profile 驱动窗口和 Canvas | 当前失败可能已改变部分 UI；模式不是 World 状态，恢复需明确设计 |
| WindowSnapController.HandleWindowDragFinished | 拖动结束的吸附入口 | 用当前显示器工作区，不用主屏固定尺寸 |

## 站台、Timeline 与应用日志

| 接口 | 当前职责 | 不可推断的行为 |
| --- | --- | --- |
| SakuramachiSceneLoop.ValidateBindings / CaptureOriginalState / Evaluate / SilenceAudio / RestoreOriginalState / NormalizedSpeed | 统一时间求列车/杆/灯状态、音频与原状态恢复 | 不加载场景，不切登录/聊天；没有 OnTrainStop 业务事件 |
| SakuramachiLoopTrack/Clip | Timeline 绑定循环并传递时间 | 新 Intro 不再同时控制列车/杆/灯 |
| PlayableDirector.stopped | 播放停止/恢复接缝 | 不代表欢迎剧情成功或世界可以退出 |
| SakuraWeatherController.ApplyProfile / SetProfile / SetTargetCamera | 应用分层 VFX 配置与相机 | 局部 Demo 指标不能证明整机性能 |
| Application.logMessageReceived | AppLogService 应用级唯一订阅/退出解绑 | 2026-10-04 按用户要求恢复旧可读格式和根目录 run_log.txt；原文日志，Error/Exception 附堆栈，不再指纹或轮转 |

## M1 世界命令、资源与表现

| 接口/事件 | 契约与调用约束 |
| --- | --- |
| WorldCoordinator.TryEnterWorld(worldId,out error) | bool 表示接受，不是加载成功；仅 Desktop 且无遗留 Scope 可进入；先预检再捕获快照和创建实例 |
| WorldCoordinator.RequestExit() | Desktop 幂等成功；Entering 标记逻辑取消；Explore 启动退出；Exiting 有活动清理时不重复启动，失败结束后可重试 |
| State / CurrentScope / HasActiveWorld | 观察状态与当前所有权；Exiting 失败仍可能拥有 Scope，不能视作 Desktop |
| StateChanged | event Action<WorldState,WorldState>，参数为旧/新状态；视图按寿命订阅/解绑；单个监听异常被隔离 |
| DebugStateChangedSubscriberCount | 仅 Editor/Development 提供订阅数量，不作为 Release API |
| WorldScope | InstanceId、Definition/WorldId、ExitRequested、SceneHandle、DesktopSnapshot、RuntimeBindings、PreviousActiveScene；附加/移除操作由内部协调流程管理 |
| WorldDefinition.TryValidate / WorldCatalog.TryValidate、TryGet | 配置与目录校验、按 worldId 查询；Build Settings 可加载检查由资源后端负责 |
| IResourceService.CanLoadWorldScene(definition,out error) | 加载前能力检查；预检失败不先改变桌面表现 |
| IResourceService.LoadWorldScene(definition,completed) | IEnumerator + Action<WorldSceneLoadResult>；一次操作一次明确结果；成功交付 WorldSceneHandle |
| IResourceService.ReleaseWorldScene(handle,completed) | IEnumerator + Action<WorldSceneReleaseResult>；按原句柄释放；Local 后端已释放/场景已不存在可幂等成功 |
| WorldResourceServiceBehaviour | MonoBehaviour 抽象后端，Inspector 序列化入口；Local 实现当前使用 Build Settings + Additive |
| WorldPresentationController.TryCaptureDesktop | 捕获 DesktopPresentationSnapshot，先于表现修改；世界表现失败仍需靠快照恢复 |
| TrySuspendDesktopForEntering / TryBindLoadedWorld / TryActivateWorld | 暂停桌面主动气泡及窗口行为、解析场景绑定、交接世界表现；每步 bool/out error |
| TryPrepareWorldForExit / TryRestoreDesktop | 先放弃世界控制并恢复 Active Scene，再在释放后恢复桌面；失败保留 Exiting 供重试 |
| WorldRuntimeBindingsResolver.TryResolve(scene,out bindings,out error) | 只在目标 Scene 根对象内解析绑定并校验；每世界场景应保持唯一入口 |
| WorldRuntimeBindings.TryValidate / TryActivateContent / DeactivateContent / SetControlEnabled | 校验与副作用分离；显式管理 Camera/AudioListener/Input 和内容寿命 |
| WorldContentBindingsBehaviour.TryValidate / TryActivate / Deactivate | 内容适配抽象；Sakuramachi 实现对接列车/Director/樱花/天空，不承担场景加载 |
| SkyboxRotator.TryActivate / Deactivate | 显式天空材质激活、停用和恢复；禁止加载尚未提交就覆盖全局环境 |

World InstanceId 与会话版本不是同一令牌。资源操作暂无真实中断 Unity 场景加载的取消接口；Entering 中请求退出后等待结果并释放。释放失败保留句柄，桌面恢复失败保留快照与 Scope；只有全部完成才广播 Desktop。普通聊天取消策略不能从暂停主动气泡推断。

压力工具与故障后端存在，但不把工具存在写成所有故障验收通过；已确认结论、内存口径与下一阶段准则见 [M1 报告](M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md)。新增阶段通知与 Action 需复用世界寿命，另定义请求 ID、取消和补偿。
