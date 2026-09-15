Shader "PetToon/Improved2/Eye"
{
    Properties
    {
        [Header(Base)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor(
            "Base Color",
            Color
        ) = (1,1,1,1)
        _Alpha(
            "Alpha",
            Range(0,1)
        ) = 1
        _EyeBrightness(
            "Eye Brightness",
            Range(0.5,2)
        ) = 1
        [Header(Lighting)]
        _MainLightInfluence(
            "Main Light Influence",
            Range(0,1)
        ) = 0.15
        _ReceiveShadowStrength(
            "Receive Shadow Strength",
            Range(0,1)
        ) = 0.18
        _ShadowFloor(
            "Shadow Floor",
            Range(0,1)
        ) = 0.8
        _IndirectLightStrength(
            "Indirect Light Strength",
            Range(0,1)
        ) = 0.02
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
        [Header(Alpha)]
        _AlphaPower(
            "Alpha Power",
            Range(0.2,3)
        ) = 1
        _AlphaCutoff(
            "Soft Alpha Cutoff",
            Range(0,0.5)
        ) = 0
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
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _USE_EYE_HIGHLIGHT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define _SURFACE_TYPE_TRANSPARENT 1
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
            half4 EyeFragment(
                Varyings input
            ) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 baseMap =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );
                baseMap *=
                    _BaseColor;
                half alpha =
                    saturate(
                        baseMap.a
                        * _Alpha
                    );
                alpha =
                    pow(
                        alpha,
                        _AlphaPower
                    );
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
                Light mainLight =
                    GetMainLight(
                        TransformWorldToShadowCoord(input.positionWS)
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
                half shadow =
                    lerp(
                        1.0h,
                        mainLight.shadowAttenuation,
                        _ReceiveShadowStrength
                    );
                shadow =
                    max(
                        shadow,
                        _ShadowFloor
                    );
                half diffuseLight =
                    lerp(0.92h, 1.0h, smoothstep(0.35h, 0.37h, NdotL));
                diffuseLight *=
                    shadow;
                half3 lightTint =
                    lerp(
                        half3(1,1,1),
                        mainLight.color,
                        _MainLightInfluence
                    );
                half3 finalColor =
                    baseMap.rgb
                    * diffuseLight
                    * lightTint
                    * _EyeBrightness;
                half3 indirect =
                    max(
                        SampleSH(half3(0,1,0)),
                        half3(0,0,0)
                    );
                finalColor +=
                    baseMap.rgb
                    * indirect
                    * _IndirectLightStrength;
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
                    half highlight =
                        smoothstep(
                            _HighlightThreshold,
                            _HighlightThreshold
                            + _HighlightSoftness,
                            rawHighlight
                        );
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
