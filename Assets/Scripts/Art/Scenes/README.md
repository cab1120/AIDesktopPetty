# 樱町场景循环

Unity 2021.3 / URP 12.1 / Timeline 1.6。脚本、列车裁剪 Shader、编辑器工具和生成资源全部在此目录。

初学者详细教程：[Markdown 版](../../../../docs/SakuramachiSceneLoop/樱町场景动画入门与修改教程.md) · [离线 HTML 阅读版](../../../../docs/SakuramachiSceneLoop/樱町场景动画入门与修改教程.html)。包含 18 章原理说明、逐步操作、修改练习和故障排查。

## 播放与预览

已配置的场景中选择 `Scene/Sakuramachi Scene Loop`，打开 `Window > Sequencing > Timeline`。进入 Play 模式自动循环；编辑器可拖动时间轴检查任意时刻，退出 Timeline 预览会恢复列车位置、栏杆和材质状态。

若要在另一个相同结构的场景配置，打开该场景，执行 `Tools > Sakuramachi > Set Up 180 Second Scene Loop`，再保存场景。重复执行不会创建第二套控制器，也不会覆盖音效编辑。菜单操作可撤销。

| 时间（秒） | 列车 | 灯和栏杆 |
| --- | --- | --- |
| 0–3 | 右洞内隐藏 | 全部灯罩绿/灰同步闪烁，栏杆开始落下 |
| 3–5 | 隐藏 | 全部转红，继续落杆 |
| 5–10 | 隐藏 | 红灯，栏杆落平 |
| 10–30 | 车头从右侧黑幕露出，逐渐减速至当前位置 | 保持红灯、落杆 |
| 30–33 | 停稳 | 全部红/灰闪烁，同时抬杆 |
| 33–35 | 停站 | 全部转绿，继续抬杆 |
| 35–45 | 停站，合计停留 15 秒 | 绿灯，栏杆抬起 |
| 45–65 | 从零速加速驶入左洞 | 保持绿灯、抬杆 |
| 65–180 | 隐藏并复位，空站等待 | 保持绿灯、抬杆 |

闪烁默认 2 Hz，可在组件中修改。周期使用 Unscaled Game Time，不受 `Time.timeScale` 影响；应用未运行或系统休眠时不会在后台补演动画。

## 音频接口

- Timeline 已预留三个空 Audio Track：警示、栏杆、到站/发车。把 AudioClip 拖入对应轨道并放到轨道名称标注的时间即可，音量和淡入淡出可直接在 Timeline 调整。
- 行驶声：将循环素材拖到组件的 `Movement Loop`，`Movement Audio` 已绑定列车子物体上的独立 AudioSource。音量、音调随速度变化；停稳、暂停或隐藏时停止。空素材不会报错。
- 行驶 AudioSource 由脚本独占，不要再绑定给 Timeline 音频轨道。三个固定音效轨道各有独立 AudioSource。
- 未附带音频素材。编辑器拖动不会启动脚本行驶声；Timeline 自带音频预览由 Timeline 窗口控制。

## 位置与模型坐标

- `Station Position` 是配置时捕获的当前停车点；`Entry Position` 对应车头刚触及右黑幕，`Exit Position` 对应车尾和描边完全穿过左黑幕后的少量余量。
- 这些位置相对于 `Railway Frame`，移动或旋转共同父物体时路径会一起变化。选中组件可看到路径 Gizmo。
- 当前导入资产的 `Tunnel_Dark_Right_DepthMask` 在负 X、Left 在正 X，所以列车实际沿 Unity 正 X 行驶。
- 左栏杆网格朝负 X、右栏杆朝正 X，抬起角度分别是局部 Y=+90° 和 -90°，落平均为 0°。工具根据杆体中心朝向选择向上的旋转方向；不旋转固定底座。
- 完整模型的 `Dynamic_Objects` 已停用，实际绑定的是独立分件。

## 隧道与材质

车身长于洞内可藏空间。列车专用 Shader 保留原三档 Toon 外观，同时在两个黑幕平面外裁掉车体、描边、深度和阴影，避免从山后露出。只为列车生成独立材质，不修改其他物体共享的原材质。

灯色通过 MaterialPropertyBlock 修改 `_AuthoredColor` 与发光属性，六个灯罩始终统一变色，灰色阶段不发光。原始共享灯材质不会被改写。

## 调整与限制

- 这是按确认的时间表制作的完整循环轨道，事件时间常量在 `SakuramachiSceneLoop` 顶部。修改时间表需同时检查运动公式、Timeline 长度和音效位置；不要通过裁切整段控制 Clip 改变事件时刻。
- 改模型尺寸或移动黑幕后，需要重新校准组件的路径端点与 `Tunnel Min/Max X`。裁剪平面应保持垂直于 Railway Frame 的 X 轴。
- 不要再用 Animator、其他 Timeline 或 Update 脚本控制同一列车位置、栏杆旋转或六个灯罩。
- 停稳后即抬杆，发车时不重新落杆，这是本次确认的演出时序。

## 实现结构

- `SakuramachiSceneLoop`：给定时间计算完整场景状态与行驶声参数。
- `SakuramachiLoopTrack` / `SakuramachiLoopClip`：Timeline 绑定、求值与预览恢复。
- `SakuramachiTrainTunnel.shader`：列车专用双平面裁剪。
- `Editor/SakuramachiLoopSetup`：配置现有场景、生成材质/Timeline 和音频绑定。
- `Editor/SakuramachiLoopValidation`：隔离项目中的批处理检查入口，不参与玩家构建。
