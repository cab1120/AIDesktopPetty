# 樱町场景循环 · 现行说明

2026-09-21 按当前源码、Timeline、3DScene 引用核对。用户确认以现行代码时序为准。Unity 2021.3 / URP 12.1 / Timeline 1.6。

## 时间表

| 时间（秒） | 列车 | 灯和栏杆 |
| --- | --- | --- |
| 0-3 | 隐藏 | 六灯绿/灰同步闪烁，同时落杆 |
| 3-5 | 隐藏 | 红灯，继续落杆 |
| 5-10 | 隐藏 | 红灯，杆落平 |
| 10-20 | 从右黑幕露出并减速 | 红灯、落杆 |
| 20-23 | 停稳 | 红/灰闪烁，同时抬杆 |
| 23-25 | 停站 | 绿灯，继续抬杆 |
| 25-35 | 停站，停稳至发车合计 15 秒 | 绿灯、抬杆 |
| 35-45 | 加速进入左洞 | 绿灯、抬杆 |
| 45-180 | 隐藏并复位 | 绿灯、抬杆 |

常量在 SakuramachiSceneLoop：CycleSeconds=180、RevealTime=10、StopTime=20、DepartureTime=35、HiddenTime=45。六灯统一变色，停稳后抬杆，发车不再次落杆。用户本轮明确保留这一设计。

## 场景与资源

已绑定场景为 Assets/Scenes/3DScene.unity，选择 Scene/Sakuramachi Scene Loop 查看 Timeline。独立场景不在当前 Build Settings，也未接入桌宠世界切换。

当前 Generated/Sakuramachi180.playable 已有四个非空 AudioClip 引用，3DScene 的 movementLoop 也非空。旧“只有空音轨”的说明不适用于当前资源；本次未播放验证听感。

单个 PlayableDirector 统一时间，循环使用 Unscaled Game Time。SakuramachiSceneLoop 根据时间直接求列车、杆、灯状态；跳转不靠累积事件推进。停止/退出预览恢复状态，运行音频由播放状态控制。

## 修改规则

- 同一列车、栏杆和灯只由循环写入。新的欢迎演出仅控制角色/镜头，不添加另一条列车动画轨。
- 行驶声 AudioSource 由脚本独占，固定音效使用 Timeline 声源，避免同声源双重控制。
- 位置相对 Railway Frame；当前右侧黑幕位于负 X，列车沿正 X 行驶。移动黑幕/改模型尺寸后重新校准边界与裁剪平面。
- 列车裁剪 Shader 同时处理车体/描边/深度/阴影；灯用实例 MaterialPropertyBlock，不改共享材质。
- Tools > Sakuramachi > Set Up 180 Second Scene Loop 是配置操作，执行前检查现有绑定与音轨并保留副本；不要用重建覆盖手工编辑。
- 时间、曲线、音频位置或材质变化后，重测关键时间和两轮播放，不能沿用旧版本的通过结论。

## 历史资料

[长篇教学教程](../../../../docs/SakuramachiSceneLoop/樱町场景动画入门与修改教程.md) 及其 HTML 保留旧版截图与数值用于学习，30/45/65 时间表已经过时。本文件是当前时序依据。

.ai 中 2026-09-15 的独立工程验证记录仅为历史。现行代码增加了曲线参数、改变时间并绑定音频；本次静态核对不替代当前 Unity 编译、Player 播放、遮挡和听感验证。
