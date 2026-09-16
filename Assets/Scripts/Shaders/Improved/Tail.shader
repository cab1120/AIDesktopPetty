Shader "GenshinToon/TailFurV2"
{
    Properties
    {
        // =========================================================
        // Base
        // =========================================================
        [Header(Base)]

        _BaseColor(
            "Base Color",
            Color
        ) = (0.8, 0.494, 0.161, 1)


        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull(
            "Cull",
            Float
        ) = 2


        // =========================================================
        // Toon Shadow
        // =========================================================
        [Header(Toon Shadow)]

        _RampTex(
            "Ramp Texture",
            2D
        ) = "white" {}

        [Toggle(_USE_RAMP_SHADOW)]
        _UseRampShadow(
            "Use Ramp Shadow",
            Float
        ) = 1

        _ShadowPosition(
            "Shadow Position",
            Range(0,1)
        ) = 0.52

        _ShadowSoftness(
            "Shadow Softness",
            Range(0.001,0.5)
        ) = 0.14

        _ReceiveShadowStrength(
            "Receive Shadow Strength",
            Range(0,1)
        ) = 0.65

        _RampID(
            "Ramp ID",
            Range(1,5)
        ) = 1

        _DayOrNight(
            "Day Or Night",
            Range(0,1)
        ) = 0


        // =========================================================
        // Scene Lighting
        // =========================================================
        [Header(Scene Lighting)]

        _MainLightColorInfluence(
            "Main Light Color Influence",
            Range(0,1)
        ) = 0.30

        _IndirectLightStrength(
            "Indirect Light Strength",
            Range(0,1)
        ) = 0.12


        // =========================================================
        // Fur Fuzz
        // =========================================================
        [Header(Fur Fuzz)]

        [Toggle(_USE_FUR_FUZZ)]
        _UseFurFuzz(
            "Use Fur Fuzz",
            Float
        ) = 1

        _FuzzColor(
            "Fuzz Color",
            Color
        ) = (1.0, 0.68, 0.32, 1)

        _FuzzIntensity(
            "Fuzz Intensity",
            Range(0,1)
        ) = 0.18

        _FuzzPower(
            "Fuzz Power",
            Range(0.5,8)
        ) = 2.5

        // 控制 Fuzz 对受光方向的依赖
        _FuzzLightWrap(
            "Fuzz Light Wrap",
            Range(0,1)
        ) = 0.45


        // =========================================================
        // Geometry Detail
        // =========================================================
        [Header(Geometry Fur Detail)]

        [Toggle(_USE_GEOMETRY_DETAIL)]
        _UseGeometryDetail(
            "Use Geometry Detail",
            Float
        ) = 1

        // 利用法线变化增强毛茸茸模型表面的小结构
        _GeometryDetailStrength(
            "Geometry Detail Strength",
            Range(0,1)
        ) = 0.18

        _GeometryDetailScale(
            "Geometry Detail Scale",
            Range(0.1,20)
        ) = 5

        _GeometryDetailColor(
            "Geometry Detail Color",
            Color
        ) = (1.0,0.72,0.38,1)


        // =========================================================
        // Soft Specular
        // =========================================================
        [Header(Soft Fur Specular)]

        [Toggle(_USE_FUR_SPECULAR)]
        _UseFurSpecular(
            "Use Fur Specular",
            Float
        ) = 1

        _SpecColor(
            "Specular Color",
            Color
        ) = (1.0,0.76,0.48,1)

        _SpecIntensity(
            "Specular Intensity",
            Range(0,1)
        ) = 0.06

        _SpecPower(
            "Specular Power",
            Range(1,128)
        ) = 24

        _SpecThreshold(
            "Specular Threshold",
            Range(0,1)
        ) = 0.55

        _SpecSoftness(
            "Specular Softness",
            Range(0.001,0.5)
        ) = 0.18


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
        ) = (1.0,0.62,0.30,1)

        _RimIntensity(
            "Rim Intensity",
            Range(0,1)
        ) = 0.035

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


        TEXTURE2D(_RampTex);
        SAMPLER(sampler_RampTex);



        CBUFFER_START(UnityPerMaterial)

            float4 _BaseColor;

            float _Cull;

            float _ShadowPosition;
            float _ShadowSoftness;

            float _ReceiveShadowStrength;

            float _RampID;
            float _DayOrNight;

            float _MainLightColorInfluence;
            float _IndirectLightStrength;

            float4 _FuzzColor;
            float _FuzzIntensity;
            float _FuzzPower;
            float _FuzzLightWrap;

            float _GeometryDetailStrength;
            float _GeometryDetailScale;
            float4 _GeometryDetailColor;

            float4 _SpecColor;
            float _SpecIntensity;
            float _SpecPower;
            float _SpecThreshold;
            float _SpecSoftness;

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

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        struct Varyings
        {
            float4 positionCS : SV_POSITION;

            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;

            float4 shadowCoord : TEXCOORD2;

            half fogFactor : TEXCOORD3;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };



        // =========================================================
        // Vertex
        // =========================================================

        Varyings TailVertex(
            Attributes input
        )
        {
            Varyings output;

            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(
                input,
                output
            );


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

        half4 TailFragment(
            Varyings input
        ) : SV_TARGET
        {
            UNITY_SETUP_INSTANCE_ID(input);


            // =====================================================
            // Directions
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
            
            // Fuzz 和 Rim 共用
            half NdotV =
                saturate(
                    dot(
                        N,
                        V
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


            half NdotL =
                dot(
                    N,
                    L
                );


            // Fur 使用稍微更柔和的 Half Lambert
            half halfLambert =
                NdotL * 0.5h
                + 0.5h;



            // =====================================================
            // Real-time Shadow
            // =====================================================

            half shadowAttenuation =
                lerp(
                    1.0h,
                    mainLight.shadowAttenuation,
                    _ReceiveShadowStrength
                );



            half toonLight =
                saturate(
                    halfLambert
                    * shadowAttenuation
                );



            // =====================================================
            // Toon Ramp
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

                // 即使关闭 Ramp，
                // 也保持比较柔和的 Fur Lighting

                half simpleDiffuse =
                    lerp(
                        0.62h,
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
                _BaseColor.rgb
                * diffuseFactor
                * mainLightTint;



            // =====================================================
            // Indirect Lighting
            // =====================================================

            half3 indirectLight =
                max(
                    SampleSH(N),
                    half3(0,0,0)
                );


            finalColor +=
                _BaseColor.rgb
                * indirectLight
                * _IndirectLightStrength;



            // =====================================================
            // Fur Fuzz / Velvet Lobe
            // =====================================================

            #if defined(_USE_FUR_FUZZ)

                // -------------------------------------------------
                // 毛绒最重要的一个视觉特征：
                //
                // 掠射视角时，
                // 边缘会产生柔和的亮层。
                //
                // 它不是传统 Rim，
                // 而更接近 Velvet / Fuzz。
                // -------------------------------------------------
            


                half fuzz =
                    pow(
                        1.0h - NdotV,
                        _FuzzPower
                    );


                // -------------------------------------------------
                // 毛绒不能完全无视光源。
                //
                // 这里使用 Wrapped Lighting，
                // 让侧光和略微背光时仍然能看到毛绒。
                // -------------------------------------------------

                half wrappedLight =
                    saturate(
                        (
                            NdotL
                            + _FuzzLightWrap
                        )
                        /
                        (
                            1.0h
                            + _FuzzLightWrap
                        )
                    );


                fuzz *=
                    wrappedLight;


                // 阴影中仍然保留一点，
                // 但明显减弱。
                fuzz *=
                    lerp(
                        0.35h,
                        1.0h,
                        shadowAttenuation
                    );


                finalColor +=
                    _FuzzColor.rgb
                    * fuzz
                    * _FuzzIntensity
                    * mainLightTint;

            #endif



            // =====================================================
            // Geometry Fur Detail
            // =====================================================

            #if defined(_USE_GEOMETRY_DETAIL)

                // -------------------------------------------------
                // 这是这套 Tail Shader 最特别的部分。
                //
                // 因为没有任何 Fur Texture，
                // 我们直接观察屏幕空间里法线变化的速度。
                //
                // 毛茸茸模型的细小凸起：
                //
                // 法线变化快
                //        ↓
                // detail 值变大
                //        ↓
                // 稍微提亮
                //
                // 从而让模型本身的毛发几何更容易看出来。
                // -------------------------------------------------

                float3 normalDx =
                    ddx(N);


                float3 normalDy =
                    ddy(N);


                half normalVariation =
                    length(normalDx)
                    + length(normalDy);


                half geometryDetail =
                    saturate(
                        normalVariation
                        * _GeometryDetailScale
                    );


                // 防止平滑面也出现明显发亮
                geometryDetail =
                    geometryDetail
                    * geometryDetail;


                // 阴影中略微压低
                geometryDetail *=
                    lerp(
                        0.55h,
                        1.0h,
                        toonLight
                    );


                finalColor +=
                    _GeometryDetailColor.rgb
                    * geometryDetail
                    * _GeometryDetailStrength;

            #endif



            // =====================================================
            // Soft Fur Specular
            // =====================================================

            #if defined(_USE_FUR_SPECULAR)

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


                half rawSpec =
                    pow(
                        max(
                            NdotH,
                            0.0001h
                        ),
                        _SpecPower
                    );


                half spec =
                    smoothstep(
                        _SpecThreshold,
                        _SpecThreshold
                        + _SpecSoftness,
                        rawSpec
                    );


                // 毛发高光不能像塑料
                // 所以整体非常弱
                spec *=
                    saturate(
                        NdotL * 3.0h
                    );


                spec *=
                    shadowAttenuation;


                finalColor +=
                    _SpecColor.rgb
                    * spec
                    * _SpecIntensity
                    * mainLightTint;

            #endif



            // =====================================================
            // Weak Rim
            // =====================================================

            #if defined(_USE_RIM_LIGHT)
            
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

            #pragma vertex TailVertex
            #pragma fragment TailFragment


            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog
            #pragma multi_compile_instancing


            #pragma shader_feature_local_fragment _USE_RAMP_SHADOW
            #pragma shader_feature_local_fragment _USE_FUR_FUZZ
            #pragma shader_feature_local_fragment _USE_GEOMETRY_DETAIL
            #pragma shader_feature_local_fragment _USE_FUR_SPECULAR
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



            float4 GetTailShadowPositionHClip(
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
                    GetTailShadowPositionHClip(
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