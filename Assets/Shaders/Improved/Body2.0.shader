Shader "GenshinToon/BodyV2"
{
    Properties
    {
        // =========================================================
        // Base
        // =========================================================
        [Header(Base)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        // 如果模型存在单面裙摆、衣服等，可以改成 Off
        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull("Cull", Float) = 2

        // 兼容你原来背面使用 UV1 的设计
        [Toggle]
        _UseUV1Backface("Use UV1 For Backface", Range(0,1)) = 0


        // =========================================================
        // Material Data
        // =========================================================
        [Header(Material Maps)]
        _MetallicMap("Metallic Map", 2D) = "black" {}
        _RoughnessMap("Roughness Map", 2D) = "white" {}

        _MetallicScale("Metallic Scale", Range(0,1)) = 1
        _RoughnessScale("Roughness Scale", Range(0,1)) = 1


        // =========================================================
        // Toon Shadow
        // =========================================================
        [Header(Toon Shadow)]
        _RampTex("Ramp Texture", 2D) = "white" {}

        [Toggle(_USE_RAMP_SHADOW)]
        _UseRampShadow("Use Ramp Shadow", Float) = 1

        // 阴影开始出现的位置
        _ShadowPosition("Shadow Position", Range(0,1)) = 0.55

        // 阴影边缘柔和程度
        _ShadowSoftness("Shadow Softness", Range(0.001,0.5)) = 0.08

        // 顶点色 G 对阴影边界的影响程度
        _ShadowRampWidth("Vertex Shadow Bias", Range(0,0.5)) = 0.1

        [Toggle]
        _UseVertexShadowBias("Use Vertex Color G", Range(0,1)) = 0

        // 实时阴影的影响强度
        _ReceiveShadowStrength("Receive Shadow Strength", Range(0,1)) = 0.75

        // Ramp 行
        _RampID("Ramp ID", Range(1,5)) = 1

        // 0 = Day
        // 1 = Night
        _DayOrNight("Day Or Night", Range(0,1)) = 0


        // =========================================================
        // Scene Lighting
        // =========================================================
        [Header(Scene Lighting)]

        // 主光颜色对人物的染色程度
        _MainLightColorInfluence(
            "Main Light Color Influence",
            Range(0,1)
        ) = 0.35

        // 环境 SH 光照，仅作为很弱的填充光
        _IndirectLightStrength(
            "Indirect Light Strength",
            Range(0,1)
        ) = 0.12


        // =========================================================
        // Stylized Specular
        // =========================================================
        [Header(Toon Specular)]

        _SpecColor(
            "Non-Metal Specular Color",
            Color
        ) = (1,1,1,1)

        _SpecIntensity(
            "Specular Intensity",
            Range(0,2)
        ) = 0.2

        // Roughness = 1 时使用
        _SpecExponentMin(
            "Rough Spec Exponent",
            Range(1,64)
        ) = 8

        // Roughness = 0 时使用
        _SpecExponentMax(
            "Smooth Spec Exponent",
            Range(16,256)
        ) = 128

        // 将传统 Specular 卡通化
        _SpecThreshold(
            "Specular Threshold",
            Range(0,1)
        ) = 0.55

        _SpecSoftness(
            "Specular Softness",
            Range(0.001,0.5)
        ) = 0.08

        _MetalSpecBoost(
            "Metal Specular Boost",
            Range(0,3)
        ) = 1.2


        // =========================================================
        // Rim
        // =========================================================
        [Header(Rim Light)]

        [Toggle(_USE_RIM_LIGHT)]
        _UseRimLight("Use Rim Light", Float) = 1

        _RimColor(
            "Rim Color",
            Color
        ) = (0.7,0.75,0.9,1)

        _RimIntensity(
            "Rim Intensity",
            Range(0,1)
        ) = 0.06

        _RimPower(
            "Rim Power",
            Range(0.5,10)
        ) = 4
    }



    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }


        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


        // ---------------------------------------------------------
        // Texture declarations
        // 不再把 sampler 塞进 UnityPerMaterial CBUFFER
        // ---------------------------------------------------------

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        TEXTURE2D(_MetallicMap);
        SAMPLER(sampler_MetallicMap);

        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);

        TEXTURE2D(_RampTex);
        SAMPLER(sampler_RampTex);


        CBUFFER_START(UnityPerMaterial)

            float4 _BaseMap_ST;
            float4 _BaseColor;

            float _Cull;
            float _UseUV1Backface;

            float _MetallicScale;
            float _RoughnessScale;

            float _ShadowPosition;
            float _ShadowSoftness;
            float _ShadowRampWidth;
            float _UseVertexShadowBias;

            float _ReceiveShadowStrength;

            float _RampID;
            float _DayOrNight;

            float _MainLightColorInfluence;
            float _IndirectLightStrength;

            float4 _SpecColor;
            float _SpecIntensity;

            float _SpecExponentMin;
            float _SpecExponentMax;

            float _SpecThreshold;
            float _SpecSoftness;

            float _MetalSpecBoost;

            float4 _RimColor;
            float _RimIntensity;
            float _RimPower;

        CBUFFER_END



        // =========================================================
        // Vertex Data
        // =========================================================

        struct UniversalAttributes
        {
            float4 positionOS : POSITION;

            float3 normalOS : NORMAL;

            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;

            float4 color : COLOR;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        struct UniversalVaryings
        {
            float4 positionCS : SV_POSITION;

            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;

            float3 normalWS : TEXCOORD2;
            float3 positionWS : TEXCOORD3;

            float4 color : TEXCOORD4;

            float4 shadowCoord : TEXCOORD5;

            half fogFactor : TEXCOORD6;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };



        // =========================================================
        // Vertex Shader
        // =========================================================

        UniversalVaryings MainVertexShader(
            UniversalAttributes input
        )
        {
            UniversalVaryings output;

            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);


            VertexPositionInputs positionInput =
                GetVertexPositionInputs(
                    input.positionOS.xyz
                );


            VertexNormalInputs normalInput =
                GetVertexNormalInputs(
                    input.normalOS
                );


            output.positionCS =
                positionInput.positionCS;

            output.positionWS =
                positionInput.positionWS;

            output.normalWS =
                normalInput.normalWS;


            output.uv0 = input.uv0;
            output.uv1 = input.uv1;

            output.color = input.color;


            // -------------------------------
            // 正确生成主光源阴影坐标
            // -------------------------------

            output.shadowCoord =
                GetShadowCoord(
                    positionInput
                );


            output.fogFactor =
                ComputeFogFactor(
                    positionInput.positionCS.z
                );


            return output;
        }



        // =========================================================
        // Fragment Shader
        // =========================================================

        half4 MainFragmentShader(
            UniversalVaryings input,
            FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC
        ) : SV_TARGET
        {
            UNITY_SETUP_INSTANCE_ID(input);


            // =====================================================
            // Front / Back Face
            // =====================================================

            half frontMask =
                IS_FRONT_VFACE(
                    isFrontFace,
                    1.0h,
                    0.0h
                );


            half normalSign =
                IS_FRONT_VFACE(
                    isFrontFace,
                    1.0h,
                    -1.0h
                );


            // 如果开启背面 UV1，
            // 背面使用 uv1，否则统一 uv0
            float useBackUV =
                (1.0h - frontMask)
                * _UseUV1Backface;


            float2 sourceUV =
                lerp(
                    input.uv0,
                    input.uv1,
                    useBackUV
                );


            float2 uv =
                sourceUV * _BaseMap_ST.xy
                + _BaseMap_ST.zw;



            // =====================================================
            // Texture Sampling
            // =====================================================

            half4 baseMap =
                SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    uv
                )
                * _BaseColor;


            half metallic =
                SAMPLE_TEXTURE2D(
                    _MetallicMap,
                    sampler_MetallicMap,
                    uv
                ).r;


            metallic =
                saturate(
                    metallic
                    * _MetallicScale
                );


            half roughness =
                SAMPLE_TEXTURE2D(
                    _RoughnessMap,
                    sampler_RoughnessMap,
                    uv
                ).r;


            roughness =
                saturate(
                    roughness
                    * _RoughnessScale
                );



            // =====================================================
            // Vector
            // =====================================================

            half3 N =
                normalize(input.normalWS)
                * normalSign;


            half3 V =
                SafeNormalize(
                    GetWorldSpaceViewDir(
                        input.positionWS
                    )
                );



            // =====================================================
            // Main Light + Real-time Shadow
            // =====================================================

            Light mainLight =
                GetMainLight(
                    input.shadowCoord
                );


            half3 L =
                normalize(
                    mainLight.direction
                );


            half NdotL =
                dot(N, L);


            half halfLambert =
                NdotL * 0.5h
                + 0.5h;



            // 这是你原 Shader 最大的缺失之一：
            // 现在真正读取主光源 ShadowMap
            half shadowAttenuation =
                lerp(
                    1.0h,
                    mainLight.shadowAttenuation,
                    _ReceiveShadowStrength
                );



            // =====================================================
            // Vertex Shadow Bias
            // =====================================================

            half vertexShadowBias =
                (input.color.g * 2.0h - 1.0h)
                * _ShadowRampWidth;


            vertexShadowBias *=
                _UseVertexShadowBias;



            // =====================================================
            // Toon Shadow Input
            // =====================================================

            half toonLight =
                saturate(

                    halfLambert
                    * shadowAttenuation

                    + vertexShadowBias
                );



            // =====================================================
            // Shadow Edge
            // =====================================================

            half softness =
                max(
                    _ShadowSoftness,
                    0.001h
                );


            half rampU =
                smoothstep(

                    _ShadowPosition
                    - softness,

                    _ShadowPosition
                    + softness,

                    toonLight
                );



            // =====================================================
            // Ramp Texture
            // =====================================================

            half rampID =
                clamp(

                    floor(
                        _RampID
                        + 0.5h
                    ),

                    1.0h,
                    5.0h
                );


            // 你原来 Ramp 的布局继续保留
            //
            // Day:
            // 0.95 / 0.85 / 0.75 / 0.65 / 0.55
            //
            // Night:
            // 0.45 / 0.35 / 0.25 / 0.15 / 0.05

            half rampNightV =
                0.45h
                - (rampID - 1.0h)
                * 0.1h;


            half rampDayV =
                rampNightV
                + 0.5h;


            half3 rampDayColor =
                SAMPLE_TEXTURE2D(

                    _RampTex,
                    sampler_RampTex,

                    half2(
                        rampU,
                        rampDayV
                    )

                ).rgb;


            half3 rampNightColor =
                SAMPLE_TEXTURE2D(

                    _RampTex,
                    sampler_RampTex,

                    half2(
                        rampU,
                        rampNightV
                    )

                ).rgb;


            half3 rampColor =
                lerp(

                    rampDayColor,
                    rampNightColor,

                    _DayOrNight
                );



            // =====================================================
            // Diffuse
            // =====================================================

            half3 diffuseFactor;


            #if defined(_USE_RAMP_SHADOW)

                diffuseFactor =
                    rampColor;

            #else

                half simpleDiffuse =
                    lerp(
                        0.65h,
                        1.0h,
                        rampU
                    );

                diffuseFactor =
                    half3(
                        simpleDiffuse,
                        simpleDiffuse,
                        simpleDiffuse
                    );

            #endif



            // 主光颜色不完全作用于角色
            //
            // 这样人物会受到场景光色影响，
            // 但不会突然变成强 PBR 风格
            half3 mainLightTint =
                lerp(

                    half3(1,1,1),

                    mainLight.color,

                    _MainLightColorInfluence
                );


            half3 finalColor =

                baseMap.rgb

                * diffuseFactor

                * mainLightTint;



            // =====================================================
            // Environment / SH Indirect Lighting
            // =====================================================

            // 没有 LightMap 并不意味着不能有环境填充光。
            //
            // SampleSH 来自场景 Ambient / Light Probe，
            // 这里只给很弱的影响。

            half3 indirectLight =
                SampleSH(N);


            indirectLight =
                max(
                    indirectLight,
                    half3(0,0,0)
                );


            finalColor +=

                baseMap.rgb

                * indirectLight

                * _IndirectLightStrength;



            // =====================================================
            // Stylized Specular
            // =====================================================

            half3 H =
                SafeNormalize(
                    L + V
                );


            half NdotH =
                saturate(
                    dot(
                        N,
                        H
                    )
                );


            // ---------------------------------
            // Roughness 控制高光大小
            //
            // Roughness 低：
            // exponent 高 -> 小而硬
            //
            // Roughness 高：
            // exponent 低 -> 大而柔
            // ---------------------------------

            half specExponent =
                lerp(

                    _SpecExponentMax,
                    _SpecExponentMin,

                    roughness
                );


            half rawSpecular =
                pow(
                    max(
                        NdotH,
                        0.0001h
                    ),

                    specExponent
                );



            // ---------------------------------
            // 不直接使用连续 Blinn-Phong，
            // 而是将高光重新 Toon 化。
            // ---------------------------------

            half toonSpecular =
                smoothstep(

                    _SpecThreshold,

                    _SpecThreshold
                    + _SpecSoftness,

                    rawSpecular
                );



            // 高光不应该出现在完全背光区域
            half specLightMask =
                saturate(
                    NdotL
                    * 4.0h
                );


            specLightMask *=
                shadowAttenuation;



            // Roughness 高时降低高光能量
            half roughSpecAttenuation =
                lerp(

                    1.0h,
                    0.12h,

                    roughness
                );



            // ---------------------------------
            // Metallic 的使用方式：
            //
            // Non-Metal:
            //     使用 SpecColor
            //
            // Metal:
            //     使用 BaseColor 作为高光颜色
            //
            // 不使用完整 PBR GGX。
            // ---------------------------------

            half3 specularColor =
                lerp(

                    _SpecColor.rgb,

                    baseMap.rgb,

                    metallic
                );


            half metalBoost =
                lerp(

                    1.0h,

                    _MetalSpecBoost,

                    metallic
                );


            half specularStrength =

                toonSpecular

                * specLightMask

                * roughSpecAttenuation

                * _SpecIntensity

                * metalBoost;



            finalColor +=

                specularColor

                * specularStrength

                * mainLightTint;



            // =====================================================
            // Very Soft Rim
            // =====================================================

            #if defined(_USE_RIM_LIGHT)

                half NdotV =
                    saturate(
                        dot(
                            N,
                            V
                        )
                    );


                half rim =
                    pow(

                        1.0h
                        - NdotV,

                        _RimPower
                    );


                // 防止整个模型一圈发光
                rim *=
                    saturate(
                        NdotL
                        + 0.35h
                    );


                rim *=
                    _RimIntensity;


                finalColor +=
                    _RimColor.rgb
                    * rim;

            #endif



            // =====================================================
            // Fog
            // =====================================================

            finalColor =
                MixFog(
                    finalColor,
                    input.fogFactor
                );


            return half4(
                finalColor,
                1.0h
            );
        }


        ENDHLSL



        // =========================================================
        // Forward
        // =========================================================

        Pass
        {
            Name "UniversalForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            Cull [_Cull]

            ZWrite On
            ZTest LEqual


            HLSLPROGRAM

            #pragma vertex MainVertexShader
            #pragma fragment MainFragmentShader


            // Main Light Shadow
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #pragma multi_compile_instancing


            #pragma shader_feature_local_fragment _USE_RAMP_SHADOW
            #pragma shader_feature_local_fragment _USE_RIM_LIGHT

            ENDHLSL
        }



        // =========================================================
        // Shadow Caster
        // =========================================================

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }


            ZWrite On
            ZTest LEqual

            ColorMask 0

            Cull [_Cull]


            HLSLPROGRAM

            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment

            #pragma multi_compile_instancing

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW


            float3 _LightDirection;
            float3 _LightPosition;



            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };



            float4 GetCharacterShadowPositionHClip(
                ShadowAttributes input
            )
            {
                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );


                float3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );


                float3 lightDirectionWS;


                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)

                    lightDirectionWS =
                        normalize(
                            _LightPosition
                            - positionWS
                        );

                #else

                    lightDirectionWS =
                        _LightDirection;

                #endif



                float4 positionCS =
                    TransformWorldToHClip(

                        ApplyShadowBias(

                            positionWS,

                            normalWS,

                            lightDirectionWS
                        )
                    );



                #if UNITY_REVERSED_Z

                    positionCS.z =
                        min(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );

                #else

                    positionCS.z =
                        max(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );

                #endif


                return positionCS;
            }



            ShadowVaryings ShadowVertex(
                ShadowAttributes input
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);


                ShadowVaryings output;


                output.positionCS =
                    GetCharacterShadowPositionHClip(
                        input
                    );


                return output;
            }



            half4 ShadowFragment(
                ShadowVaryings input
            ) : SV_TARGET
            {
                return 0;
            }


            ENDHLSL
        }
    }
}