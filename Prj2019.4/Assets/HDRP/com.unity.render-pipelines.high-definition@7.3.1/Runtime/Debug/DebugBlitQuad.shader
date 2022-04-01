Shader "Hidden/HDRP/DebugBlitQuad"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Common.hlsl"
            #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Debug/DebugDisplay.hlsl"

            TEXTURE2D(_InputTexture);
            SAMPLER(sampler_InputTexture);
            float _Mipmap;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_LOD(_InputTexture, sampler_InputTexture, input.texcoord.xy, _Mipmap) * exp2(_DebugExposure);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
