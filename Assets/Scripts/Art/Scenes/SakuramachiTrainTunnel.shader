Shader "Sakuramachi/Train Tunnel Toon URP"
{
    Properties
    {
        [HideInInspector] _TunnelClip ("Tunnel clip", Float) = 0
        [HideInInspector] _TunnelPlaneMin ("Tunnel minimum plane", Vector) = (1,0,0,1000)
        [HideInInspector] _TunnelPlaneMax ("Tunnel maximum plane", Vector) = (-1,0,0,1000)
        [MainColor] _BaseColor ("Base color", Color) = (1,1,1,1)
        _AuthoredColor ("Blender linear palette", Vector) = (1,1,1,1)
        _UseAuthoredColor ("Use source palette", Float) = 0
        _ReceiveShadows ("Receive realtime shadows", Range(0,1)) = 0
        _ShadowTint ("Shadow multiplier", Color) = (0.47,0.50,0.62,1)
        _MidTint ("Middle multiplier", Color) = (0.78,0.79,0.85,1)
        _ShadowThreshold ("Shadow threshold", Range(0,1)) = 0.23
        _LightThreshold ("Light threshold", Range(0,1)) = 0.62
        _Unlit ("Unlit ink or tunnel", Range(0,1)) = 0
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionStrength ("Emission strength", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _ShadowTint, _MidTint, _EmissionColor;
            float4 _AuthoredColor;
            float4 _TunnelPlaneMin, _TunnelPlaneMax;
            float _TunnelClip;
            float _UseAuthoredColor, _ReceiveShadows;
            float _ShadowThreshold, _LightThreshold, _Unlit, _EmissionStrength, _Cull;
        CBUFFER_END
        void ClipTunnel(float3 positionWS)
        {
            if (_TunnelClip > 0.5)
            {
                clip(dot(_TunnelPlaneMin, float4(positionWS, 1)));
                clip(dot(_TunnelPlaneMax, float4(positionWS, 1)));
            }
        }
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };
        Varyings Vertex(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull [_Cull] ZWrite On
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            half4 Fragment(Varyings input) : SV_Target
            {
                ClipTunnel(input.positionWS);
                Light key = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 direction = dot(key.direction, key.direction) > 0.01 ? key.direction : normalize(half3(-0.4,0.8,-0.3));
                half intensity = saturate(dot(normalize(input.normalWS), direction)) * lerp(1, key.shadowAttenuation, _ReceiveShadows);
                // Source ramp multipliers are linear numeric factors, not sRGB colors.
                half3 shadow = _UseAuthoredColor > 0.5 ? half3(0.47,0.50,0.62) : _ShadowTint.rgb;
                half3 middle = _UseAuthoredColor > 0.5 ? half3(0.78,0.79,0.85) : _MidTint.rgb;
                half3 band = intensity < _ShadowThreshold ? shadow : (intensity < _LightThreshold ? middle : half3(1,1,1));
                half3 base = _UseAuthoredColor > 0.5 ? _AuthoredColor.rgb : _BaseColor.rgb;
                half3 color = base * lerp(band, half3(1,1,1), _Unlit);
                color += _EmissionColor.rgb * _EmissionStrength;
                #if defined(UNITY_COLORSPACE_GAMMA)
                    if (_UseAuthoredColor > 0.5) color = LinearToSRGB(color);
                #endif
                return half4(color, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection, _LightPosition;
            Varyings ShadowVertex(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction = normalize(_LightPosition - positionWS);
                #else
                    float3 direction = _LightDirection;
                #endif
                float4 clip = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, direction));
                #if UNITY_REVERSED_Z
                    clip.z = min(clip.z, clip.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    clip.z = max(clip.z, clip.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                Varyings output;
                output.positionCS = clip;
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                return output;
            }
            half4 ShadowFragment(Varyings input) : SV_Target { ClipTunnel(input.positionWS); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull] ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment DepthFragment
            half4 DepthFragment(Varyings input) : SV_Target { ClipTunnel(input.positionWS); return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}

