Shader "Custom/OutlineShader"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,1,0,1)
        _Outline ("Outline Width", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // URP Core
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _Outline;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;

                // 노멀 방향으로 외곽선 팽창 (오브젝트 공간)
                float3 n = normalize(v.normalOS);
                float3 posOS = v.positionOS.xyz + n * _Outline;

                o.positionCS = TransformObjectToHClip(float4(posOS, 1.0));
                o.color = _OutlineColor;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}