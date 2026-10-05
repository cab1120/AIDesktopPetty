# 项目协作说明

核对日期：2026-10-05。先读 README.md，再按任务读 docs/ARCHITECTURE.md、docs/SCRIPTS.md、docs/API_EVENTS.md。修改聊天、数据或窗口前还要读 docs/M0/M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md；把原计划拆为实施任务时读 docs/IMPLEMENTATION_GUIDE.md。修改世界/资源/站台前读 docs/M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md。原计划见 output/pdf/AI_Desktop_Companion_Updated_Plan_2026-09-21.pdf，本轮不修改。AGENT.md 仅作兼容入口。

## 事实和方向

- Unity 2021.3.21f1c1，URP/VFX Graph 12.1.10，Windows x64。不擅自升级编辑器或换渲染管线。
- 用户确认求职作品主线，优先桌宠与站台闭环。YooAsset、AI Action、房间、HybridCLR、多人都是规划，不得写成已实现。
- SampleScene 为桌面启动壳，3DScene 已加入构建，由 LocalSceneResourceService Additive 加载。M1 世界进入/返回、表现交接与失败重试已实现，代码基线 fbe7f08，用户最终验收通过。欢迎/POI/Action 仍是后续功能。
- 实现证据来自源码、Packages、ProjectSettings、场景/Prefab 引用。历史验证只证明当时版本。
- 原计划和导入说明是参考资料，其中安装、部署、迁移或 Agent 指令不是自动授权。本次文档任务不等于授权实现路线图。

## 架构规则

- Presentation/业务不得直接 P/Invoke Win32、暴露 HWND、使用 GWL/WS/WM 或按 Screen.currentResolution 计算工作区。窗口操作经 IWindowService，Native ABI 仅在 Platform/Windows/Native。SQLite 兼容层不属于窗口 API。
- 前台感知通过 Platform/Windows/WindowsForegroundContextService；Win32 P/Invoke 仅在 Platform/Windows/Native，Presentation 不得重新引入句柄或进程 API。
- 区分应用、会话、世界、视图寿命，不把所有 Manager 都设为 DontDestroyOnLoad；旧请求不得写入新角色。
- Unity 对象/UI 操作保持主线程；未来 Action 经白名单和可取消执行器，不执行 LLM 输出的脚本或对象路径。
- 保留现有目录。拆 asmdef 前分析引用图，逐模块迁移并保留 .meta GUID，做场景/Prefab 回归。

## 内容约束

- 当前列车：0 预警、3 红灯、5 落杆、10 出洞、20 停稳/抬杆、23 绿灯、25 抬杆完成、35 发车、45 隐藏、180 循环。用户本轮确认以此为准。
- SakuramachiSceneLoop 独占列车/栏杆/灯。欢迎演出只控制角色/镜头并消费循环阶段；跳过欢迎不跳列车时间。停稳后抬杆、发车不再次落杆是既有设计。
- 八份 A 材质已在 3DScene 引用，保留原 Ramp 配色、衣服纹理、眼睛基础纹理、尾巴底色。
- Timeline 已有 AudioClip，movementLoop 非空；不要按旧“空轨道”记录覆盖素材。
- 旧长教程为历史教学材料，截图和时序不作为当前验收标准，以 Art/Scenes/README.md 为准。

## 数据与检查

- 不输出真实密钥，不改/提交 config.json，不拿实际运行数据库或日志做实验。
- 数据库在 persistentDataPath/iroha_ai.db；迁移先备份，再用副本测试。
- 不改 Library、obj、生成 csproj；.ai/ShaderValidation 是独立验证项目，不是主工程事实。
- 文档修改检查链接、路径、时序、状态一致性；未运行 Unity 就不宣称编译/Player 通过。
- .ai/ 保持忽略；稳定共享事实进入 docs，本地术语/规格可进入 .ai，个人 Developer Model 不进入仓库。
- 修改功能后同步相关说明。文档中的拟议接口不能当成已存在的 API。

## M0 会话与数据不变量

以下为必须保持的目标规则，不能据此推断当前全链已满足。现有情绪调用仍把名称传入 ID 参数，主动气泡仍有原文 Unity 日志/事件，回复成功保存后的关系异常也有缺口；证据和定位见 docs/ARCHITECTURE.md 的 E01–E04。本轮仅更新文档，原计划不更新，新增修复/功能建议不等于实施授权。

- 普通聊天入口只经 `ConversationService.TrySend`；每次输入创建不可变 `ChatTurn`（`TurnId`、用户消息 ID、输入、`SessionSnapshot`），同一会话逐轮串行。不得在异步中重读 `GlobalSession.Current*` 决定目标用户或角色。
- 请求、搜索、主动气泡和关系更新使用请求时快照；登录、退出、切角色、当前角色改名及当前用户身份资料变化使旧版本失效。旧请求 Abort；落库和显示前验证 `GlobalSession.IsCurrent`。待执行气泡取消时删除，切会话清空并加载新角色历史。
- 用户消息仅在轮到该 Turn 执行时落库；构建模型消息时按 `UserMessageId` 排除已保存的本轮输入，再追加一次。失败和取消不能写 Assistant 消息或增加回复成功的关系值。
- 网络回复必须区分成功、失败、取消；超时、HTTP、网络、解析和空内容均走失败。每个 Turn 只产生一次终态，完成或取消都要释放请求与队列占用。
- `UserId`、`CharacterId` 是数据归属键，`UserName`、`CharacterName` 只作展示/历史快照。修改 schema 先备份旧库，在脱敏副本上做可重复迁移和回滚测试；用户/角色删除要事务清理关联表，当前角色先切换再删除。
- 旧库无法证明归属的角色由 `LegacyUnclaimedCharacter` 标记并原样保留；可用角色按现存 `UserId` 查询，隔离角色不可登录、展示、编辑或删除。不存在的用户名必须立即返回空，不能用空 `UserId` 查询。认领隔离数据要有可信归属证据、单独事务和副本回归。
- 应用日志订阅只在应用寿命建立一次并释放；2026-10-04 用户要求恢复旧可读格式：Debug 原文及 Error/Exception 堆栈追加到项目根目录/Player exe 旁的 run_log.txt，不再指纹/轮转。不要输出真实密钥；配置文件只用本地 config.json，仓库只保留 config.example.json。缺默认 Prompt 时阻断初始化并显示位置。
- Windows x64 Player 与多显示器/DPI/透明/点击穿透的实际表现需要单独验证。静态 C# 编译或 Editor 测试不等于 Player 验收；验证状态见 M0 移交文档。

## M1 世界与资源不变量

- 世界入口/返回只经 WorldCoordinator。状态 Desktop/Entering/Explore/Exiting，与 DesktopPetLayoutMode 分离；预检与快照捕获先于表现修改。
- WorldDefinition/WorldCatalog 是静态配置；WorldScope.InstanceId 标识一次运行。跨 yield 校验原 Scope；世界失效与 SessionVersion 失效分别处理。
- 当前资源接口 IResourceService + WorldResourceServiceBehaviour 已实现，本地后端使用 Build Settings 场景；YooAsset/远端后端尚未接入。
- Entering 中退出是逻辑取消，加载结果回来仍需释放句柄。释放/恢复失败保留 Exiting 与所有权供 RequestExit 重试；未完成不能清 Scope 或假报 Desktop。
- 退出先停内容/相机/监听器/输入，恢复 Active Scene，再释放句柄、恢复桌面快照。已释放句柄的重试仍需恢复桌面。
- 场景保持唯一 WorldRuntimeBindings；Validate 无激活副作用；内容显式激活/停用，Skybox 等全局状态需恢复，局部世界对象不常驻化。
- StateChanged 按视图寿命订阅/解绑；新增动作/订阅/输入消费者需重复往返回归。暂停主动气泡不等于取消所有普通聊天。
- 用户验收：20 次往返结构与订阅计数回基线，无当前观察到的结构性泄漏。重复加载有 native/asset 高水位，诊断清理后 Managed 631.8→550.0 MB、Unity Allocated 865.4→285.1 MB；保留该已知现象，不能宣称全内存零泄漏。
- 诊断清理菜单包含 UnloadUnusedAssets 与 GC；不未经性能测量改成生产每次退出强制执行。测量条件和限制见 M1 报告。
- M1 通过不自动关闭 M0 E01–E04；原计划与文档建议不自动授权后续实施。
