#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// NEWLITTODO : Handling of TESSELATION, DISPLACEMENT, HEIGHTMAP, WIND

#define ATTRIBUTES_NEED_TEXCOORD0

#define VARYINGS_NEED_TEXCOORD0

// This include will define the various Attributes/Varyings structure
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
