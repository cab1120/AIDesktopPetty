# M1 世界生命周期：实现、验收与后续移交

更新日期：2026-10-05。实现核对到 Git fbe7f08a62e1e887c96a8e33c821806172af89ee。本次只修改文档，未重新运行 Unity、构建 Player 或读取实际数据库/运行日志。

## 1. 阶段结论与证据范围

**M1 已通过用户最终验收。** 桌面壳与樱町世界的进入、表现交接、退出、资源释放和失败恢复已实现，不再是待新增架构。

用户给出的 Git 边界为 8473655938ee1d695c9ff14b398761ead4444074 → fbe7f08a62e1e887c96a8e33c821806172af89ee。起点已经包含 SessionSnapshot/ChatTurn；此区间还包含 6853f6f 的 M0 收尾，不能把全部区间差异都算成 M1 新功能。M0 历史与未解决项继续见 [M0 移交](../M0/M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md)及 [架构 E01–E04](../ARCHITECTURE.md)。M1 验收不自动关闭这些缺口。

| 提交 | 主要变化 |
| --- | --- |
| 6853f6f | M0 聊天请求、终态与启动日志整理，作为世界功能的已有基础 |
| 0402cdd | WorldCoordinator、WorldScope、WorldState 生命周期 |
| bb6a8f8 | 世界定义、目录与本地场景资源后端 |
| 40e8743 | WorldResourceServiceBehaviour 序列化适配与协调器依赖调整 |
| 17ba22f | 桌面快照、世界表现与运行时绑定 |
| a700322 | 世界入口、导航、场景与表现配置接线 |
| 7fecb5c | 樱町内容绑定、天空寿命处理；按用户要求恢复 run_log.txt |
| 137fa87 | 资源故障注入、退出分阶段处理与重试 |
| fbe7f08 | WorldM1StressRunner、生命周期与订阅调试计数器 |

## 2. 最终验收记录及内存现象

以下运行结论来自用户于 2026-10-05 的反馈；本次文档核对没有独立重跑。具体测试机器、Editor/Player 类型、采样原始文件与执行日期未另行提供，不补写为已知事实。

连续 **20 次 World 往返**后，Scene、WorldRuntimeBindings、SakuramachiSceneLoop、Camera、AudioListener、播放 AudioSource 和 StateChanged 订阅数量均恢复至基线，**未观察到结构性生命周期泄漏**。

| 内存指标 | 清理前约值 | 清理后约值 | 观察 |
| --- | --- | --- | --- |
| Managed | 631.8 MB | 550.0 MB | 回落约 81.8 MB |
| Unity Allocated | 865.4 MB | 285.1 MB | 回落约 580.3 MB |

用户报告在执行 Resources.UnloadUnusedAssets() 后观察到上述回落。源码中的 Diagnostic Cleanup 还执行 GC.Collect → WaitForPendingFinalizers → GC.Collect，并等待两帧后采样。因此若数据来自该菜单，应该描述为“诊断清理后回落”；单次这组结果不能区分各个清理步骤的独立贡献。

重复加载仍产生 Unity native/asset 内存高水位；清理后的明显回落支持“增长主要来自可回收的 unused asset/native resource retention”，而非当前已观察到的永久 retained-object leak。**这项现象作为 M1 已通过验收的已知记录保留。** 不把计数恢复写成所有内存绝无泄漏，也不把高水位增长直接判为永久泄漏。550.0 MB 不是本次提供的初始基线，不能据此宣称 Managed 完全恢复。

WorldM1StressRunner 的 Managed 来源为 GC.GetTotalMemory(false)，Unity Allocated/Reserved 来自 Unity Profiler，另采样进程 Private Bytes；这些口径不能相加或互相替代。后续性能阶段应保存环境、初始基线、每轮采样、清理耗时与清理后趋势，再决定清理频率。当前生产退出路径卸载场景，并未自动逐次执行 UnloadUnusedAssets 或强制 GC；本轮不改该策略。

## 3. 运行路径和设计原因

SampleScene 保留桌面应用壳；3DScene 已加入 Build Settings，由 LocalSceneResourceService 按完整路径 Additive 加载。WorldCatalog 中当前世界 ID 为 sakuramachi。配置在 Assets/Config/Worlds；樱花配置迁到 Assets/Config/ScenesVFX。

### 进入

1. UI 的 WorldEntryButton 调 WorldCoordinator.TryEnterWorld。只有 Desktop 且无遗留 Scope 才接受。
2. 校验目录、定义、后端加载能力和桌面表现绑定，先捕获 DesktopPresentationSnapshot，再创建递增 InstanceId 的 WorldScope。
3. 状态转 Entering，暂停桌面主动搭话、隐藏瞬时气泡、暂停吸附与穿透控制，启动资源协程。
4. 加载返回后核对当前 Scope 和退出标记。加载中 RequestExit 是逻辑取消：Unity 加载不被假装 Abort；成功得到的句柄仍要释放。
5. 解析目标场景的 WorldRuntimeBindings、校验内容；转交 Active Scene、窗口、桌面/世界相机、AudioListener 和输入，再激活樱町内容，成功后进入 Explore。

TryEnterWorld 返回 true 只表示请求已接受，不表示已加载完成。普通聊天仍由原 ConversationService 所有；暂停主动气泡不能解释为“所有普通聊天都被取消”。世界 InstanceId 与 SessionVersion 属于不同失效边界，未来跨世界异步功能应分别校验。

### 退出与失败

1. Explore → Exiting。先停止世界内容和控制权、恢复先前 Active Scene，再解绑活动 RuntimeBindings。
2. 释放 Scope 拥有的场景句柄。失败或没有完成结果时保留句柄与 Exiting，允许之后 RequestExit 重试。
3. 场景成功释放后才移除句柄；恢复桌面布局、精确窗口边界和行为状态。
4. 桌面恢复失败也保留 Scope/Exiting。下一次重试即使没有句柄，也必须继续恢复桌面；只有清理与恢复完成才清 Scope、转 Desktop。
5. 正在退出时重复请求不启动第二条清理协程；Desktop 下退出视为目标已满足。

这种分阶段补偿避免“卸载成功但窗口恢复失败”被误报为退出成功，也避免重试重复卸载已释放资源。它不是数据库式原子事务：异步场景与窗口操作靠所有权记录和可重试步骤补偿。

## 4. 逐文件实现地图

以下路径均相对仓库；全部脚本可点击定位于 [脚本索引](../SCRIPTS.md)。这里说明责任与为什么拆分，不把配置对象和一次运行实例混为一谈。

| 脚本（Assets/Scripts 下） | 做了什么，为什么 |
| --- | --- |
| Application/World/WorldState.cs | Desktop/Entering/Explore/Exiting；世界寿命与桌面 UI 布局状态分离 |
| Application/World/WorldScope.cs | 一次进入的 InstanceId、Definition、ExitRequested、句柄、桌面快照、运行绑定与原 Active Scene；退出仍需知道自己拥有何物 |
| Application/World/WorldCoordinator.cs | 唯一进入/退出协调、状态迁移、过期结果处理、分阶段清理/重试与 StateChanged；UI 不直接加载场景 |
| Application/World/WorldContentBindingsBehaviour.cs | 内容侧校验/激活/停用抽象；世界层不硬编码樱町内容 |
| Application/World/Definition/WorldDefinition.cs | 静态 WorldId、展示名、完整 ScenePath 与配置校验；不存运行句柄 |
| Application/World/Definition/WorldCatalog.cs | 世界配置集合与查询/校验；入口按 ID 选择世界 |
| Application/World/Resource/IResourceService.cs | 可加载检查、加载和释放协程契约；协调器不依赖具体资源来源 |
| Application/World/Resource/WorldResourceServiceBehaviour.cs | MonoBehaviour 抽象实现接口，供 Inspector 序列化后端引用 |
| Application/World/Resource/LocalSceneResourceService.cs | Build Settings 检查、Additive Load/Unload、释放幂等；当前后端是 Unity 本地场景，不是 YooAsset |
| Application/World/Resource/WorldSceneHandle.cs | 已加载场景及释放标记；退出必须释放原句柄而非猜场景名 |
| Application/World/Resource/WorldSceneLoadResult.cs | 成功句柄/失败原因；加载完成不能只靠协程结束猜测 |
| Application/World/Resource/WorldSceneReleaseResult.cs | 释放成功/失败；明确失败后仍保留 Scope 所有权 |
| Application/World/Presentation/DesktopPresentationSnapshot.cs | 进入前布局、窗口/显示器信息、相机/监听器及桌面行为启用状态；恢复实际进入前状态 |
| Application/World/Presentation/WorldPresentationProfile.cs | 世界窗口尺寸和当前显示器居中配置；不把 UI Layout 当世界状态 |
| Application/World/Presentation/WorldPresentationController.cs | Capture/Suspend/Bind/Activate/PrepareExit/Restore；统一窗口、输入、相机、音频控制权交接 |
| Application/World/Presentation/WorldRuntimeBindings.cs | 场景提供相机、监听器、输入行为与内容入口；Additive 加载初期避免自动抢控制权 |
| Application/World/Presentation/WorldRuntimeBindingsResolver.cs | 在目标 Scene 根对象中定位并验证绑定；不跨世界全局 Find |
| Application/World/Presentation/WorldEntryButton.cs | 只请求进入，按世界状态切换按钮可用性；不持有资源 |
| Application/World/Presentation/WorldNavigationPanel.cs | 世界返回导航及状态订阅/解绑；清理重试仍经协调器 |
| Art/Scenes/SakuramachiWorldBindings.cs | 校验并接入 Loop、Director、SakuraWeather、Skybox；内容停用时停止音频/演出并恢复 |
| Art/Skybox/SkyboxRotator.cs | 显式激活/停用天空、运行材质与恢复，避免 Additive 尚未提交时污染全局天空 |
| Art/Scenes/SakuramachiSceneLoop.cs | 保留列车/栏杆/灯统一时序，增加验收用调试观察；世界层不接管列车时间 |
| Test/World/ResourceServiceDebugHarness.cs | 资源服务手动加载/释放验证入口；测试工具不作为业务所有者 |
| Test/World/WorldCoordinatorDebugHarness.cs | 手动验证状态机、进入/退出与异常路径 |
| Test/World/FaultInjectingResourceService.cs | 包装真实后端，注入加载/释放失败与完成延迟；证明失败路径不是靠运气执行 |
| Test/World/WorldM1StressRunner.cs | 20 次往返、基线比较、耗时与内存采样、诊断清理；仅 Editor/Development 编译 |
| DataBase/AppLogService.cs | 应用寿命一次订阅，恢复旧 Debug 原文与 Error/Exception 堆栈追加到 run_log.txt；不是 World 寿命日志器 |

其他资产变化：SampleScene 接入协调器、资源/表现后端、入口及导航；3DScene 接入运行与内容绑定；EditorBuildSettings 同时启用两场景。WorldCatalog.asset 引用 Sakuramachi.asset，后者映射 3DScene；SakuramachiPresentation.asset 当前为 1280×720、当前屏居中。SakuraWeather 配置目录调整保留资源 GUID，不能按旧目录重复创建或覆盖。

## 5. 后续修改必须保持的规则

- 世界命令只经 WorldCoordinator；不要在按钮、内容脚本或新业务里各自 Load/Unload 同一场景。
- WorldDefinition 是静态配置，WorldScope 是运行实例；跨 yield 持有原 Scope，并在提交前验证有效性。失效结果仍可能携带必须释放的资源。
- 后端回调每次操作只提交一次明确结果；成功加载必须交付句柄。释放失败不能伪装为 Desktop，重试不能丢掉原句柄。
- 捕获快照在修改表现之前。窗口操作经 IWindowService；相机、AudioListener、输入与窗口行为只允许当前模式拥有控制权。
- 每场景保持唯一明确 WorldRuntimeBindings；不要依赖 Resolver 查出同一根下任意重复组件。内容 Validate 不得启动演出或修改全局状态。
- 世界内容采用显式激活/停用与资源释放；全局 RenderSettings/Active Scene 有恢复责任。不要把局部内容改成 DontDestroyOnLoad。
- StateChanged 在视图寿命订阅/解绑；新增订阅者或异步动作需纳入重复往返基线比较。
- 普通聊天、账户数据库与应用日志保留 M0 规则；退出世界不等于退出账号，不关闭应用共享数据库。
- SakuramachiSceneLoop 独占列车/栏杆/灯；欢迎仅角色/镜头，跳过不改列车时间。新增阶段事件先定义回卷、跳时及重复通知。
- UnloadSceneAsync 不等于主动回收全部 unused assets。诊断清理不是每次退出的产品策略；性能优化先测可回收量、停顿与重复增长。
- 新世界需配置、Build Settings、绑定、失败恢复和重复往返验收；资源后端可替换，但 YooAsset/Bundle/远端目前未实现。

## 6. 复验与下一阶段输入

WorldM1StressRunner 在 Play Mode 下使用 Inspector 上下文菜单 M1-7 / Run 20 Round Trips，默认 20 次、Explore 停留 2 秒、桌面稳定等待 0.5 秒、迁移超时 30 秒；运行前绑定 Coordinator 并从 Desktop 开始。这不是每轮完整 180 秒站台演出验收。Diagnostic Cleanup 是单独菜单；不要拿真实数据库或包含敏感信息的日志做实验。

压力测试采样 CSV 写到 Application.persistentDataPath/m1-world-stress-yyyyMMdd-HHmmss.csv；日志输出环境、采样和进入/退出 P50/P95。诊断清理样本当前写到 Unity 日志，不能假定已自动附入压力 CSV。本轮不打开实际运行日志或 CSV。

WorldNavigationPanel 目前只在 Explore 显示返回入口，WorldEntryButton 只在 Desktop 可点击。协调器提供失败退出重试，不意味着普通导航 UI 已提供 Exiting 状态的重试按钮；故障复验可用调试工具，未来产品错误反馈/重试视图需单独设计。

工具具备故障注入与重复进入/退出检查能力；“工具存在”不等于每个故障场景都已有独立通过证据。本次确定的最终证据为用户声明 M1 通过，以及上面的 20 次往返和清理数据。多屏/DPI、后续欢迎/Action/网络路径各有自己的验收要求。

向其他 LLM 提供 README、原计划及本报告：将 M1 作为已实现且已验收的基线，先核对原计划剩余工作与 M0 缺口，再提出欢迎/交互/Action 或性能任务；不得重复搭建 WorldScope/IResourceService，也不得把内存高水位悄悄从记录删掉。原计划本轮不修改。
