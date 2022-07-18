#ifndef UNIVERSAL_META_INPUT_INCLUDED
#define UNIVERSAL_META_INPUT_INCLUDED

#include "Assets/URP/com.unity.render-pipelines.core@12.1.6/ShaderLibrary/MetaPass.hlsl"
#include "Assets/URP/com.unity.render-pipelines.universal@12.1.6/ShaderLibrary/Lighting.hlsl"
#include "Assets/URP/com.unity.render-pipelines.core@12.1.6/ShaderLibrary/Color.hlsl"

#define MetaInput UnityMetaInput
#define MetaFragment UnityMetaFragment
float4 MetaVertexPosition(float4 positionOS, float2 uv1, float2 uv2, float4 uv1ST, float4 uv2ST)
{
    return UnityMetaVertexPosition(positionOS.xyz, uv1, uv2, uv1ST, uv2ST);
}
#endif
