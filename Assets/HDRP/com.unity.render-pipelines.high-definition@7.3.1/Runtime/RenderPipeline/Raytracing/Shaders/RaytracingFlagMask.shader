Shader "Hidden/HDRP/RaytracingFlagMask"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Forward"
            Tags{ "LightMode" = "Forward" }

            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Common.hlsl"
            #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Color.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

            PackedVaryingsType Vert(AttributesMesh inputMesh)
            {
                VaryingsType varyingsType;
                varyingsType.vmesh = VertMesh(inputMesh);
                return PackVaryingsType(varyingsType);
            }

            float Frag(PackedVaryingsToPS packedInput) : SV_Target
            {
                return 1.0f;
            }

            ENDHLSL
        }

    }
    Fallback Off
}
