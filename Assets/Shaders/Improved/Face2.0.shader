Shader "GenshinToon/FaceV2"
{
    Properties
    {
        // =========================================================
        // Base
        // =========================================================
        [Header(Base)]

        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull("Cull", Float) = 2


        // =========================================================
        // Material
        // =========================================================
        [Header(Material Maps)]

        _MetallicMap("Metallic Map", 2D) = "black" {}
        _RoughnessMap("Roughness Map", 2D) = "white" {}

        // 面部正常情况下 Metallic 应接近 0
        _MetallicScale("Metallic Scale", Range(0,1)) = 0

        _RoughnessScale("Roughness Scale", Range(0,1)) = 1


        // =========================================================
        // Face Lighting
        // =========================================================
        [Header(Face Lighting)]

        // 模型本地空间中的“脸正前方”
        // 如果你的模型 Z+ 不是朝脸前方，需要修改这里
        _FaceForwardOS(
            "Face Forward OS",
            Vector
        ) = (0,0,1,0)

        // 面部法线向 FaceForward 拉近的程度
        _FaceNormalFlatten(
            "Face Normal Flatten",
            Range(0,1)
        ) = 0.72

        // 只对朝脸前方的区域进行扁平化
        // 防止耳朵、脸侧也被强行弄平
        _FaceFlattenStart(
            "Face Flatten Start",
            Range(-1,1)
        ) = 0.15

        // 整体提亮面部阴影
        _FaceLightBias(
            "Face Light Bias",
            Range(-0.5,0.5)
        ) = 0.08


        // =========================================================
        // Toon Shadow
        // =========================================================
        [Header(Toon Shadow)]

        _RampTex("Ramp Texture", 2D) = "white" {}

        [Toggle(_USE_RAMP_SHADOW)]
        _UseRampShadow("Use Ramp Shadow", Float) = 1

        _ShadowPosition(
            "Shadow Position",
            Range(0,1)
        ) = 0.48

        _ShadowSoftness(
            "Shadow Softness",
            Range(0.001,0.5)
        ) = 0.12

        _RampID(
            "Ramp ID",
            Range(1,5)
        ) = 1

        _DayOrNight(
            "Day Or Night",
            Range(0,1)
        ) = 0


        // =========================================================
        // Real-time Shadow
        // =========================================================
        [Header(Scene Shadow)]

        // 屋檐、头发等投射到脸上的真实阴影
        _ReceiveShadowStrength(
            "Receive Shadow Strength",
            Range(0,1)
        ) = 0.55

        // 防止真实阴影把脸压成纯黑
        _FaceShadowFloor(
            "Face Shadow Floor",
            Range(0,1)
        ) = 0.45


        // =========================================================
        // Scene Lighting
        // =========================================================
        [Header(Scene Lighting)]

        _MainLightColorInfluence(
            "Main Light Color Influence",
            Range(0,1)
        ) = 0.25

        _IndirectLightStrength(
            "Indirect Light Strength",
            Range(0,1)
        ) = 0.16


        // =========================================================
        // Skin Specular
        // =========================================================
        [Header(Skin Specular)]

        [Toggle(_USE_SKIN_SPECULAR)]
        _UseSkinSpecular(
            "Use Skin Specular",
            Float
        ) = 1

        _SkinSpecColor(
            "Skin Specular Color",
            Color
        ) = (1.0,0.92,0.90,1)

        _SkinSpecIntensity(
            "Skin Specular Intensity",
            Range(0,1)
        ) = 0.06

        _SkinSpecExponentMin(
            "Rough Spec Exponent",
            Range(1,64)
        ) = 6

        _SkinSpecExponentMax(
            "Smooth Spec Exponent",
            Range(16,256)
        ) = 64

        _SkinSpecThreshold(
            "Skin Spec Threshold",
            Range(0,1)
        ) = 0.55

        _SkinSpecSoftness(
            "Skin Spec Softness",
            Range(0.001,0.5)
        ) = 0.15


        // =========================================================
        // Rim
        // =========================================================
        [Header(Rim)]

        [Toggle(_USE_RIM_LIGHT)]
        _UseRimLight(
            "Use Rim Light",
            Float
        ) = 0

        _RimColor(
            "Rim Color",
            Color
        ) = (0.8,0.72,0.9,1)

        _RimIntensity(
            "Rim Intensity",
            Range(0,1)
        ) = 0.025

        _RimPower(
            "Rim Power",
            Range(0.5,10)
        ) = 5
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

            float _MetallicScale;
            float _RoughnessScale;

            float4 _FaceForwardOS;

            float _FaceNormalFlatten;
            float _FaceFlattenStart;
            float _FaceLightBias;

            float _ShadowPosition;
            float _ShadowSoftness;

            float _RampID;
            float _DayOrNight;

            float _ReceiveShadowStrength;
            float _FaceShadowFloor;

            float _MainLightColorInfluence;
            float _IndirectLightStrength;

            float4 _SkinSpecColor;

            float _SkinSpecIntensity;

            float _SkinSpecExponentMin;
            float _SkinSpecExponentMax;

            float _SkinSpecThreshold;
            float _SkinSpecSoftness;

            float4 _RimColor;
            float _RimIntensity;
            float _RimPower;

        CBUFFER_END



        // =========================================================
        // Vertex Input
        // =========================================================

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;

            float2 uv : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };



        struct Varyings
        {
            float4 positionCS : SV_POSITION;

            float2 uv : TEXCOORD0;

            float3 positionWS : TEXCOORD1;
            float3 normalWS : TEXCOORD2;

            float4 shadowCoord : TEXCOORD3;

            half fogFactor : TEXCOORD4;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };



        // =========================================================
        // Vertex
        // =========================================================

        Varyings FaceVertex(Attributes input)
        {
            Varyings output;

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


            output.uv =
                TRANSFORM_TEX(
                    input.uv,
                    _BaseMap
                );


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
        // Fragment
        // =========================================================

        half4 FaceFragment(Varyings input) : SV_TARGET
        {
            UNITY_SETUP_INSTANCE_ID(input);


            // =====================================================
            // Texture
            // =====================================================

            half4 baseMap =
                SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    input.uv
                )
                * _BaseColor;


            half metallic =
                SAMPLE_TEXTURE2D(
                    _MetallicMap,
                    sampler_MetallicMap,
                    input.uv
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
                    input.uv
                ).r;


            roughness =
                saturate(
                    roughness
                    * _RoughnessScale
                );



            // =====================================================
            // Actual Normal
            // =====================================================

            half3 N =
                normalize(
                    input.normalWS
                );


            half3 V =
                SafeNormalize(
                    GetWorldSpaceViewDir(
                        input.positionWS
                    )
                );



            // =====================================================
            // Face Forward
            // =====================================================

            half3 faceForwardWS =
                normalize(
                    TransformObjectToWorldDir(
                        _FaceForwardOS.xyz
                    )
                );



            // =====================================================
            // Face Normal Flatten
            // =====================================================

            // -----------------------------------------------------
            // 判断当前像素是不是脸的正面区域
            //
            // 正脸：
            // dot(N, FaceForward) 接近 1
            //
            // 耳朵 / 脸侧：
            // 接近 0
            //
            // 后方：
            // 小于 0
            // -----------------------------------------------------

            half faceFrontness =
                dot(
                    N,
                    faceForwardWS
                );


            half flattenMask =
                smoothstep(
                    _FaceFlattenStart,
                    1.0h,
                    faceFrontness
                );


            half flattenAmount =
                _FaceNormalFlatten
                * flattenMask;



            // 真正用于 Diffuse 的 Face Normal
            //
            // 正面区域：
            // 往脸正前方向拉
            //
            // 侧面：
            // 仍然保持模型自己的法线

            half3 faceLightNormal =
                normalize(
                    lerp(
                        N,
                        faceForwardWS,
                        flattenAmount
                    )
                );



            // =====================================================
            // Main Light
            // =====================================================

            Light mainLight =
                GetMainLight(
                    input.shadowCoord
                );


            half3 L =
                normalize(
                    mainLight.direction
                );



            // =====================================================
            // Face Diffuse Lighting
            // =====================================================

            // 注意：
            //
            // 这里不用真正的 N
            // 而使用经过扁平化的 faceLightNormal

            half faceNdotL =
                dot(
                    faceLightNormal,
                    L
                );


            half faceHalfLambert =
                faceNdotL * 0.5h
                + 0.5h;


            // 人为将脸整体略微提亮
            faceHalfLambert =
                saturate(
                    faceHalfLambert
                    + _FaceLightBias
                );



            // =====================================================
            // Real-time Shadow
            // =====================================================

            half sceneShadow =
                lerp(
                    1.0h,
                    mainLight.shadowAttenuation,
                    _ReceiveShadowStrength
                );


            // 非常重要：
            //
            // 即使 ShadowMap 完全为黑，
            // 面部也不允许真正降到 0
            //
            // 防止屋檐 / 刘海把脸压成黑块

            sceneShadow =
                max(
                    sceneShadow,
                    _FaceShadowFloor
                );



            // =====================================================
            // Toon Light
            // =====================================================

            half toonLight =
                saturate(
                    faceHalfLambert
                    * sceneShadow
                );



            // =====================================================
            // Ramp
            // =====================================================

            half softness =
                max(
                    _ShadowSoftness,
                    0.001h
                );


            half rampU =
                smoothstep(
                    _ShadowPosition - softness,
                    _ShadowPosition + softness,
                    toonLight
                );



            half rampID =
                clamp(
                    floor(
                        _RampID + 0.5h
                    ),
                    1.0h,
                    5.0h
                );


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
                        0.75h,
                        1.0h,
                        rampU
                    );

                diffuseFactor =
                    simpleDiffuse.xxx;

            #endif



            // =====================================================
            // Main Light Tint
            // =====================================================

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
            // Indirect Light
            // =====================================================

            // 环境光仍然使用真实法线 N，
            // 不使用假法线。
            //
            // 这样脸侧仍然保留一些场景空间感。

            half3 indirectLight =
                max(
                    SampleSH(N),
                    half3(0,0,0)
                );


            finalColor +=
                baseMap.rgb
                * indirectLight
                * _IndirectLightStrength;



            // =====================================================
            // Skin Specular
            // =====================================================

            #if defined(_USE_SKIN_SPECULAR)

                half3 H =
                    SafeNormalize(
                        L + V
                    );


                // Specular 使用真实几何法线
                // 而不是被拉平的 Face Normal
                //
                // 否则整个脸会出现一整块均匀亮斑。

                half NdotH =
                    saturate(
                        dot(
                            N,
                            H
                        )
                    );


                half specExponent =
                    lerp(
                        _SkinSpecExponentMax,
                        _SkinSpecExponentMin,
                        roughness
                    );


                half rawSpec =
                    pow(
                        max(
                            NdotH,
                            0.0001h
                        ),
                        specExponent
                    );



                half skinSpec =
                    smoothstep(
                        _SkinSpecThreshold,
                        _SkinSpecThreshold
                        + _SkinSpecSoftness,
                        rawSpec
                    );



                // 粗糙皮肤进一步减弱高光
                half roughnessAttenuation =
                    lerp(
                        1.0h,
                        0.18h,
                        roughness
                    );



                // 高光主要出现在受光侧
                half specLightMask =
                    saturate(
                        faceNdotL * 3.0h
                    );


                specLightMask *=
                    sceneShadow;



                // Skin 不应该有 Metallic Specular
                //
                // 即使输入 Metallic Map 有一些 AI 生成噪点，
                // 这里也自动压制。

                half nonMetalMask =
                    1.0h - metallic;



                skinSpec *=
                    roughnessAttenuation
                    * specLightMask
                    * nonMetalMask
                    * _SkinSpecIntensity;



                finalColor +=
                    _SkinSpecColor.rgb
                    * skinSpec
                    * mainLightTint;

            #endif



            // =====================================================
            // Very Weak Rim
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
                        1.0h - NdotV,
                        _RimPower
                    );


                // 正脸不要出现一圈白边
                rim *=
                    saturate(
                        faceNdotL + 0.25h
                    );


                finalColor +=
                    _RimColor.rgb
                    * rim
                    * _RimIntensity;

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

            #pragma vertex FaceVertex
            #pragma fragment FaceFragment


            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog
            #pragma multi_compile_instancing


            #pragma shader_feature_local_fragment _USE_RAMP_SHADOW
            #pragma shader_feature_local_fragment _USE_SKIN_SPECULAR
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



            float4 GetFaceShadowPositionHClip(
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
                    GetFaceShadowPositionHClip(
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