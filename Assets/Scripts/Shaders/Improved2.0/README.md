# 角色 Shader：方案 A

目标：保留角色贴图、原 Ramp 阴影配色与五官细节，用克制的高光和深色描边与樱町场景统一。

## Ramp 恢复版

七个非眼睛材质已恢复原 `_RampTex`、`_RampID`、日夜参数、阴影位置与柔和度，并恢复原主光染色、阴影强度、环境补光。Ramp 模式沿用原半兰伯特输入和采样公式，不叠乘上一版的冷色阴影乘数，也不额外量化 Ramp。描边与较弱高光仍保留。

| 原材质 | Ramp 行 |
| --- | --- |
| 头、尾巴 | 1 |
| 身体、基础衣、猫耳 | 2 |
| 和服、头发 | 3 |

如果已使用此目录的 A 材质，Unity 重新导入后即生效。自行复制的材质需核对 Ramp 贴图和行号，或使用对应的 A 材质。

## 使用

1. 等 Unity 完成导入与编译，在 Hierarchy 选中角色根对象。
2. 执行 **Tools → Character Style A → Apply to Selected Character**，将其子 Renderer 中的八个原材质换成此目录 `Materials` 中的 A 版。
3. 查看效果后保存场景。支持 Undo；菜单 **Restore Original on Selected Character** 可切回原材质。

菜单只替换选中场景对象中匹配的原材质引用，不修改 FBX、原材质、原 Shader 或背景。也可以手动把 `Materials` 下的对应材质拖入 Renderer。

直接在旧材质上切 Shader 会继承旧的高光、环境光等数值，因此建议使用已调好初始参数的 A 版材质。

| Shader 菜单路径 | 对应材质 | 输入 |
| --- | --- | --- |
| `PetToon/Improved2/Body` | 和服、基础衣、猫耳、身体 | 原基础贴图与 Ramp；可选金属度、粗糙度 |
| `PetToon/Improved2/Face` | 头 | 原基础贴图与 Ramp；沿用 FaceV2 的法线修正 |
| `PetToon/Improved2/Hair` | 头发 | 原基础贴图与 Ramp；可选材质图及模型切线 |
| `PetToon/Improved2/Eye` | 眼睛 | 仅原基础纹理及其 Alpha |
| `PetToon/Improved2/Tail` | 尾巴 | `_BaseColor` 底色，加可选 Ramp 光照查色表；无表面纹理 |

## 与旧版的关系

- 已阅读 Improved 中五个 Shader。Body 保留背面 UV1 选项、可选顶点 G 阴影偏移；Face 保留 `_FaceForwardOS`、面部法线平整与亮度保护；Hair 保留较弱的切线高光；Eye 保留透明度修正、深度测试与透明混合；Tail 保留原橙色参数。
- 衣服紫青渐变和图案来自基础贴图，直接保留。Ramp 模式按原配色和采样计算阴影，不量化贴图颜色。
- 不需要烘焙光照贴图、角色 LightMap、面部 SDF 或 AO 贴图。Ramp 是光照查色表，与模型光照贴图不同。`Ambient probe fill` 是可选环境光填充。
- 身体、脸、头发、尾巴包含投影、深度与法线通道。眼睛保持透明通道，不生成额外实体描边或不透明投影。
- 描边使用 URP 的 `SRPDefaultUnlit` 额外通道，表面使用 `UniversalForwardOnly`，无需修改 Renderer Asset。依据：[Unity URP 12 Pass 标签文档](https://docs.unity3d.com/cn/Packages/com.unity.render-pipelines.universal%4012.1/manual/urp-shaders/urp-shaderlab-pass-tags.html)。

## 常用调节

| 参数 | 作用 |
| --- | --- |
| Use original Ramp palette | A 材质已开启；新建空材质默认关闭，方便无 Ramp 时使用回退分档 |
| Original shadow Ramp / Original Ramp row | 原查色表与行号，保留原五组日/夜布局 |
| Day to night palette | 0 为白天，1 为夜间；中间值混合两组配色 |
| Ramp shadow position / softness | 原采样的阴影位置与过渡宽度 |
| Outline width in pixels | 描边宽度。身体 1.4，头发 1.3，尾巴 0.9，脸 0.55；0 关闭 |
| Fallback shadow / middle multiplier | 仅关闭 Ramp 时使用的暗部和中间档颜色乘数 |
| Fallback shadow / light threshold | 仅关闭 Ramp 时使用的三档边界 |
| Band edge softness | 回退分档和小块高光的边界柔化；不控制 Ramp 的柔和度 |
| Receive realtime shadows | 遮挡投影对角色的影响 |
| Base brightness | 贴图整体亮度，保留原色比例 |
| Face normal flatten / Face forward | 沿用原 FaceV2 面部法线处理；朝向约定与旧材质一致 |

像素宽度以渲染目标为准，渲染比例降低时显示宽度可能变化。反向外壳描边沿原网格法线展开，硬法线接缝、单面布片或很近的镜头可能需要调细对应材质；脸部已使用较细的默认值。

仅跟随主光、实时阴影和少量环境填充，与背景的主光分档思路一致。没有加入逐像素额外灯光或发光轮廓；脸部正面方向仍遵循原 Shader 的对象空间方式，并未新增头骨跟踪依赖。

## 验证

初始目标环境：Unity 2021.3.21f1c1 / URP 12.1.10 / 当前项目 Gamma 色彩空间。
在独立工程中检查五份 Shader 的 D3D11 编译（包括主光各阴影模式）、七个材质与原 Ramp 参数的一致性、Ramp/回退实际像素差异、描边，以及正面、侧面、背面、背光和原车站场景副本。最新结果以 `.ai/shader-validation.txt` 为准。

Ramp 恢复版验证结果：零 Shader 错误，七个材质贴图与参数检查通过；Ramp/回退切换改变 149,626 个像素，描边开关改变 8,145 个像素。

实际车站渲染图位于 `.ai/validation-output/style-a-station.png`，日志为 `.ai/shader-validation.txt`。此前 A/B 生成图是美术概念参考。尚未做最终桌宠窗口的性能测量和所有动画、极近镜头的视觉检查。
