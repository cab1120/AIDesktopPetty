Shader "GenshinToon/Body" // 父着色器
{
    Properties // 开放给外界的属性
    {
        [Header(Textures)]
        _BaseMap("Base Map",2D) = "white" {} // 纹理贴图
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _LightMap("Light Map",2D) = "white" {} // 光照贴图
        [Toggle(_USE_LIGHTMAP_AO)] _UselightMapAO ("Use LightMap AO",Range(0,1)) = 0 //AO开关
        
        
        [Header(Ramp Shadow)]
        _RampTex("Ramp Tex",2D) = "white"{} // 色阶阴影贴图
        [Toggle(_USE_RAMP_SHADOW)] _UseRampShadow ("Use Ramp Shadow",Range(0,1)) = 1 //色阶阴影开关
        _ShadowRampWidth ("Shadow Ramp Width",Float) = 1 //色阶阴影宽度
        _ShadowPosition ("Shadow Position",Float) = 0.55 //阴影位置
        _ShadowSoftness ("Shadow Softness",Float) = 0.5 //阴影柔和都
        [Toggle] _UseRampShadow2 ("Use Ramp Shadow 2",Range(0,1)) = 1 //使用第二行Ramp阴影开关
        [Toggle] _UseRampShadow3 ("Use Ramp Shadow 3",Range(0,1)) = 1 //使用第三行Ramp阴影开关
        [Toggle] _UseRampShadow4 ("Use Ramp Shadow 4",Range(0,1)) = 1 //使用第四行Ramp阴影开关
        [Toggle] _UseRampShadow5 ("Use Ramp Shadow 5",Range(0,1)) = 1 //使用第五行Ramp阴影开关
        _RampID ("Ramp ID", Range(1,5)) = 1 // ramp贴图行数
        
        [Header(Lighting Options)]
        _DayOrNight ("Day or Night",Range(0,1)) = 0 //日夜切换参数
        
    }
    SubShader // 子着色器
    {
        Tags // 标签
        {
            "RenderPipeline" = "UniversalRenderPipeline" // 指定渲染管线：URP
            "RenderType" = "Opaque" // 指定渲染类型：不透明
        }
        
        HLSLINCLUDE // 公共代码块开始
            // 预处理指令，头文件，常量定义，函数定义
            #pragma multi_compile _MAIN_LIGHT_SHADOWS // 主光源阴影
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_CASCADE // 主光源阴影级联
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_SCREEN // 主光源阴影屏幕空间

            #pragma multi_compile_fragment _LIGHT_LAYERS // 光照层
            #pragma multi_compile_fragment _LIGHT_COOKIES // 光照饼干
            #pragma multi_compile_fragment _SCREEN_SPACE_OCCLUSION // 屏幕空间遮挡
            #pragma multi_compile_fragment _ADDITIONAL_LIGHT_SHADOWS // 额外光源阴影
            #pragma multi_compile_fragment _SHADOWS_SOFT // 阴影软化
            #pragma multi_compile_fragment _REFLECTION_PROBE_BLENDING // 反射探针混合
            #pragma multi_compile_fragment _REFLECTION_PROBE_BOX_PROJECTION // 反射探针盒投影
        
            #pragma shader_feature_local _USE_LIGHTMAP_AO  //AO开关
            #pragma shader_feature_local _USE_RAMP_SHADOW  //色阶阴影开关
        
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // 光照库
        
        
            CBUFFER_START(UnityPerMaterial) // 常量缓冲区开始
                //Textures
                sampler2D _BaseMap; // 基础纹理
                sampler2D _LightMap; // 光照贴图
                float4 _BaseColor;
        
                //Ramp
                sampler2D _RampTex; // 色阶阴影贴图
                float _ShadowRampWidth; // 阴影边缘宽度
                float _ShadowPosition; // 阴影位置
                float _ShadowSoftness; // 阴影柔和都
                float _UseRampShadow2; //使用第2行Ramp阴影开关
                float _UseRampShadow3; //使用第3行Ramp阴影开关
                float _UseRampShadow4; //使用第4行Ramp阴影开关
                float _UseRampShadow5; //使用第5行Ramp阴影开关
                float _RampID; //ramp贴图行数
        
                //Lighting Options
                float _DayOrNight; // 日夜切换开关
            CBUFFER_END // 常量缓冲区结束
        
            // 官方版本的RampShadowID函数
            float RampShadowID(float input, float useShadow2, float useShadow3, float useShadow4, float useShadow5, 
                float shadowValue1, float shadowValue2, float shadowValue3, float shadowValue4, float shadowValue5)
            {
                // 根据input值将模型分为5个区域
                float v1 = step(0.6, input) * step(input, 0.8); // 0.6-0.8区域
                float v2 = step(0.4, input) * step(input, 0.6); // 0.4-0.6区域
                float v3 = step(0.2, input) * step(input, 0.4); // 0.2-0.4区域
                float v4 = step(input, 0.2);                    // 0-0.2区域

                // 根据开关控制是否使用不同材质的值
                float blend12 = lerp(shadowValue1, shadowValue2, useShadow2);
                float blend15 = lerp(shadowValue1, shadowValue5, useShadow5);
                float blend13 = lerp(shadowValue1, shadowValue3, useShadow3);
                float blend14 = lerp(shadowValue1, shadowValue4, useShadow4);

                // 根据区域选择对应的材质值
                float result = blend12;                // 默认使用材质1或2
                result = lerp(result, blend15, v1);    // 0.6-0.8区域使用材质5
                result = lerp(result, blend13, v2);    // 0.4-0.6区域使用材质3
                result = lerp(result, blend14, v3);    // 0.2-0.4区域使用材质4
                result = lerp(result, shadowValue1, v4); // 0-0.2区域使用材质1

                return result;
            }
        ENDHLSL // 公共代码块结束
        
        Pass // 渲染通道 （渲染流程）
        {
            Name "UniversalForward"
            Tags // 标签
            {
                "LightMode" = "UniversalForward" //光照模型 ：向前渲染
            }
            
            HLSLPROGRAM // 着色器程序开始
                #pragma  vertex MainVertexShader // 申明顶点着色器函数
                #pragma  fragment MainFragmentShader // 声明片元着色器函数

                //顶点着色器输入参数
                struct Attributes
                {
                    float4 positionObjectSpace : POSITION; // 本地空间顶点坐标
                    float2 uv0 : TEXCOORD0; // 第一套纹理坐标
                    float3 normalOS : NORMAL; // 本地坐标法线
                    float4 color : COLOR0; //顶点颜色
                };

                // 片元着色器输入参数，由顶点着色器返回
                struct Varyings
                {
                    float4 positionCS : SV_POSITION; // 裁剪空间顶点坐标
                    float2 uv0 : TEXCOORD0; //第一套纹理坐标
                    float3 normalWS : TEXCOORD1; // 世界坐标法线
                    float4 color : TEXCOORD2; // 顶点颜色
                };
            
                //顶点着色器函数：处理顶点，返回裁剪空间坐标
                Varyings MainVertexShader(Attributes input)
                {
                    Varyings output; // 定义顶点着色器返回值
                    
                    //Position
                    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionObjectSpace.xyz); // 转换定点空间
                    output.positionCS = vertexInput.positionCS; // 拿到裁剪空间顶点坐标
                    
                    //UV   
                    output.uv0 = input.uv0; // 拿到uv坐标
                    
                    //color
                    output.color = input.color; // 传递顶点颜色
                    
                    //normal
                    VertexNormalInputs VertexNormalInputs = GetVertexNormalInputs(input.normalOS); // 转换法线空间
                    output.normalWS = VertexNormalInputs.normalWS;
                    
                    return output;
                }
            
                //片元着色器函数：处理像素，返回颜色(RGBA)
                half4 MainFragmentShader(Varyings input) : SV_TARGET
                {                    
                    Light light = GetMainLight(); // 获取主光源
                    half4 vertexColor = input.color; // 获取顶点颜色
                    
                    //Textures Info
                    half4 baseMap = tex2D(_BaseMap,input.uv0) * _BaseColor; // 采样纹理贴图
                    half4 lightMap = tex2D(_LightMap,input.uv0); // 采样光照贴图
                    
                    //Normalize Vector
                    half3 N = normalize(input.normalWS); // 归一化法线
                    half3 L = normalize(light.direction); // 归一化光源
                    half Ndotl = dot(N,L);   
                    
                    //Lambert
                    half lambert = Ndotl; // (-1,1) 
                    half halflambert = lambert * 0.5 + 0.5; //(0,1)
                    halflambert *= pow(halflambert,1); // 调整光照亮度
                    half lamertstep = smoothstep(0.01,0.4,halflambert); // 在[0.01，0.4]进行平滑插值
                    half shadowFactor = lerp(0,halflambert,lamertstep); // 计算阴影因子
                    
                    //AO
                    #if _USE_LIGHTMAP_AO
                        half ambient = lightMap.g; // 环境光
                    #else
                        half ambient = halflambert;
                    #endif
                    
                    
                    half rampWidthFactor = vertexColor.g * 2 * _ShadowRampWidth; //使用顶点颜色g通道控制ramp宽度
                    half shadowPosition = (_ShadowPosition - shadowFactor) / _ShadowPosition; //带入阴影因子计算阴影位置
                    half shadow = (ambient + halflambert) * 0.5; // 环境光遮蔽
                    shadow = lerp(shadow , 1 , step(0.95 , ambient)); // 非常亮的区域强制全亮
                    shadow = lerp(shadow , 0 , step(ambient , 0.05)); // 非常暗的区域强制全暗
                    half isShadowArea = step(shadow,_ShadowPosition); // 判断是否处于阴影区域
                    half shadowDepth = saturate((_ShadowPosition - shadow) / _ShadowPosition); // 阴影深度
                    shadowDepth = pow(shadowDepth,_ShadowSoftness); //根据柔和都调整阴影深度
                    shadowDepth = min(shadowDepth,1); //限制阴影深度不超过1
                    
                    //Ramp
                    half rampU = 1 - saturate(shadowDepth / rampWidthFactor); //计算ramp采样的横坐标(柔和阴影边缘)
                    //half rampID = RampShadowID(lightMap.a,_UseRampShadow2,_UseRampShadow3,_UseRampShadow4,_UseRampShadow5,1,2,3,4,5); //根据lightmap通道选择rampID
                    half rampID = _RampID; //直接指定rampID
                    half rampV = 0.45 - (rampID - 1) * 0.1; //根据rampID计算v坐标
                    half2 rampDayUV = half2(rampU,rampV + 0.5); //构建白天的ramp的uv坐标
                    half2 rampNightUV = half2(rampU,rampV); //构建夜晚的ramp的uv坐标
                    half3 rampDayColor = tex2D(_RampTex,rampDayUV).rgb; // 采样白天的ramp图的颜色
                    half3 rampNightColor = tex2D(_RampTex,rampNightUV).rgb; // 采样夜晚的ramp图的颜色
                    half3 rampColor = lerp(rampDayColor,rampNightColor,_DayOrNight); //根据日夜开关切换颜色
                    
                    //Merge Color
                    #if _USE_RAMP_SHADOW //采用ramp阴影
                        half3 finalColor = baseMap.rgb * rampColor; //采用ramp阴影
                    #else
                        half3 finalColor = baseMap.rgb * halflambert * (shadow+0.1); // 采用lambert阴影
                    #endif
                    
                    return float4(finalColor.rgb,1);
                }
            ENDHLSL // 着色器程序结束
        }
    }
}
