Shader "Hidden/HDRP/TerrainLit_BasemapGen"
{
    Properties
    {
        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }

    SubShader
    {
        Tags { "SplatCount" = "8" }

        HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #define SURFACE_GRADIENT // Must use Surface Gradient as the normal map texture format is now RG floating point
        #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Common.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
        #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/NormalSurfaceGradient.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/TerrainLit/TerrainLitSurfaceData.hlsl"

        // Terrain builtin keywords
        #pragma shader_feature_local _TERRAIN_8_LAYERS
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _MASKMAP

        #pragma shader_feature_local _TERRAIN_BLEND_HEIGHT
        #define _TERRAIN_BASEMAP_GEN

        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/TerrainLit/TerrainLit_Splatmap_Includes.hlsl"

        CBUFFER_START(UnityTerrain)
            UNITY_TERRAIN_CB_VARS
            float4 _Control0_ST;
        CBUFFER_END

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 texcoord : TEXCOORD0;
        };

        #pragma vertex Vert
        #pragma fragment Frag

        float2 ComputeControlUV(float2 uv)
        {
            // adjust splatUVs so the edges of the terrain tile lie on pixel centers
            return (uv * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;
        }

        Varyings Vert(uint vertexID : SV_VertexID)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
            output.texcoord.xy = TRANSFORM_TEX(GetFullScreenTriangleTexCoord(vertexID), _Control0);
            output.texcoord.zw = ComputeControlUV(output.texcoord.xy);
            return output;
        }

        ENDHLSL

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                TerrainLitSurfaceData surfaceData;
                InitializeTerrainLitSurfaceData(surfaceData);
                TerrainSplatBlend(input.texcoord.zw, input.texcoord.xy, surfaceData);
                return float4(surfaceData.albedo, surfaceData.smoothness);
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "RG16"
                "Size" = "1/4"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

            float2 Frag(Varyings input) : SV_Target
            {
                TerrainLitSurfaceData surfaceData;
                InitializeTerrainLitSurfaceData(surfaceData);
                TerrainSplatBlend(input.texcoord.zw, input.texcoord.xy, surfaceData);
                return float2(surfaceData.metallic, surfaceData.ao);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
