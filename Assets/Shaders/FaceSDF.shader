Shader "GenshinToon/FaceSDF" // 父着色器
{
    Properties // 开放给外界的属性
    {
        [Header(Textures)]
        _BaseMap("Base Map",2D) = "white" {} // 纹理贴图
        
        [header(Shadow Options)]
        [Toggle (_USE_SDF_SHADOW)] _UseSDFShadow ("Use SDF Shadow",Range(0,1)) = 1 // sdf开关
        _SDF ("SDF",2D) = "white" {} // 距离场阴影
        _ShadowMask ("ShadowMask",2D) = "white" {} //阴影遮罩
        _ShadowColor ("Shadow Color",Color) = (1,0.87,0.87,1) //阴影颜色
        
        [Header(Header Direction)]
        [HideInInspector]HeadForward ("Head Forward" , Vector) = (0,0,1,0) //面部前方
        [HideInInspector]_HeadRight ("Head Right" , Vector) = (1,0,0,0) //面部右方
        [HideInInspector]_HeadUp ("Head Up" , Vector) = (0,1,0,0) //面部上方
        
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
        
            #pragma shader_feature_local _USE_SDF_SHADOW //SDF开关
        
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // 核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // 光照库
        
            CBUFFER_START(UnityPerMaterial) // 常量缓冲区开始
                //Textures
                sampler2D _BaseMap; // 基础纹理
        
                //Shadow Options
                sampler2D _SDF; // 距离场纹理
                sampler2D _ShadowMask; // 阴影遮罩
                float4 _ShadowColor; //阴影颜色
        
                //Head Direction
                float3 _HeadForward; // 面部前方
                float3 _HeadRight; // 面部右方
                float3 _HeadUp; // 面部上方

            CBUFFER_END // 常量缓冲区结束
        
           
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
                };

                // 片元着色器输入参数，由顶点着色器返回
                struct Varyings
                {
                    float4 positionCS : SV_POSITION; // 裁剪空间顶点坐标
                    float2 uv0 : TEXCOORD0; //第一套纹理坐标
                    float3 normalWS : TEXCOORD1; // 世界坐标法线
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
                    
                    //normal
                    VertexNormalInputs VertexNormalInputs = GetVertexNormalInputs(input.normalOS); // 转换法线空间
                    output.normalWS = VertexNormalInputs.normalWS;
                    
                    return output;
                }
            
                //片元着色器函数：处理像素，返回颜色(RGBA)
                half4 MainFragmentShader(Varyings input) : SV_TARGET
                {                    
                    Light light = GetMainLight(); // 获取主光源
                    
                    //Textures Info
                    half4 baseMap = tex2D(_BaseMap,input.uv0); // 采样纹理贴图
                    half4 shadowMask = tex2D(_ShadowMask,input.uv0); //采样阴影遮罩
                    
                    //Normalize Vector
                    half3 N = normalize(input.normalWS); // 归一化法线
                    half3 L = normalize(light.direction); // 归一化光源
                    half Ndotl = dot(N,L);   
                    half3 headUpDir = normalize(_HeadUp); // 归一化面部上方
                    half3 headRightDir = normalize(_HeadRight); // 归一化面部右侧
                    half3 headForwardDir = normalize(_HeadForward); // 归一化面部前方
                    
                    //Lambert
                    half lambert = Ndotl; // (-1,1) 
                    half halflambert = lambert * 0.5 + 0.5; //(0,1)
                    halflambert *= pow(halflambert,2); // 调整光照亮度
                    
                    //Face Shadow
                    half3 LpU = dot(L, headUpDir) / pow(length(headUpDir), 2) * headUpDir; // 计算光源方向在面部上方的投影
                    half3 LpHeadHorizon = normalize(L- LpU); // 光照方向在头部水平面上的投影
                    half value = acos(dot(LpHeadHorizon, headRightDir)) / 3.141592654; // 计算光照方向与面部右方的夹角
                    half exposeRight = step(value, 0.5); // 判断光照是来自右侧还是左侧
                    half valueR = pow(1 - value * 2, 3); // 右侧阴影强度
                    half valueL = pow(value * 2 - 1, 3); // 左侧阴影强度
                    half mixValue = lerp(valueL, valueR, exposeRight); // 混合阴影强度
                    half sdfLeft = tex2D(_SDF, half2(1 - input.uv0.x, input.uv0.y)).r; // 左侧距离场
                    half sdfRight = tex2D(_SDF, input.uv0).r; // 右侧距离场
                    half mixSdf = lerp(sdfRight, sdfLeft, exposeRight); // 采样SDF纹理
                    half sdf = step(mixValue, mixSdf); // 计算硬边界阴影
                    sdf = lerp(0, sdf, step(0, dot(LpHeadHorizon, headForwardDir))); // 计算右侧阴影
                    sdf *= shadowMask.g; // 使用G通道控制阴影强度
                    sdf = lerp(sdf, 1, shadowMask.a); // 使用A通道作为阴影遮罩
                    
                    //Merge Color
                    #if _USE_SDF_SHADOW
                        half3 finalColor = lerp(_ShadowColor.rgb * baseMap.rgb , baseMap.rgb , sdf); //合并最终颜色
                    #else
                        half3 finalColor = baseMap.rgb * halflambert; //采用ramp阴影
                    #endif
                    
                    
                    return float4(finalColor,1);
                }
            ENDHLSL // 着色器程序结束
        }
    }
}
