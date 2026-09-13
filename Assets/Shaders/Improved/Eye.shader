Shader "GenshinToon/EyeV2"
{
    Properties
    {
        // =========================================================
        // Base
        // =========================================================
        [Header(Base)]

        _BaseMap("Base Map", 2D) = "white" {}

        _BaseColor(
            "Base Color",
            Color
        ) = (1,1,1,1)

        // 整体透明度
        _Alpha(
            "Alpha",
            Range(0,1)
        ) = 1

        // 眼睛通常应该保持比较亮
        _EyeBrightness(
            "Eye Brightness",
            Range(0.5,2)
        ) = 1.05


        // =========================================================
        // Lighting
        // =========================================================
        [Header(Lighting)]

        // 场景主光源影响程度
        //
        // 眼睛不应该像皮肤一样被强烈打暗
        _MainLightInfluence(
            "Main Light Influence",
            Range(0,1)
        ) = 0.20

        // 阴影影响程度
        _ReceiveShadowStrength(
            "Receive Shadow Strength",
            Range(0,1)
        ) = 0.25

        // 即使完全进入阴影，
        // 眼睛也不会低于这个亮度
        _ShadowFloor(
            "Shadow Floor",
            Range(0,1)
        ) = 0.70

        // 环境光影响
        _IndirectLightStrength(
            "Indirect Light Strength",
            Range(0,1)
        ) = 0.08


        // =========================================================
        // Procedural Highlight
        // =========================================================
        [Header(Eye Highlight)]

        [Toggle(_USE_EYE_HIGHLIGHT)]
        _UseEyeHighlight(
            "Use Eye Highlight",
            Float
        ) = 0

        _HighlightColor(
            "Highlight Color",
            Color
        ) = (1,1,1,1)

        _HighlightIntensity(
            "Highlight Intensity",
            Range(0,2)
        ) = 0.15

        _HighlightPower(
            "Highlight Power",
            Range(4,256)
        ) = 64

        _HighlightThreshold(
            "Highlight Threshold",
            Range(0,1)
        ) = 0.6

        _HighlightSoftness(
            "Highlight Softness",
            Range(0.001,0.5)
        ) = 0.08


        // =========================================================
        // Alpha
        // =========================================================
        [Header(Alpha)]

        // 用于修正 PNG 边缘
        _AlphaPower(
            "Alpha Power",
            Range(0.2,3)
        ) = 1

        // 如果透明边缘过于发白，
        // 可以略微提高
        _AlphaCutoff(
            "Soft Alpha Cutoff",
            Range(0,0.5)
        ) = 0


        // =========================================================
        // Render
        // =========================================================
        [Header(Render)]

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull(
            "Cull",
            Float
        ) = 2
    }



    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"

            "RenderType" = "Transparent"

            "Queue" = "Transparent"

            "IgnoreProjector" = "True"
        }


        // =========================================================
        // Transparent Rendering
        // =========================================================

        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off

        ZTest LEqual

        Cull [_Cull]



        Pass
        {
            Name "UniversalForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            HLSLPROGRAM

            #pragma vertex EyeVertex
            #pragma fragment EyeFragment


            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment _USE_EYE_HIGHLIGHT



            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"



            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);



            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _Alpha;

                float _EyeBrightness;

                float _MainLightInfluence;

                float _ReceiveShadowStrength;

                float _ShadowFloor;

                float _IndirectLightStrength;

                float4 _HighlightColor;

                float _HighlightIntensity;

                float _HighlightPower;

                float _HighlightThreshold;

                float _HighlightSoftness;

                float _AlphaPower;

                float _AlphaCutoff;

                float _Cull;

            CBUFFER_END



            // =====================================================
            // Attributes
            // =====================================================

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



            // =====================================================
            // Vertex
            // =====================================================

            Varyings EyeVertex(
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



            // =====================================================
            // Fragment
            // =====================================================

            half4 EyeFragment(
                Varyings input
            ) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);



                // =================================================
                // BaseMap
                // =================================================

                half4 baseMap =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                baseMap *=
                    _BaseColor;



                // =================================================
                // Alpha
                // =================================================

                // PNG 自身 Alpha
                half alpha =
                    saturate(
                        baseMap.a
                        * _Alpha
                    );


                // 调整透明度曲线
                alpha =
                    pow(
                        alpha,
                        _AlphaPower
                    );


                // 柔和去除最透明区域
                //
                // 不是 Alpha Clip，
                // 所以仍然保持 Transparent。
                alpha =
                    saturate(
                        (
                            alpha
                            - _AlphaCutoff
                        )
                        /
                        max(
                            1.0h
                            - _AlphaCutoff,
                            0.001h
                        )
                    );



                // =================================================
                // Directions
                // =================================================

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



                // =================================================
                // Main Light
                // =================================================

                Light mainLight =
                    GetMainLight(
                        input.shadowCoord
                    );


                half3 L =
                    normalize(
                        mainLight.direction
                    );



                half NdotL =
                    saturate(
                        dot(
                            N,
                            L
                        )
                    );



                // =================================================
                // Scene Shadow
                // =================================================

                half shadow =
                    lerp(
                        1.0h,
                        mainLight.shadowAttenuation,
                        _ReceiveShadowStrength
                    );


                // 眼睛不允许变得太暗
                shadow =
                    max(
                        shadow,
                        _ShadowFloor
                    );



                // =================================================
                // Gentle Eye Lighting
                // =================================================

                // 我们不直接使用 NdotL，
                // 否则球形眼球会出现非常明显的明暗球体感。
                //
                // 这里把光照压缩到很小的范围。

                half diffuseLight =
                    lerp(
                        0.85h,
                        1.0h,
                        NdotL
                    );


                diffuseLight *=
                    shadow;



                // =================================================
                // Main Light Tint
                // =================================================

                half3 lightTint =
                    lerp(
                        half3(1,1,1),
                        mainLight.color,
                        _MainLightInfluence
                    );



                // =================================================
                // Base Color
                // =================================================

                half3 finalColor =
                    baseMap.rgb
                    * diffuseLight
                    * lightTint
                    * _EyeBrightness;



                // =================================================
                // Indirect Lighting
                // =================================================

                half3 indirect =
                    max(
                        SampleSH(N),
                        half3(0,0,0)
                    );


                finalColor +=
                    baseMap.rgb
                    * indirect
                    * _IndirectLightStrength;



                // =================================================
                // Optional Procedural Eye Highlight
                // =================================================

                #if defined(_USE_EYE_HIGHLIGHT)

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


                    half rawHighlight =
                        pow(
                            max(
                                NdotH,
                                0.0001h
                            ),
                            _HighlightPower
                        );


                    // Toon 化
                    half highlight =
                        smoothstep(
                            _HighlightThreshold,
                            _HighlightThreshold
                            + _HighlightSoftness,
                            rawHighlight
                        );


                    // 避免在完全背光侧产生明显亮点
                    highlight *=
                        saturate(
                            NdotL * 4.0h
                        );


                    highlight *=
                        shadow;


                    finalColor +=
                        _HighlightColor.rgb
                        * highlight
                        * _HighlightIntensity;

                #endif



                // =================================================
                // Fog
                // =================================================

                finalColor =
                    MixFog(
                        finalColor,
                        input.fogFactor
                    );



                return half4(
                    finalColor,
                    alpha
                );
            }


            ENDHLSL
        }
    }
}