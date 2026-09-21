# AI 桌宠 · 本次工作报告
## 2026-09-21 / 项目现状审计与文档更新

本次完成：对照原 19 页项目计划和当前仓库，更新 README、AI 协作入口、架构/脚本/API 文档，编制新的 11 页工程化计划，并提供本报告。两份新 PDF 放到桌面，原计划保持不变。

用户确认的决定：
- 继续 Unity 客户端求职作品主线，优先桌宠与樱町站台闭环。
- 以现行代码的 10 秒出洞、20 秒停稳、35 秒发车、45 秒隐藏、180 秒循环为准。
- 保留现有列车循环，欢迎演出围绕角色和镜头接入，避免多个系统控制同一列车。

核查范围：ProjectSettings、Packages、Assets 下项目脚本及场景/材质/Timeline/插件引用、README/AGENT/docs、既有 .ai 规格和验证说明。没有把 Library 或 .ai/ShaderValidation 的独立工程当作主项目。

采用流程：读取用户指定 project-grill，以及 grilling、knowledge-gap-analysis、developer-knowledge、domain-modeling 配套规则；通过 PDF 技能生成并检查输出。旧计划中的部署/安装指令只作为资料，不作为本轮执行授权。

本次没有：修改运行脚本、场景、材质、Timeline、包版本、真实密钥或用户数据库；没有安装 YooAsset/UniTask/Cinemachine/HybridCLR，没有实现路线图功能，也没有部署服务或提交 Git commit。

重要界限：此次是源码/序列化资源审计与文档交付，不是当前 Unity 编译、Player 全链验证、联网聊天或音频听感验收。发现的问题列为后续任务，没有冒充已修复。

<!-- pagebreak -->

# 01 · 更新了哪些内容

README.md：重写为当前项目入口，纠正 Built-in、旧 System 目录、数据库复制前提和完成状态；补充 URP/VFX、窗口服务、布局、3D 场景、材质与音频，并明确当前构建只有 SampleScene。

AGENTS.md：新增标准 AI 协作入口，记录阅读顺序、当前事实、用户确认约束、平台边界、数据与密钥保护、验证要求。AGENT.md 改为兼容入口，保留原窗口规则并指向同一份说明。

docs/ARCHITECTURE.md：以当前入口/生命周期重写，包含 12 项风险、触发证据、建议和验证限制。把目标结构明确标成“拟议”，不混成现有实现。

docs/SCRIPTS.md、docs/API_EVENTS.md：重建维护导航，移除不存在的旧路径/签名，加入 Platform、Layout、场景循环与 SakuraWeather。避免复制大量会再次过期的源码细节。

docs/PROJECT_PLAN.md：新版计划可维护源文件；按 M0-M5 编排基础修正、世界往返、交互、资源更新、AI 动作、工具/性能，补充边界、依赖、估算与验收。桌面计划 PDF 来自该文件。

docs/WORK_REPORT_2026-09-21.md：本报告可维护源文件，桌面报告 PDF 来自该文件。

Assets/Scripts/Art/Scenes/README.md：更新现行时间表、音频状态、唯一属性控制者和验证限制。角色 Improved2.0 README 增补当前八材质已引用状态。

旧长教程 Markdown/HTML：在开头明确标为历史教程，保留教学正文和旧截图，不再作为当前时序验收依据；没有悄悄将旧截图标成当前渲染。

本地 .ai：新增 CONTEXT.md 术语和 desktop-station-integration.md 规格；既有 Shader/列车规格增加当前状态更正，保留历史来源。维持 .ai 的 Git 忽略规则；关键事实也写入公开 docs，克隆仓库不会依赖本地缓存才能理解项目。

<!-- pagebreak -->

# 02 · 最重要的发现与建议

发现一：项目不是“只有旧桌宠”。窗口接口层、URP、站台列车、VFX、A 版材质和音频已有实际资产/源码。计划应复用这些投入，不从零重做。

发现二：项目还不是完整“桌宠 + 世界”产品。Build Settings 只有 SampleScene；没有 WorldService、资源句柄生命周期和桌面至世界切换。下一里程碑必须证明正常往返及失败后恢复。

发现三：原生隔离仍有缺口。DesktopContextManager 直接调用 user32；原生 DLL 有 ABI/能力检查，但仓库未见对应 C/C++ 源码。建议补齐前台感知边界、可重建信息和当前 x64 冒烟，不把平台层简单标为完全结束。

发现四：聊天链会影响未来跨场景扩展。先保存用户消息再追加当前消息可能重复；异步过程读取可变会话，有串角色风险；错误文本仍按回复保存和增长关系。建议先做固定身份、turnId、取消、类型化结果和请求终态。

发现五：用户改名与角色归属采用不一致更新。角色按 UserName 查找，用户更新只改用户记录；扩展世界存档前应做稳定 ID、事务和迁移版本，而非继续叠加按名字关联的表。

发现六：旧说明也落后于最近美术更新。时间已从 30/45/65 改为 20/35/45；当前 3DScene 已引用八份 A 材质，Timeline 与 movementLoop 已有音频。历史“空轨道/未应用材质”的结论不再适用。

发现七：性能数字和授权状态不能照搬。SakuraWeather 表格只是局部实验且有空项；本次未发现根目录 LICENSE，不能仅据旧 README 承诺全项目 MIT 或素材可再分发。

建议路线：M0 修基础 → M1 本地世界往返 → M2 手动交互/欢迎 → M3 资源交付 → M4 AI 动作 → M5 性能/工具。把平台阻塞修正与性能基线提前，避免全仓目录迁移和多项高风险技术同时接入。

<!-- pagebreak -->

# 03 · 验证、限制与交接

本次检查的事实依据：
- Unity 版本、URP/VFX/Timeline 依赖来自 ProjectSettings/Packages。
- 场景脚本挂载和材质/音频状态来自 GUID 与序列化资源交叉核对；引用存在不代表运行一定有效。
- 当前时间表来自 SakuramachiSceneLoop 常量、Setup 描述和 Timeline；用户确认后同步文档。
- 数据和请求问题来自 UIManager、AIChat、ChatContextBuilder、ChatMessageService、UserRepository 等调用链。
- README 链接、文档路径、关键状态与时序进行静态检查；PDF 输出经文本提取、页数/边界检查和页面渲染检查。

未覆盖：Unity 主工程编译、Windows Player 构建、外部 API 可用性、原生 ABI 实际调用、多屏/DPI 交互、当前音频听感、材质实际画面和整机性能。这些进入新计划的验收清单，不标为通过。

知识检索仅用于判断任务所需概念：生命周期/取消、稳定身份/事务、ABI、资源所有权和渐进拆分。全局模型已有部分生命周期/拆分基础证据；事务、ABI、请求终态仍缺独立验证。未据项目代码给用户打分，未更新个人模型，也未发起广泛测验。资源系统知识没有对应检索证据，不等于用户不会。

后续 AI 阅读入口：
1. AGENTS.md：不可破坏的约束与阅读规则。
2. README.md：项目现状、启动、构建和导航。
3. docs/ARCHITECTURE.md：风险/证据/目标接缝。
4. docs/PROJECT_PLAN.md：实施顺序和验收。
5. 按任务进入脚本、接口和艺术子模块 README。
6. .ai 仅作本地历史/术语补充，不覆盖当前代码和用户新决定。

现在可以开始 M0。预计近期 M0 + M1 共 9-15 个有效开发日；应以当前 Player 基线和失败恢复验收推进，不能仅以写完代码作为完成。对后续架构的 Additive 方案、动作资产和包版本仍保持显式假设，实施时核验。

交付采用新文件名：AI_Desktop_Companion_Updated_Plan_2026-09-21.pdf 和 AI_Desktop_Companion_Work_Report_2026-09-21.pdf。原 AI_Desktop_Companion_Unity2021_Project_Plan.pdf 未覆盖。
