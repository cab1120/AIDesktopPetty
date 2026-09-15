#ifndef PET_CHARACTER_TOON_INCLUDED
#define PET_CHARACTER_TOON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// Ramp is a lighting palette shared by textured parts and the solid-color tail.
// Tail still needs no surface texture or UV. No lightmap/SDF/AO map is required.
TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
#if !defined(PET_TAIL)
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_MetallicMap); SAMPLER(sampler_MetallicMap);
TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
#endif

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _ShadowTint, _MidTint, _OutlineColor;
    float _Cull, _ShadowThreshold, _LightThreshold, _BandSoftness;
    float _ReceiveShadowStrength, _MainLightColorInfluence, _IndirectLightStrength;
    float _OutlineWidth, _Brightness;
    float _UseRampShadow, _RampID, _DayOrNight, _ShadowPosition, _ShadowSoftness;
#if !defined(PET_TAIL)
    float4 _BaseMap_ST, _SpecColor;
    float _MetallicScale, _RoughnessScale, _SpecIntensity, _SpecPower;
#endif
#if defined(PET_BODY)
    float _UseUV1Backface, _UseVertexShadowBias, _ShadowRampWidth;
#endif
#if defined(PET_FACE)
    float4 _FaceForwardOS;
    float _FaceNormalFlatten, _FaceFlattenStart, _FaceLightBias, _FaceShadowFloor;
#endif
#if defined(PET_HAIR)
    float4 _HairHighlightColor;
    float _HairHighlightIntensity, _HairHighlightExponent, _HairAnisoShift;
#endif
CBUFFER_END

struct PetAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
#if !defined(PET_TAIL)
    float2 uv : TEXCOORD0;
#endif
#if defined(PET_BODY)
    float2 backUV : TEXCOORD1;
    float4 color : COLOR;
#endif
#if defined(PET_HAIR)
    float4 tangentOS : TANGENT;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PetVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    half fogFactor : TEXCOORD2;
#if !defined(PET_TAIL)
    float2 uv : TEXCOORD3;
#endif
#if defined(PET_BODY)
    float2 backUV : TEXCOORD4;
    half vertexBias : TEXCOORD5;
#endif
#if defined(PET_HAIR)
    float3 tangentWS : TEXCOORD4;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

PetVaryings PetVertex(PetAttributes input)
{
    PetVaryings output = (PetVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.fogFactor = ComputeFogFactor(position.positionCS.z);
#if !defined(PET_TAIL)
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
#endif
#if defined(PET_BODY)
    output.backUV = TRANSFORM_TEX(input.backUV, _BaseMap);
    output.vertexBias = (input.color.g * 2 - 1) * _ShadowRampWidth * _UseVertexShadowBias;
#endif
#if defined(PET_HAIR)
    output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
#endif
    return output;
}

// Per-fragment coordinates avoid interpolating across cascade boundaries.
float4 PetShadowCoord(float3 positionWS)
{
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    return ComputeScreenPos(TransformWorldToHClip(positionWS));
#else
    return TransformWorldToShadowCoord(positionWS);
#endif
}

half PetBandStep(half value, half edge)
{
    half width = max(max(_BandSoftness, fwidth(value) * 0.5h), 0.0005h);
    return smoothstep(edge - width, edge + width, value);
}

half3 PetBands(half lightValue)
{
    // Vector properties: numeric multipliers, with no inspector sRGB conversion.
    half shadowEdge = min(_ShadowThreshold, _LightThreshold);
    half lightEdge = max(_ShadowThreshold, _LightThreshold);
    half3 band = lerp(_ShadowTint.rgb, _MidTint.rgb, PetBandStep(lightValue, shadowEdge));
    return lerp(band, half3(1,1,1), PetBandStep(lightValue, lightEdge));
}

half3 PetRamp(half lightValue)
{
    // Exactly the original Improved layout: five night rows and five day rows.
    // Do not multiply this palette by the fallback shadow tint a second time.
    half softness = max(_ShadowSoftness, 0.001h);
    half u = smoothstep(_ShadowPosition - softness, _ShadowPosition + softness, lightValue);
    half row = clamp(floor(_RampID + 0.5h), 1.0h, 5.0h);
    half nightV = 0.45h - (row - 1.0h) * 0.1h;
    half3 day = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, half2(u, nightV + 0.5h)).rgb;
    half3 night = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, half2(u, nightV)).rgb;
    return lerp(day, night, saturate(_DayOrNight));
}

half4 PetFragment(PetVaryings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half3 normal = SafeNormalize(input.normalWS) * IS_FRONT_VFACE(facing, 1.0h, -1.0h);
    half3 lightingNormal = normal;
    half3 baseColor = _BaseColor.rgb;
#if !defined(PET_TAIL)
    float2 uv = input.uv;
#if defined(PET_BODY)
    uv = lerp(uv, input.backUV, IS_FRONT_VFACE(facing, 0.0h, 1.0h) * _UseUV1Backface);
#endif
    baseColor *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
#endif
#if defined(PET_FACE)
    // Preserve FaceV2's geometry-based flattening; no invented face SDF texture.
    half3 forward = SafeNormalize(TransformObjectToWorldDir(_FaceForwardOS.xyz));
    half flatten = smoothstep(min(_FaceFlattenStart, 0.99), 1, dot(normal, forward));
    lightingNormal = SafeNormalize(lerp(normal, forward, flatten * _FaceNormalFlatten));
#endif
    Light key = GetMainLight(PetShadowCoord(input.positionWS));
    half3 lightDir = SafeNormalize(key.direction);
    half receive = lerp(1.0h, key.shadowAttenuation, _ReceiveShadowStrength);
#if defined(PET_FACE)
    receive = max(receive, _FaceShadowFloor);
#endif
    half ndotl = dot(lightingNormal, lightDir);
    // Original ramp was authored for half-Lambert, not the station's raw N.L.
    half lightValue = _UseRampShadow > 0.5 ? ndotl * 0.5h + 0.5h : saturate(ndotl);
#if defined(PET_FACE)
    lightValue = saturate(lightValue + _FaceLightBias);
#endif
    lightValue *= receive;
#if defined(PET_BODY)
    lightValue = saturate(lightValue + input.vertexBias);
#endif
    half3 tint = lerp(half3(1,1,1), key.color, _MainLightColorInfluence);
    half3 diffuse = _UseRampShadow > 0.5 ? PetRamp(lightValue) : PetBands(lightValue);
    half3 color = baseColor * diffuse * tint * _Brightness;
    // Restore the original normal-dependent ambient fill in Ramp mode.
    half3 ambientNormal = _UseRampShadow > 0.5 ? normal : half3(0,1,0);
    color += baseColor * max(SampleSH(ambientNormal), 0) * _IndirectLightStrength;
#if !defined(PET_TAIL)
    half3 viewDir = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
    half3 halfway = SafeNormalize(lightDir + viewDir);
    half litMask = (_UseRampShadow > 0.5 ? saturate(ndotl * 4.0h) : PetBandStep(lightValue, _LightThreshold)) * receive;
    if (_SpecIntensity > 0)
    {
        half metallic = saturate(SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, uv).r * _MetallicScale);
        half roughness = saturate(SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r * _RoughnessScale);
        half spec = pow(saturate(dot(normal, halfway)), max(_SpecPower, 1));
        spec = PetBandStep(spec, 0.6h) * lerp(1.0h, 0.15h, roughness) * litMask;
        color += lerp(_SpecColor.rgb, baseColor, metallic) * spec * _SpecIntensity * tint;
    }
#endif
#if defined(PET_HAIR)
    if (_HairHighlightIntensity > 0 && dot(input.tangentWS, input.tangentWS) > 0.01)
    {
        half3 tangent = SafeNormalize(SafeNormalize(input.tangentWS) + normal * _HairAnisoShift);
        half th = dot(tangent, halfway);
        half highlight = pow(sqrt(saturate(1 - th * th)), max(_HairHighlightExponent, 1));
        color += _HairHighlightColor.rgb * PetBandStep(highlight, 0.6h) * _HairHighlightIntensity * litMask;
    }
#endif
    return half4(MixFog(color, input.fogFactor), 1);
}

PetVaryings PetOutlineVertex(PetAttributes input)
{
    PetVaryings output = PetVertex(input);
    // Inverted hull, width measured in render-target pixels (not model scale).
    float3 normalVS = TransformWorldToViewDir(output.normalWS);
    float2 projected = mul((float3x3)UNITY_MATRIX_P, normalVS).xy;
    float2 pixelDirection = projected * _ScaledScreenParams.xy;
    pixelDirection *= rsqrt(max(dot(pixelDirection, pixelDirection), 1e-8));
    output.positionCS.xy += pixelDirection * (2.0 * _OutlineWidth / _ScaledScreenParams.xy) * output.positionCS.w;
    return output;
}

half4 PetOutlineFragment(PetVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(_OutlineWidth - 0.001);
    return half4(MixFog(_OutlineColor.rgb, input.fogFactor), 1);
}

float3 _LightDirection, _LightPosition;
float4 PetShadowVertex(PetAttributes input) : SV_POSITION
{
    UNITY_SETUP_INSTANCE_ID(input);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 direction = normalize(_LightPosition - positionWS);
#else
    float3 direction = _LightDirection;
#endif
    float4 clipPosition = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, direction));
#if UNITY_REVERSED_Z
    clipPosition.z = min(clipPosition.z, clipPosition.w * UNITY_NEAR_CLIP_VALUE);
#else
    clipPosition.z = max(clipPosition.z, clipPosition.w * UNITY_NEAR_CLIP_VALUE);
#endif
    return clipPosition;
}
half4 PetDepthFragment() : SV_Target { return 0; }

half4 PetNormalsFragment(PetVaryings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float3 normal = SafeNormalize(input.normalWS) * IS_FRONT_VFACE(facing, 1.0h, -1.0h);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormal = PackNormalOctQuadEncode(normal);
    return half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0);
#else
    return half4(normal, 0);
#endif
}
#endif
