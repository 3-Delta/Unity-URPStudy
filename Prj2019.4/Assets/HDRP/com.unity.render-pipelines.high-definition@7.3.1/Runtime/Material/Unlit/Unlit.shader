Shader "HDRP/Unlit"
{
    Properties
    {
        // Be careful, do not change the name here to _Color. It will conflict with the "fake" parameters (see end of properties) required for GI.
        _UnlitColor("Color", Color) = (1,1,1,1)
        _UnlitColorMap("ColorMap", 2D) = "white" {}

        [HDR] _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        // Used only to serialize the LDR and HDR emissive color in the material UI,
        // in the shader only the _EmissiveColor should be used
        [HideInInspector] _EmissiveColorLDR("EmissiveColor LDR", Color) = (0, 0, 0)
        [ToggleUI] _AlbedoAffectEmissive("Albedo Affect Emissive", Float) = 0.0
        [HideInInspector] _EmissiveIntensityUnit("Emissive Mode", Int) = 0
        [ToggleUI] _UseEmissiveIntensity("Use Emissive Intensity", Int) = 0
        _EmissiveIntensity("Emissive Intensity", Float) = 1
        _EmissiveExposureWeight("Emissive Pre Exposure", Range(0.0, 1.0)) = 1.0

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}
        [ToggleUI] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleUI] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleUI] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 1.0
        [Enum(Add, 0, Multiply, 1, Replace, 2)] _DistortionBlendMode("Distortion Blend Mode", Int) = 0
        [HideInInspector] _DistortionSrcBlend("Distortion Blend Src", Int) = 0
        [HideInInspector] _DistortionDstBlend("Distortion Blend Dst", Int) = 0
        [HideInInspector] _DistortionBlurSrcBlend("Distortion Blur Blend Src", Int) = 0
        [HideInInspector] _DistortionBlurDstBlend("Distortion Blur Blend Dst", Int) = 0
        [HideInInspector] _DistortionBlurBlendMode("Distortion Blur Blend Mode", Int) = 0
        _DistortionScale("Distortion Scale", Float) = 1
        _DistortionVectorScale("Distortion Vector Scale", Float) = 2
        _DistortionVectorBias("Distortion Vector Bias", Float) = -1
        _DistortionBlurScale("Distortion Blur Scale", Float) = 1
        _DistortionBlurRemapMin("DistortionBlurRemapMin", Float) = 0.0
        _DistortionBlurRemapMax("DistortionBlurRemapMax", Float) = 1.0

        // Transparency
        [ToggleUI]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _TransparentSortPriority("_TransparentSortPriority", Float) = 0

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _AlphaSrcBlend("__alphaSrc", Float) = 1.0
        [HideInInspector] _AlphaDstBlend("__alphaDst", Float) = 0.0
        [HideInInspector][ToggleUI] _ZWrite("__zw", Float) = 1.0
        [HideInInspector][ToggleUI] _TransparentZWrite("_TransparentZWrite", Float) = 0.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [Enum(UnityEditor.Rendering.HighDefinition.TransparentCullMode)] _TransparentCullMode("_TransparentCullMode", Int) = 2 // Back culling by default
        [HideInInspector] _ZTestModeDistortion("_ZTestModeDistortion", Int) = 8
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestTransparent("Transparent ZTest", Int) = 4 // Less equal
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal

        [ToggleUI] _EnableFogOnTransparent("Enable Fog", Float) = 0.0
        [ToggleUI] _DoubleSidedEnable("Double sided enable", Float) = 0.0

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 0  // StencilUsage.Clear
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 3 // StencilUsage.RequiresDeferredLighting | StencilUsage.SubsurfaceScattering
        // Depth prepass
        [HideInInspector] _StencilRefDepth("_StencilRefDepth", Int) = 0 // Nothing
        [HideInInspector] _StencilWriteMaskDepth("_StencilWriteMaskDepth", Int) = 8 // StencilUsage.TraceReflectionRay
        // Motion vector pass
        [HideInInspector] _StencilRefMV("_StencilRefMV", Int) = 32 // StencilUsage.ObjectMotionVector
        [HideInInspector] _StencilWriteMaskMV("_StencilWriteMaskMV", Int) = 32 // StencilUsage.ObjectMotionVector

        [ToggleUI] _AddPrecomputedVelocity("AddPrecomputedVelocity", Float) = 0.0

        // Distortion vector pass
        [HideInInspector] _StencilRefDistortionVec("_StencilRefDistortionVec", Int) = 2 // StencilUsage.DistortionVectors
        [HideInInspector] _StencilWriteMaskDistortionVec("_StencilWriteMaskDistortionVec", Int) = 2 // StencilUsage.DistortionVectors

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // For raytracing indirect illumination effects, we need to be able to define if the emissive part of the material should contribute or not (mainly for area light sources in order to avoid double contribution)
        // By default, the emissive is contributing
        [HideInInspector] _IncludeIndirectLighting("_IncludeIndirectLighting", Float) = 1.0

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE

    #pragma target 4.5

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature_local _ALPHATEST_ON
    // #pragma shader_feature_local _DOUBLESIDED_ON - We have no lighting, so no need to have this combination for shader, the option will just disable backface culling

    #pragma shader_feature_local _EMISSIVE_COLOR_MAP

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature_local _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
    #pragma shader_feature_local _ENABLE_FOG_ON_TRANSPARENT

    #pragma shader_feature_local _ADD_PRECOMPUTED_VELOCITY

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Assets/URP/com.unity.render-pipelines.core@7.5.1/ShaderLibrary/Common.hlsl"
    #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitProperties.hlsl"

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDUnlitShader" }

        // Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not be the meta pass.

        Pass
        {
            Name "SceneSelectionPass"
            Tags{ "LightMode" = "SceneSelectionPass" }

            Cull[_CullMode]

            ZWrite On

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing

            // Note: Require _ObjectId and _PassValue variables

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/ShaderPass/UnlitDepthPass.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma editor_sync_compilation

            ENDHLSL
        }

        Pass
        {
            Name "DepthForwardOnly"
            Tags{ "LightMode" = "DepthForwardOnly" }

            Stencil
            {
                WriteMask [_StencilWriteMaskDepth]
                Ref  [_StencilRefDepth]
                Comp Always
                Pass Replace
            }

            Cull[_CullMode]

            ZWrite On

            // Caution: When using MSAA we have normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            ColorMask 0 0

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing

            #pragma multi_compile _ WRITE_MSAA_DEPTH
            // Note we don't need to define WRITE_NORMAL_BUFFER

            #define SHADERPASS SHADERPASS_DEPTH_ONLY

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/ShaderPass/UnlitDepthPass.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }


        Pass
        {
            Name "MotionVectors"
            Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

            // If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity
            Stencil
            {
                WriteMask [_StencilWriteMaskMV]
                Ref [_StencilRefMV]
                Comp Always
                Pass Replace
            }

            Cull[_CullMode]

            ZWrite On

            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            ColorMask 0 1

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing

            #pragma multi_compile _ WRITE_MSAA_DEPTH
            // Note we don't need to define WRITE_NORMAL_BUFFER

            #define SHADERPASS SHADERPASS_MOTION_VECTORS

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/ShaderPass/UnlitSharePass.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        // Unlit shader always render in forward
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }


            Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTestDepthEqualForOpaque]

            Stencil
            {
                WriteMask[_StencilWriteMask]
                Ref[_StencilRef]
                Comp Always
                Pass Replace
            }

            Cull [_CullMode]

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing

            #pragma multi_compile _ DEBUG_DISPLAY

            #ifdef DEBUG_DISPLAY
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Debug/DebugDisplay.hlsl"
            #endif

            #define SHADERPASS SHADERPASS_FORWARD_UNLIT

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/ShaderPass/UnlitSharePass.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "META" }

            Cull Off

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/ShaderPass/UnlitSharePass.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing

            #define SHADERPASS SHADERPASS_SHADOWS
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/ShaderPass/UnlitDepthPass.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "DistortionVectors"
            Tags { "LightMode" = "DistortionVectors" } // This will be only for transparent object based on the RenderQueue index

            Stencil
            {
                WriteMask [_StencilRefDistortionVec]
                Ref [_StencilRefDistortionVec]
                Comp Always
                Pass Replace
            }

            Blend [_DistortionSrcBlend] [_DistortionDstBlend], [_DistortionBlurSrcBlend] [_DistortionBlurDstBlend]
            BlendOp Add, [_DistortionBlurBlendOp]
            ZTest [_ZTestModeDistortion]
            ZWrite off
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma multi_compile_instancing
            
            #define SHADERPASS SHADERPASS_DISTORTION

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/ShaderPass/UnlitDistortionPass.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
			#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "IndirectDXR"
            Tags{ "LightMode" = "IndirectDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11

            #pragma raytracing surface_shader

            #define SHADERPASS SHADERPASS_RAYTRACING_INDIRECT

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ForwardDXR"
            Tags{ "LightMode" = "ForwardDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11

            #pragma raytracing surface_shader

            #define SHADERPASS SHADERPASS_RAYTRACING_FORWARD

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingForward.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "GBufferDXR"
            Tags{ "LightMode" = "GBufferDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11

            #pragma raytracing surface_shader

            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON

            #define SHADERPASS SHADERPASS_RAYTRACING_GBUFFER

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/NormalBuffer.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/StandardLit/StandardLit.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitRaytracing.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "VisibilityDXR"
            Tags{ "LightMode" = "VisibilityDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11

            #pragma raytracing surface_shader

            #define SHADOW_LOW
            #pragma multi_compile _ TRANSPARENT_COLOR_SHADOW

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingVisibility.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "PathTracingDXR"
            Tags{ "LightMode" = "PathTracingDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11

            #pragma raytracing surface_shader

            #define SHADOW_LOW
            #define SHADERPASS SHADERPASS_PATH_TRACING

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Material.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/Unlit.hlsl"

            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/Material/Unlit/UnlitData.hlsl"
            #include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "Rendering.HighDefinition.UnlitGUI"
}