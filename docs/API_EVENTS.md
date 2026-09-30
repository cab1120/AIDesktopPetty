# 接口与事件

2026-09-30。以下是现有接口；IWorldService、IResourceService、ActionQueue 等仍为计划项。

| 接缝 | 用途与限制 |
| --- | --- |
| DatabaseManager.Initialize / Close | 应用库连接；退出世界不能随意关闭 |
| AuthService.Login(userName, password, characterName, out error) | 成功修改 GlobalSession，生成新的 SessionVersion |
| GlobalSession.CaptureSnapshot / IsCurrent / SessionVersionChanged | 捕获请求身份、提交前判定、切换时取消旧工作；用户/角色 ID 和版本用于身份判定 |
| ConversationService.TrySend(input, out turn) | 创建 ChatTurn 并排入串行队列；用户消息轮到执行时才落库 |
| ConversationService.AssistantReplyReady / TurnStarted / TurnCancelled / TurnFailed | UI 观察成功回复、已开始执行、排队取消和失败；失败不存 Assistant 消息 |
| CharacterRepository.GetByUserAndName / SetActiveCharacter | 用户所属角色查询/启用；不是旧 GetByName |
| AIChat.GetAIReply(turn, callback) | IEnumerator，`ChatReplyResult` 成功/失败；TurnId 在 ChatTurn 中，网络请求 30 秒超时，版本变化 Abort |
| AIChat.GetAIBubbleReply(session, context, callback) | 主动气泡携带快照；忙时 yield break，不能假定每次有回调 |
| DesktopContextManager.OnWindowChanged | 公开 Action<string,string> 字段：标题/进程，不是全局总线 |
| AIContextReactionManager.OnEnable / OnDisable | 订阅/解绑前台变化，异步销毁联动待补 |
| DesktopPetLayoutController.ApplyLayout(mode) | bool 返回窗口结果，失败后仍可能改变 UI 状态 |
| WindowsPlatformBootstrap.WindowService | 返回 IWindowService，需检查初始化 |
| IWindowService | IsInitialized、SetBorderless、SetTransparentBackground、SetTopMost、SetLogicalSize、SetPhysicalBounds、SetClickThrough、BeginWindowDrag |
| IWindowService 查询 | TryGetCurrentMonitorInfo、TryGetWindowRect、TryGetCursorPosition、IsPointOnAnyMonitor；失败不能当有效坐标 |
| WindowSnapController.HandleWindowDragFinished | 拖拽后的吸附入口 |
| SakuramachiSceneLoop | ValidateBindings、CaptureOriginalState、Evaluate、SilenceAudio、RestoreOriginalState、NormalizedSpeed |
| PlayableDirector.stopped | 循环恢复；停止不等于欢迎剧情成功 |
| Application.logMessageReceived | AIChat 订阅并在销毁时解绑；日志仍同步写盘且未脱敏 |

未来阶段通知需适配循环；当前没有 OnTrainStop 业务事件，不能照旧计划直接调用。新请求/动作结果应携带 worldInstanceId、sessionVersion、requestId，并先定义所有权。
