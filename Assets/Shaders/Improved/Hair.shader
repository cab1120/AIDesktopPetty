Shader "GenshinToon/HairV2"
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
        // Material Maps
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

        _ShadowPosition(
            "Shadow Position",
            Range(0,1)
        ) = 0.55

        _ShadowSoftness(
            "Shadow Softness",
            Range(0.001,0.5)
        ) = 0.08

        _ReceiveShadowStrength(
            "Receive Shadow Strength",
            Range(0,1)
        ) = 0.7

        _RampID(
            "Ramp ID",
            Range(1,5)
        ) = 1

        _DayOrNight(
            "Day Or Night",
            Range(0,1)
        ) = 0


        // =========================================================
        // Scene Light
        // =========================================================
        [Header(Scene Lighting)]

        _MainLightColorInfluence(
            "Main Light Color Influence",
            Range(0,1)
        ) = 0.30

        _IndirectLightStrength(
            "Indirect Light Strength",
            Range(0,1)
        ) = 0.10


        // =========================================================
        // Normal Specular
        // =========================================================
        [Header(Base Specular)]

        _SpecColor(
            "Base Specular Color",
            Color
        ) = (1,1,1,1)

        _SpecIntensity(
            "Base Specular Intensity",
            Range(0,2)
        ) = 0.08

        _SpecExponentMin(
            "Rough Spec Exponent",
            Range(1,64)
        ) = 8

        _SpecExponentMax(
            "Smooth Spec Exponent",
            Range(16,256)
        ) = 96

        _SpecThreshold(
            "Spec Threshold",
            Range(0,1)
        ) = 0.55

        _SpecSoftness(
            "Spec Softness",
            Range(0.001,0.5)
        ) = 0.08


        // =========================================================
        // Hair Anisotropic Highlight
        // =========================================================
        [Header(Hair Highlight)]

        [Toggle(_USE_HAIR_HIGHLIGHT)]
        _UseHairHighlight(
            "Use Hair Highlight",
            Float
        ) = 1

        _HairHighlightColor(
            "Hair Highlight Color",
            Color
        ) = (1.0,0.82,0.95,1)

        _HairHighlightIntensity(
            "Hair Highlight Intensity",
            Range(0,2)
        ) = 0.35

        _HairHighlightExponent(
            "Hair Highlight Sharpness",
            Range(1,256)
        ) = 64

        _HairHighlightThreshold(
            "Hair Highlight Threshold",
            Range(0,1)
        ) = 0.45

        _HairHighlightSoftness(
            "Hair Highlight Softness",
            Range(0.001,0.5)
        ) = 0.08

        _HairAnisoShift(
            "Anisotropic Shift",
            Range(-1,1)
        ) = 0.10

        _HairHighlightRoughnessInfluence(
            "Roughness Influence",
            Range(0,1)
        ) = 0.75


        // =========================================================
        // Secondary Hair Highlight
        // =========================================================
        [Header(Secondary Highlight)]

        [Toggle(_USE_SECOND_HIGHLIGHT)]
        _UseSecondHighlight(
            "Use Second Highlight",
            Float
        ) = 1

        _SecondHighlightColor(
            "Second Highlight Color",
            Color
        ) = (0.8,0.65,1.0,1)

        _SecondHighlightIntensity(
            "Second Highlight Intensity",
            Range(0,1)
        ) = 0.10

        _SecondHighlightShift(
            "Second Highlight Shift",
            Range(-1,1)
        ) = -0.25

        _SecondHighlightExponent(
            "Second Highlight Sharpness",
            Range(1,256)
        ) = 32


        // =========================================================
        // Rim
        // =========================================================
        [Header(Rim)]

        [Toggle(_USE_RIM_LIGHT)]
        _UseRimLight(
            "Use Rim Light",
            Float
        ) = 1

        _RimColor(
            "Rim Color",
            Color
        ) = (0.75,0.70,0.95,1)

        _RimIntensity(
            "Rim Intensity",
            Range(0,1)
        ) = 0.05

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

            float _ShadowPosition;
            float _ShadowSoftness;

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

            float4 _HairHighlightColor;
            float _HairHighlightIntensity;

            float _HairHighlightExponent;
            float _HairHighlightThreshold;
            float _HairHighlightSoftness;

            float _HairAnisoShift;

            float _HairHighlightRoughnessInfluence;

            float4 _SecondHighlightColor;
            float _SecondHighlightIntensity;

            float _SecondHighlightShift;
            float _SecondHighlightExponent;

            float4 _RimColor;
            float _RimIntensity;
            float _RimPower;

        CBUFFER_END



        // =========================================================
        // Vertex
        // =========================================================

        struct Attributes
        {
            float4 positionOS : POSITION;

            float3 normalOS : NORMAL;

            // 头发方向性高光依赖 Tangent
            float4 tangentOS : TANGENT;

            float2 uv : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        struct Varyings
        {
            float4 positionCS : SV_POSITION;

            float2 uv : TEXCOORD0;

            float3 positionWS : TEXCOORD1;

            float3 normalWS : TEXCOORD2;

            float3 tangentWS : TEXCOORD3;

            float3 bitangentWS : TEXCOORD4;

            float4 shadowCoord : TEXCOORD5;

            half fogFactor : TEXCOORD6;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };



        Varyings HairVertex(Attributes input)
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
                    input.normalOS,
                    input.tangentOS
                );


            output.positionCS =
                positionInput.positionCS;

            output.positionWS =
                positionInput.positionWS;


            output.normalWS =
                normalInput.normalWS;

            output.tangentWS =
                normalInput.tangentWS;

            output.bitangentWS =
                normalInput.bitangentWS;


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

        half4 HairFragment(Varyings input) : SV_TARGET
        {
            UNITY_SETUP_INSTANCE_ID(input);


            // -----------------------------------------------------
            // Texture
            // -----------------------------------------------------

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



            // -----------------------------------------------------
            // Directions
            // -----------------------------------------------------

            half3 N =
                normalize(
                    input.normalWS
                );


            half3 T =
                normalize(
                    input.tangentWS
                );


            half3 B =
                normalize(
                    input.bitangentWS
                );


            half3 V =
                SafeNormalize(
                    GetWorldSpaceViewDir(
                        input.positionWS
                    )
                );



            // -----------------------------------------------------
            // Main Light
            // -----------------------------------------------------

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
                NdotL * 0.5h + 0.5h;


            half shadowAttenuation =
                lerp(
                    1.0h,
                    mainLight.shadowAttenuation,
                    _ReceiveShadowStrength
                );



            // -----------------------------------------------------
            // Toon Diffuse
            // -----------------------------------------------------

            half toonLight =
                saturate(
                    halfLambert
                    * shadowAttenuation
                );


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
                    floor(_RampID + 0.5h),
                    1.0h,
                    5.0h
                );


            half rampNightV =
                0.45h
                - (rampID - 1.0h)
                * 0.1h;


            half rampDayV =
                rampNightV + 0.5h;


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
                    simpleDiffuse.xxx;

            #endif



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



            // -----------------------------------------------------
            // Environment Fill
            // -----------------------------------------------------

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
            // Basic Toon Specular
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


            half specExponent =
                lerp(
                    _SpecExponentMax,
                    _SpecExponentMin,
                    roughness
                );


            half basicSpec =
                pow(
                    max(
                        NdotH,
                        0.0001h
                    ),
                    specExponent
                );


            basicSpec =
                smoothstep(
                    _SpecThreshold,
                    _SpecThreshold + _SpecSoftness,
                    basicSpec
                );


            half roughSpec =
                lerp(
                    1.0h,
                    0.15h,
                    roughness
                );


            half lightMask =
                saturate(
                    NdotL * 4.0h
                )
                * shadowAttenuation;


            // Hair 基本不应该体现强 Metallic
            // 但如果贴图中有发饰等区域，仍然保留一点作用
            half3 basicSpecColor =
                lerp(
                    _SpecColor.rgb,
                    baseMap.rgb,
                    metallic
                );


            finalColor +=
                basicSpecColor
                * basicSpec
                * roughSpec
                * lightMask
                * _SpecIntensity;



            // =====================================================
            // Main Hair Anisotropic Highlight
            // =====================================================

            #if defined(_USE_HAIR_HIGHLIGHT)

                // -------------------------------------------------
                // 核心思路：
                //
                // 不直接使用 N·H，
                // 而利用 Tangent 与 H 的关系产生沿发丝方向的高光。
                //
                // Kajiya-Kay 风格简化版。
                // -------------------------------------------------

                half3 shiftedTangent =
                    normalize(
                        T
                        + N * _HairAnisoShift
                    );


                half TdotH =
                    dot(
                        shiftedTangent,
                        H
                    );


                // sin(theta)
                // tangent 与 H 越垂直时越亮
                half sinTH =
                    sqrt(
                        saturate(
                            1.0h
                            - TdotH * TdotH
                        )
                    );


                half hairSpec =
                    pow(
                        sinTH,
                        _HairHighlightExponent
                    );


                // 将连续高光卡通化
                hairSpec =
                    smoothstep(
                        _HairHighlightThreshold,
                        _HairHighlightThreshold
                        + _HairHighlightSoftness,
                        hairSpec
                    );


                // Roughness 控制 Hair Highlight
                half hairRoughnessMask =
                    lerp(
                        1.0h,
                        1.0h - roughness,
                        _HairHighlightRoughnessInfluence
                    );


                // 阴影中大幅降低高光
                hairSpec *=
                    lightMask;


                hairSpec *=
                    hairRoughnessMask;


                finalColor +=
                    _HairHighlightColor.rgb
                    * hairSpec
                    * _HairHighlightIntensity
                    * mainLightTint;

            #endif



            // =====================================================
            // Secondary Highlight
            // =====================================================

            #if defined(_USE_SECOND_HIGHLIGHT)

                // 第二层高光有不同的 Shift，
                // 用来避免头发只出现一条完全均匀的高光线。

                half3 secondTangent =
                    normalize(
                        T
                        + N * _SecondHighlightShift
                    );


                half secondDot =
                    dot(
                        secondTangent,
                        H
                    );


                half secondSin =
                    sqrt(
                        saturate(
                            1.0h
                            - secondDot * secondDot
                        )
                    );


                half secondSpec =
                    pow(
                        secondSin,
                        _SecondHighlightExponent
                    );


                secondSpec =
                    smoothstep(
                        0.55h,
                        0.70h,
                        secondSpec
                    );


                secondSpec *=
                    lightMask;


                secondSpec *=
                    1.0h - roughness * 0.7h;


                finalColor +=
                    _SecondHighlightColor.rgb
                    * secondSpec
                    * _SecondHighlightIntensity;

            #endif



            // =====================================================
            // Rim
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


                rim *=
                    saturate(
                        NdotL + 0.35h
                    );


                finalColor +=
                    _RimColor.rgb
                    * rim
                    * _RimIntensity;

            #endif



            // -----------------------------------------------------
            // Fog
            // -----------------------------------------------------

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

            #pragma vertex HairVertex
            #pragma fragment HairFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment _USE_RAMP_SHADOW
            #pragma shader_feature_local_fragment _USE_HAIR_HIGHLIGHT
            #pragma shader_feature_local_fragment _USE_SECOND_HIGHLIGHT
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



            float4 GetHairShadowPositionHClip(
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
                    GetHairShadowPositionHClip(
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