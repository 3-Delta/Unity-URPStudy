Shader "ColorPyramidPS"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: Bilinear tri
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma editor_sync_compilation
                #pragma target 4.5
                #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
                #pragma vertex Vert
                #pragma fragment Frag
                #define DISABLE_TEXTURE2D_X_ARRAY 1
                #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/RenderPass/ColorPyramidPS.hlsl"
            ENDHLSL
        }

        // 1: no tex array
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma editor_sync_compilation
                #pragma target 4.5
                #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
                #pragma vertex Vert
                #pragma fragment Frag
                #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/RenderPass/ColorPyramidPS.hlsl"
        ENDHLSL
        }

    }
        Fallback Off
}
