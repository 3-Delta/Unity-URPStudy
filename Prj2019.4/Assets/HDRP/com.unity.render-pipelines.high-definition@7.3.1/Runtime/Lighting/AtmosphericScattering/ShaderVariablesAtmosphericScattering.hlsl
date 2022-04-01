#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Lighting/AtmosphericScattering/ShaderVariablesAtmosphericScattering.cs.hlsl"
#else
    TEXTURE3D(_VBufferLighting);
    TEXTURECUBE_ARRAY(_SkyTexture);

    #define _MipFogNear                     _MipFogParameters.x
    #define _MipFogFar                      _MipFogParameters.y
    #define _MipFogMaxMip                   _MipFogParameters.z

    #define _FogColor                       _FogColor
#endif

