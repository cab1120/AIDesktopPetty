# 从当前代码与原计划拆出实现任务

核对日期：2026-10-05，代码基线 Git fbe7f08。本文是实施定位指南；[原计划 PDF](../output/pdf/AI_Desktop_Companion_Updated_Plan_2026-09-21.pdf)本轮不改。原 docs/PROJECT_PLAN.md 已被移除，不再作为当前实施入口。先读 [README](../README.md)、[架构](ARCHITECTURE.md)、[M0 移交](M0/M0_ARCHITECTURE_AND_HANDOFF_2026-09-30.md)、[M1 实现与验收](M1/M1_IMPLEMENTATION_AND_ACCEPTANCE_2026-10-05.md)和 [现有接口](API_EVENTS.md)。

## 校准阶段状态

| 范围 | 当前事实 | 下一阶段怎样处理 |
| --- | --- | --- |
| M0 身份与普通聊天 | SessionSnapshot/ChatTurn/FIFO、输入去重、旧请求取消与提交校验存在 | 复用入口；独立处理 E01–E04 与未完运行验收，不因 M1 通过宣称全部缺口关闭 |
| M0 数据 | 稳定 ID、schema v1、事务删除、旧孤儿隔离；用户确认原库迁移/登录成功 | 新迁移先备份和副本回归；禁止猜测孤儿归属 |
| M1 世界闭环 | WorldCoordinator/Scope、IResourceService 本地 Additive 后端、表现快照、入口/导航、内容绑定已实现；用户最终验收通过 | 不重复创建世界状态机；在现有边界上扩展 |
| M1 内存记录 | 20 次往返结构计数回基线；native/asset 高水位诊断清理后明显回落 | 保留已知 retention；性能任务另测趋势和清理耗时，不能直接每次退出强制 GC |
| 站台/欢迎/交互 | 站台循环已有；欢迎产品流程、POI、统一 Action 和业务阶段事件尚未实现 | 列出待新增接口；复用世界激活/停用，列车只由现有 Loop 写入 |
| 资源框架/程序集 | 未接 YooAsset/UniTask/Cinemachine/HybridCLR，无自有 asmdef | 资源替换接 IResourceService；拆程序集先分析引用/GUID，不按计划文字推断已安装 |

## 按功能定位

所有下列脚本前缀为 Assets/Scripts；具体逐文件含义见 [SCRIPTS](SCRIPTS.md)。

| 工作 | 现有入口 | 必须考虑 |
| --- | --- | --- |
| 普通输入、网络和重试 | Character/UI/UIManager.cs、Character/Conversation/ConversationService.cs、Character/AI/AIChat/*、DataBase/Data/ChatMessage/* | TurnId、输入去重、结构化结果、终态一次、关系更新异常 |
| 情绪 E01 | AIChat、Character/AI/Prompt/Emotion/EmotionMemory.cs、DataBase/Data/Emotion/SQLiteEmotionStorage.cs、Test/EmotionBuildDebugTest.cs | 名称/ID 混用、历史归属歧义、登录前初始化、改名与级联副本回归 |
| 数据账户 | DataBase/AuthService.cs、DatabaseSchemaMigrator.cs、Data/UserData/UserRepository.cs、Data/Character/CharacterRepository.cs | 初始化门槛、稳定 ID、事务与隔离、兼容旧库 |
| 前台气泡与日志 | Presentation/DesktopContextManager.cs、Character/AI/AutoTalk/AIContextReactionManager.cs、BubbleUIManager.cs、InteractionEventService.cs、DataBase/AppLogService.cs | 普通队列与气泡不同；世界暂停后的锁释放；原文日志/事件/网络策略分别定义 |
| 世界状态/加载/退出 | Application/World/WorldCoordinator.cs、WorldScope.cs、Resource/*、Definition/* | 预检、Scope 校验、逻辑取消、原句柄、释放失败与恢复失败重试 |
| 世界表现/输入/窗口 | Application/World/Presentation/*、Platform/Windows/IWindowService.cs、Character/UI/Layout/* | 快照先捕获；唯一相机/监听器/输入所有者；窗口服务失败；Player/DPI验收 |
| 樱町内容/欢迎 | Art/Scenes/SakuramachiWorldBindings.cs、SakuramachiSceneLoop.cs、SakuramachiLoopTrack.cs、3DScene 的 Director | 世界提交后激活，退出可取消；欢迎只写角色/镜头，不能双写列车 |
| 樱花/天空/配置 | Art/Skybox/SkyboxRotator.cs、Assets/Dev/SakuraWeather、Assets/Config/ScenesVFX、Assets/Config/Worlds | 全局环境恢复、运行材质释放、配置 GUID、资源引用与清理边界 |
| 故障与压力回归 | Test/World/FaultInjectingResourceService.cs、WorldCoordinatorDebugHarness.cs、ResourceServiceDebugHarness.cs、WorldM1StressRunner.cs | 加载中退出、失败释放重试、恢复失败、反复往返、订阅与结构基线、内存口径 |

## 后续功能建议拆法（实施需单独授权）

1. 对照原计划列出剩余目标；把已验收 M1 标为基线，把 M0 缺口及 M1 内存记录独立列出，避免重复建设。
2. 欢迎流程先定义开始/完成/跳过/世界退出时的行为。消费世界已激活结果，新增角色/镜头演出；不要让 Timeline.stopped 直接代表业务完成。
3. 若需列车阶段消费接口，先定义 20 秒停稳等事件如何处理跳时、回卷、180 秒循环与重复订阅；列车/杆/灯控制权保留给 SakuramachiSceneLoop。
4. 最小 POI/交互明确输入所有者、触发与可取消动作，挂在当前 WorldScope；退出前停止它们，过期返回不得修改新世界对象。
5. AI Action 用白名单与可取消执行器，明确 SessionSnapshot + 世界实例 + 请求 ID 校验，不执行模型输出脚本/对象路径。
6. 新增世界配置需 WorldDefinition/Catalog、Build Settings、唯一 RuntimeBindings、内容绑定和表现配置，并测试缺绑定、加载失败及往返基线。
7. 资源后端替换须保持 IResourceService 句柄/结果契约、失败所有权与释放幂等；性能清理策略先测停顿和内存趋势，不能从 M1 高水位记录直接推导强制清理。
8. 新功能回归 M1 进入/退出、重复请求、加载中退出、释放/恢复失败重试与 20 次往返；多屏/DPI、联网、欢迎完整流程另有独立运行验收。

## 每项任务的交付模板

- 触发、成功/失败/取消后的可见行为。
- 当前文件/方法/场景与配置证据；待新增接口明确标注。
- 逐文件修改、依赖方向、GUID/序列化影响。
- App/Session/World/View 状态归属；跨 yield 捕获、有效性校验与释放责任。
- 数据归属、迁移、备份、旧数据兼容。
- 每个中间步骤的失败补偿和重试语义。
- 可重复验收输入、故障注入、基线与测量口径；注明 Editor/Player、硬件与网络条件。
- 仅列代码无法确定且影响产品语义的用户决策。

## 可直接交给其他 LLM 的请求

> 请结合 README、原计划和 M1 移交报告制定下一阶段具体实施步骤。M1 世界生命周期已实现并通过用户验收；复用现有协调器、Scope、资源契约、快照和绑定，不重复搭建。保留内存高水位及 M0 E01–E04 记录。按依赖给出文件/方法、现有与待新增接口、身份/寿命归属、取消/失败重试、数据兼容和验收。不要把欢迎、Action、YooAsset 或阶段事件写成已实现，不改变列车时序。此请求用于制定步骤，实施另行授权。

仅有文档时，方法和绑定是定位线索，仍需源码核对；不虚构签名或运行证据。可访问仓库时遵循 [AGENTS.md](../AGENTS.md)。
