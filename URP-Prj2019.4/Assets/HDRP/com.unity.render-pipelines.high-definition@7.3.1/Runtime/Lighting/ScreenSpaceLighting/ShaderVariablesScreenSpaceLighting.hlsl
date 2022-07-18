#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Lighting/ScreenSpaceLighting/ShaderVariablesScreenSpaceLighting.cs.hlsl"
#else
    TEXTURE2D_X(_AmbientOcclusionTexture);
    TEXTURE2D_X(_CameraMotionVectorsTexture);
    TEXTURE2D_X(_SsrLightingTexture);
#endif
