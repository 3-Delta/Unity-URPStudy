#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#ifdef _ALPHATEST_ON
#define ATTRIBUTES_NEED_TEXCOORD0

#define VARYINGS_NEED_TEXCOORD0
#endif

// This include will define the various Attributes/Varyings structure
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
