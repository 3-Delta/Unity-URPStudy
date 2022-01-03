Shader "URPStudy/PlanarShadow"
{
    Properties
    {
        _GroundHeight("_GroundHeight", Float) = 0
        _GroundNormalWS("_GroundNormalWS", Vector) = (0, 0, 1, 0)
        _ShadowColor("_ShadowColor", Color) = (0, 0, 0, 1)
        _ShadowFalloff("_ShadowFalloff", Range(0, 1)) = 0.05
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        
        // 物体自身着色使用URP自带的ForwardLit pass
        USEPASS "Universal Render Pipeline/Lit/ForwardLit"

        Pass
        {
            Name "PlanarShadow"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            //深度稍微偏移防止阴影与地面穿插
            Offset -1 , 0

            HLSLPROGRAM
            #include "Assets/URP/com.unity.render-pipelines.universal@7.5.1/ShaderLibrary/Core.hlsl"
            #include "Assets/URP/com.unity.render-pipelines.universal@7.5.1/ShaderLibrary/Lighting.hlsl"

            #include "Assets/Ext/Shader/Utils.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerDrawPlanarShadow)
                // 平面高度
                float _GroundHeight;
                // 平面法线
                float4 _GroundNormalWS;
                // 阴影颜色
                float4 _ShadowColor;
                float _ShadowFalloff;
            CBUFFER_END

            float3 ShadowProjectPos(float4 vertPosOS)
            {
                float3 shadowPos;

                // 得到顶点的世界空间坐标
                float3 vertPosWS = TransformObjectToWorld(vertPosOS).xyz;

                // 灯光方向
                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);

                // 阴影的世界空间坐标（低于地面的部分不做改变）
                shadowPos.y = min(vertPosWS.y, _GroundHeight);
                // 相似三角形
                shadowPos.xz = vertPosWS.xz - lightDirWS.xz * max(0, vertPosWS.y - _GroundHeight) / lightDirWS.y;

                return shadowPos;
            }

            v2f vert(appdata v)
            {
                v2f o;
                // 得到阴影的世界空间坐标
                float3 shadowPosWS = ShadowProjectPos(v.vertex);

                // 转换到裁切空间
                // o.vertex = UnityWorldToClipPos(shadowPos);
                o.vertex = TransformWorldToHClip(shadowPosWS);

                // 得到中心点世界坐标
                float3 center = float3(unity_ObjectToWorld[0].w, _GroundHeight, unity_ObjectToWorld[2].w);
                // 计算阴影衰减
                float falloff = 1 - saturate(distance(shadowPosWS, center) * _ShadowFalloff);

                //阴影颜色
                o.color = _ShadowColor;
                o.color.a *= falloff;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}
