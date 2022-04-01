#ifndef CUSTOM_PASS_RENDERERS
#define CUSTOM_PASS_RENDERERS

#define SHADERPASS SHADERPASS_FORWARD_UNLIT

//-------------------------------------------------------------------------------------
// Define
//-------------------------------------------------------------------------------------

#include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Common.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitProperties.hlsl"

#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"

#include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/BuiltinUtilities.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/MaterialUtilities.hlsl"
#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Decal/DecalUtilities.hlsl"

float _CustomPassInjectionPoint;
float _FadeValue;

#endif // CUSTOM_PASS_RENDERERS
