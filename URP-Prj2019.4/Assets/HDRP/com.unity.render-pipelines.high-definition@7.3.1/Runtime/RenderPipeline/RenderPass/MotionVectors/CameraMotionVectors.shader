Shader "Hidden/HDRP/CameraMotionVectors"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 128
        [HideInInspector] _StencilMask("_StencilMask", Int) = 128
    }

    HLSLINCLUDE

        #pragma target 4.5

        #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Common.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
        #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Builtin/BuiltinData.hlsl"        

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }

        void Frag(Varyings input, out float4 outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float depth = LoadCameraDepth(input.positionCS.xy);

            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

            float4 worldPos = float4(posInput.positionWS, 1.0);
            float4 prevPos = worldPos;

            float4 prevClipPos = mul(UNITY_MATRIX_PREV_VP, prevPos);
            float4 curClipPos = mul(UNITY_MATRIX_UNJITTERED_VP, worldPos);

            float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
            float2 positionCS = curClipPos.xy / curClipPos.w;

            // Convert from Clip space (-1..1) to NDC 0..1 space
            float2 motionVector = (positionCS - previousPositionCS);
#if UNITY_UV_STARTS_AT_TOP
            motionVector.y = -motionVector.y;
#endif

            // Convert motionVector from Clip space (-1..1) to NDC 0..1 space
            // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
            // Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (motionVector * 0.5)
            EncodeMotionVector(motionVector * 0.5, outColor);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            // We will perform camera motion vector only where there is no object motion vectors
            Stencil
            {
                WriteMask [_StencilMask]
                ReadMask [_StencilMask]
                Ref [_StencilRef]
                Comp NotEqual
                Fail Zero   // We won't need the bit anymore.
            }

            Cull Off ZWrite Off
            ZTest Less // Required for XR occlusion mesh optimization

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
