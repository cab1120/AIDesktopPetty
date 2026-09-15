Shader "PetToon/Improved2/Face"
{
    Properties
    {
        [MainColor] _BaseColor("Base color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base color texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2

        [Header(Original Ramp Palette)]
        _RampTex("Original shadow Ramp - optional", 2D) = "white" {}
        [Toggle] _UseRampShadow("Use original Ramp palette", Float) = 0
        _RampID("Original Ramp row", Range(1,5)) = 1
        _DayOrNight("Day to night palette", Range(0,1)) = 0
        _ShadowPosition("Ramp shadow position", Range(0,1)) = 0.48
        _ShadowSoftness("Ramp shadow softness", Range(0.001,0.5)) = 0.12

        [Header(Fallback Cel Lighting)]
        _ShadowTint("Fallback shadow multiplier", Vector) = (0.78,0.73,0.76,1)
        _MidTint("Fallback middle multiplier", Vector) = (0.92,0.89,0.90,1)
        _ShadowThreshold("Fallback shadow threshold", Range(0,1)) = 0.23
        _LightThreshold("Fallback light threshold", Range(0,1)) = 0.62
        _BandSoftness("Band edge softness", Range(0.001,0.1)) = 0.012
        _Brightness("Base brightness", Range(0.5,1.5)) = 1
        _ReceiveShadowStrength("Receive realtime shadows", Range(0,1)) = 0.7
        _MainLightColorInfluence("Main light color influence", Range(0,1)) = 0.15
        _IndirectLightStrength("Ambient probe fill - no lightmap", Range(0,0.3)) = 0.025
        [Header(Outline)]
        _OutlineColor("Ink color", Color) = (0.12,0.13,0.18,1)
        _OutlineWidth("Outline width in pixels", Range(0,4)) = 0.55

        [Header(Optional Material Maps)]
        _MetallicMap("Metallic map - optional", 2D) = "black" {}
        _RoughnessMap("Roughness map - optional", 2D) = "white" {}
        _MetallicScale("Metallic scale", Range(0,1)) = 1
        _RoughnessScale("Roughness scale", Range(0,1)) = 1
        _SpecColor("Small cel highlight color", Color) = (1,1,1,1)
        _SpecIntensity("Small cel highlight intensity", Range(0,0.3)) = 0
        _SpecPower("Highlight sharpness", Range(1,128)) = 48

        [Header(Face Lighting Without SDF)]
        _FaceForwardOS("Face forward in object space", Vector) = (0,0,1,0)
        _FaceNormalFlatten("Face normal flatten", Range(0,1)) = 0.72
        _FaceFlattenStart("Face flatten start", Range(-1,0.99)) = 0.15
        _FaceLightBias("Face light bias", Range(-0.5,0.5)) = 0.08
        _FaceShadowFloor("Face shadow attenuation floor", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #define PET_FACE 1
        #include "CharacterToon.hlsl"
        ENDHLSL
        // URP extra outline pass, before the surface; no renderer feature required.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex PetOutlineVertex
            #pragma fragment PetOutlineFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull [_Cull] ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex PetVertex
            #pragma fragment PetFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex PetShadowVertex
            #pragma fragment PetDepthFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull] ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex PetVertex
            #pragma fragment PetDepthFragment
            #pragma multi_compile_instancing

            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull [_Cull] ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex PetVertex
            #pragma fragment PetNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
    FallBack Off
}
