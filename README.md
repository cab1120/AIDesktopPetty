# AIDesktopPetty · AI 桌宠

基于 Unity 2021.3 的 Windows 桌面 AI 陪伴项目，包含桌面聊天、前台窗口感知、本地关系/情绪数据，以及独立的樱町 3D 场景。用户已确认继续以 Unity 客户端求职作品为主线，近期目标是“桌宠 → 站台 → 交互 → 返回桌宠”。

核对日期：2026-09-21。“已有”指源码或序列化资源可核实；本次未执行 Unity Player 构建、联网聊天或听感测试。

## 当前状态

| 能力 | 核实结果 |
| --- | --- |
| 桌面聊天与管理 | SampleScene 挂载登录、聊天、历史、用户/角色管理、主动气泡 |
| AI 与搜索 | SiliconFlow 聊天 + Bocha 搜索；协程、非流式；模型名写在 AIChat |
| 本地数据 | SQLite 六张业务表，运行库在 persistentDataPath；关系、情绪、互动事件已有实现 |
| Windows 窗口 | IWindowService → WindowsWindowService → Native ABI → x64 DLL；透明、置顶、拖拽、吸附、穿透、DPI/多屏查询 |
| UI 布局 | DesktopPetLayoutController + Profile 统一登录、折叠、聊天和管理布局 |
| 樱町站台 | 3DScene 已引用 180 秒列车/栏杆/六灯循环、樱花 VFX、天空旋转、八份 A 版角色材质与音频 |
| 世界切换 | 尚无 WorldService/资源生命周期闭环；3DScene 不在当前 Build Settings |
| 后续技术 | 未安装 YooAsset、UniTask、Cinemachine、HybridCLR；Assets 下未发现 asmdef |

独立场景演示不等于已经接通的桌宠产品。参见 [开发计划](docs/PROJECT_PLAN.md) 和 [架构风险](docs/ARCHITECTURE.md)。

## 环境与启动

- 编辑器：2021.3.21f1c1，以 ProjectSettings/ProjectVersion.txt 为准。
- 渲染：URP 12.1.10，GraphicsSettings 已绑定管线资源；不是 Built-in 项目。
- 主要包：VFX Graph 12.1.10、Timeline 1.6.4、TMP 3.0.6、Newtonsoft JSON 3.2.2、FBX 4.1.2。
- 目标：Windows x64 Player。编辑器不能验证被 !UNITY_EDITOR 排除的窗口路径。

1. 用对应 Unity 版本打开项目，等待导入与编译，打开 Assets/Scenes/SampleScene.unity。
2. 在本机将 Assets/StreamingAssets/config.example.json 复制为同目录 config.json，填写自己的 siliconFlowKey 和 bochaApiKey。不要覆盖已有配置或提交密钥。
3. 确保 Assets/StreamingAssets/DefaultCharacterPrompt.json 存在。初始化器在文件缺失时仍继续读取，可能抛异常。
4. Play 后默认登录：用户 DefaultUser，密码 123456，角色 DefaultCharacter；账号权限为 Admin。这些是开发默认值。
5. 数据库自动在 Application.persistentDataPath/iroha_ai.db 创建，无需复制 StreamingAssets 数据库。
6. 单独查看 3D 内容时打开 Assets/Scenes/3DScene.unity。该场景不是完整登录/聊天启动入口。

AIChat 当前代码使用 SiliconFlow 的 /v1/chat/completions，模型 Pro/deepseek-ai/DeepSeek-V3；搜索调用 Bocha /v1/web-search。代码配置不能证明服务端账号已开通或当前模型可用。

## 构建与数据

Build Settings 仅启用 SampleScene。构建选 Windows x86_64，核验 Assets/Plugins/x64 下的 DesktopPet.Native.Windows.dll、sqlite3.dll 及输出。原生 DLL 的 Importer 含其他平台启用项，发布前需要收敛核验。仅添加 3DScene 不会自动产生“进入站台”功能。

业务表：User、CharacterProfile、UserCharacterState、EmotionState、ChatMessage、InteractionEvent。聊天当前每用户/角色最多保留 100 条，模型消息上下文取最近 8 条，不等于长期语义记忆。迁移/重置前备份实际运行数据库，实验使用脱敏副本。

AIChat 订阅 Application.logMessageReceived 后同步写入 Application.dataPath/../run_log.txt。日志可能含聊天和窗口标题，目前订阅没有对应解绑；跨场景扩展前需要修正。

## 代码与文档导航

| 路径 | 用途 |
| --- | --- |
| Assets/Scripts/Character/AI | 请求、搜索、Prompt、主动气泡、情绪 |
| Assets/Scripts/Character/UI | 桌宠 UI、历史、管理、Layout |
| Assets/Scripts/DataBase | 初始化、会话、Repository、业务 Service、SQLite |
| Assets/Scripts/Presentation | 窗口表现和前台感知；旧 System 路径已失效 |
| Assets/Scripts/Platform/Windows | 窗口服务、值类型、Native ABI |
| Assets/Scripts/Art/Scenes | 列车循环、Timeline、裁剪 Shader、配置/验证工具 |
| Assets/Scripts/Shaders/Improved2.0 | A 版人物 Shader、材质、切换工具 |
| Assets/Dev/SakuraWeather | 分层 VFX、配置、演示、局部性能记录；3DScene 已引用 |
| Assets/Prefeb | 当前 UI 预制体目录，保留原拼写 |

- [架构](docs/ARCHITECTURE.md) / [脚本](docs/SCRIPTS.md) / [接口事件](docs/API_EVENTS.md)
- [近期计划](docs/PROJECT_PLAN.md) / [本次工作报告](docs/WORK_REPORT_2026-09-21.md)
- [列车现行说明](Assets/Scripts/Art/Scenes/README.md) / [角色材质说明](Assets/Scripts/Shaders/Improved2.0/README.md)
- [AI 协作入口](AGENTS.md)。.ai/ 被 Git 忽略，核心交接不能只存在其中。

## 已知缺口

表现层通过 IWindowService 操作窗口，不得新增 HWND、Win32 常量或直接 P/Invoke。DesktopContextManager 仍直接 user32，是已知遗留违例。

近期先处理请求取消/会话隔离、日志解绑、用户改名关联和世界生命周期，再加交互与远端资源。当前输入可能重复进入上下文，错误文本被正常落库，切角色后迟到回复可能串会话。触发条件和验收见架构与计划；本次未修复运行代码。

现行列车时间为 10 秒出洞、20 秒停稳、35 秒发车、45 秒隐藏、180 秒循环。不要让新欢迎 Timeline 同时写列车、栏杆或灯属性。

## 授权状态

旧 README 曾标为 MIT，本次未发现根目录 LICENSE 文件。不能据此承诺整个仓库及模型、贴图、动作、音频均可再分发；公开发布前补齐代码许可和素材来源清单。
