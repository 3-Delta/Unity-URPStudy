Shader "Hidden/HDRP/preIntegratedFGD_GGXDisneyDiffuse"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM

            #pragma editor_sync_compilation

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            #define PREFER_HALF 0
            #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Common.hlsl"
            #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.cs.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // We want the LUT to contain the entire [0, 1] range, without losing half a texel at each side.
                float2 coordLUT = RemapHalfTexelCoordTo01(input.texCoord, FGDTEXTURE_RESOLUTION);

                // The FGD texture is parametrized as follows:
                // X = sqrt(dot(N, V))
                // Y = perceptualRoughness
                // These coordinate sampling must match the decoding in GetPreIntegratedDFG in Lit.hlsl,
                // i.e here we use perceptualRoughness, must be the same in shader
                // Note: with this angular parametrization, the LUT is almost perfectly linear,
                // except for the grazing angle when (NdotV -> 0).
                float NdotV = coordLUT.x * coordLUT.x;
                float perceptualRoughness = coordLUT.y;

                // Pre integrate GGX with smithJoint visibility as well as DisneyDiffuse
                float4 preFGD = IntegrateGGXAndDisneyDiffuseFGD(NdotV, PerceptualRoughnessToRoughness(perceptualRoughness));

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
