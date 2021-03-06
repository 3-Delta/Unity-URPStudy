#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#include "Assets/URP/com.unity.shadergraph@12.1.6/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

float SampleSceneDepth(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
}

float LoadSceneDepth(uint2 uv)
{
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, uv).r;
}
#endif
