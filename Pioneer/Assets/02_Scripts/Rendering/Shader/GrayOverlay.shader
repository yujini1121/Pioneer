Shader "Custom/GrayOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        // 회색
        _GrayColor ("Gray Color", Color) = (0.5, 0.5, 0.5, 0.15) // 회색, 알파 0.15
        
        // 연습용 유리 색상
        _GlassColor ("Glass Tint Color", Color) = (0.8, 0.9, 1.0, 0.1) // 유리 느낌의 푸른빛 틴트, 알파 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "GrayOverlayPass"
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha // 알파 블렌딩 설정

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _GrayColor;
            float4 _GlassColor;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 원본 텍스처 샘플링
                //half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // 회색 오버레이와 블렌딩
                //return lerp(col, _GrayColor, _GrayColor.a);

                // === 연습용 유리 ===
                // 텍스처 샘플링 (선택적: 유리 틴트만 사용할 경우 무시 가능)
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // 유리 색상 틴트와 블렌딩, 알파는 낮게 설정
                half4 finalColor = _GrayColor;
                finalColor.a = _GrayColor.a; // 알파값을 매우 낮게 설정 (0.0~0.1)
                return finalColor;
            }
            ENDHLSL
        }
    }
}