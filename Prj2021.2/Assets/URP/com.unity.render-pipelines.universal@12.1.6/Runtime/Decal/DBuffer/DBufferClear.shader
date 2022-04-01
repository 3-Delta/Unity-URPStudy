Shader "Hidden/Universal Render Pipeline/DBufferClear"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "DBufferClear"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL

            #include "Assets/URP/com.unity.render-pipelines.universal@12.1.6/Shaders/Utils/Fullscreen.hlsl"
            #include "Assets/URP/com.unity.render-pipelines.core@12.1.6/ShaderLibrary/Color.hlsl"
            #include "Assets/URP/com.unity.render-pipelines.universal@12.1.6/ShaderLibrary/DBuffer.hlsl"

            void Fragment(
                Varyings input,
                OUTPUT_DBUFFER(outDBuffer))
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                outDBuffer0 = half4(0, 0, 0, 1);
#if defined(_DBUFFER_MRT3) || defined(_DBUFFER_MRT2)
                outDBuffer1 = half4(0.5f, 0.5f, 0.5f, 1);
#endif
#if defined(_DBUFFER_MRT3)
                outDBuffer2 = half4(0, 0, 0, 1);
#endif
            }
            ENDHLSL
        }
    }
}
