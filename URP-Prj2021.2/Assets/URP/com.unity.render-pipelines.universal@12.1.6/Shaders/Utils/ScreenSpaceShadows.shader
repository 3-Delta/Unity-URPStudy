Shader "Hidden/Universal Render Pipeline/ScreenSpaceShadows"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

        HLSLINCLUDE

        //Keep compiler quiet about Shadows.hlsl.
        #include "Assets/URP/com.unity.render-pipelines.core@12.1.6/ShaderLibrary/Common.hlsl"
        #include "Assets/URP/com.unity.render-pipelines.core@12.1.6/ShaderLibrary/EntityLighting.hlsl"
        #include "Assets/URP/com.unity.render-pipelines.core@12.1.6/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Assets/URP/com.unity.render-pipelines.universal@12.1.6/ShaderLibrary/Core.hlsl"
        #include "Assets/URP/com.unity.render-pipelines.universal@12.1.6/ShaderLibrary/Shadows.hlsl"
        #include "Assets/URP/com.unity.render-pipelines.universal@12.1.6/ShaderLibrary/DeclareDepthTexture.hlsl"

        struct Attributes
        {
            float4 positionOS   : POSITION;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            half4  positionCS   : SV_POSITION;
            half4  uv           : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vertex(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

            float4 projPos = output.positionCS * 0.5;
            projPos.xy = projPos.xy + projPos.w;

            output.uv.xy = UnityStereoTransformScreenSpaceTex(input.texcoord);
            output.uv.zw = projPos.xy;

            return output;
        }

        /*
            https://blog.csdn.net/wodownload2/article/details/105202706/
            首先得到屏幕空间的深度图（摄像机视角下的深度信息），在延迟渲染中已经存在，在前向渲染中需要把场景渲染一遍，得到深度图
            然后将摄像机与光源重合（光源空间）下通过那个特有的pass通道渲染出阴影映射纹理（其实也是一张深度图）
            将屏幕空间下的深度图变换到光源空间，与阴影映射纹理进行比较，若前者深度更大（深度越大越不可见），则说明该区域虽然可见，但出于此光源的阴影中。
            通过比较后得到一张包含了阴影信息的屏幕空间阴影图，通过这张图对阴影进行采样，就可以得到最后的阴影效果了。

            重点：cameraDepth表示的是可以被看见的所有最近点，lightDepth同理， 都转换到lightSpace之后
            如果一个点cameraDepth > lightDepth, 那么表面这个点在阴影之内

            和shadowmap比较，ssShadow一般用于延迟渲染，因为cameraDepth天然就有
            foreword模式下，则因为只渲染camera可以看见的像素（通过z-test保证），所以相比较shadowmap也消耗少一点儿
         */
        half4 Fragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if UNITY_REVERSED_Z // 该模式下，z在[0, 1]之间，不需要转换到[-1, 1]之间，所以不需要*2-1
            float zCS = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy).r;
#else
            float zCS = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy).r;
            deviceDepth = deviceDepth * 2.0 - 1.0;
#endif

            float2 positionNDC = input.uv.xy;
            float3 posWS = ComputeWorldSpacePosition(positionNDC, zCS, unity_MatrixInvVP);

            //Fetch shadow coordinates for cascade.
            float4 coords = TransformWorldToShadowCoord(posWS);

            // 屏幕空间阴影只能用于 正交投影，平行光
            // Screenspace shadowmap is only used for directional lights which use orthogonal projection.
            ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
            half4 shadowParams = GetMainLightShadowParams();
            return SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), coords, shadowSamplingData, shadowParams, false);
        }

        ENDHLSL

        Pass
        {
            Name "ScreenSpaceShadows"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex   Vertex
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}
