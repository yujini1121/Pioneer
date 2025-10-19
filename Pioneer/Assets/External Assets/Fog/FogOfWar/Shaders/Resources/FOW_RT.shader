Shader "Hidden/FullScreen/FOW/FOW_RT"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma multi_compile_local _ USE_REGROW
            #pragma multi_compile_local _ USE_FADEIN
            #pragma multi_compile_local _ IGNORE_HEIGHT

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include_with_pragmas "FogOfWarLogic.hlsl"
            //#include "../FogOfWarLogic.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            float4x4 _camToWorldMatrix;
            float4x4 _inverseProjectionMatrix;
			
            float4 _unKnownColor;
            float _regrowSpeed;
            float _maxRegrowAmount;


            fixed4 frag (v2f i) : SV_Target
            {
                //fixed4 color = tex2D(_MainTex, i.uv);

                float coneCheckOut = 0;
				float2 pos = float2(((i.uv.x-.5) * _worldBounds.x) + _worldBounds.y, ((i.uv.y-.5) * _worldBounds.z) + _worldBounds.w);

                //return float4(pos.x, pos.y, 0,1);

                FOW_Sample_Raw_float(pos, 0, coneCheckOut);
                //CustomCurve_float(coneCheckOut, coneCheckOut);

                #if USE_REGROW
                    //return tex2D(_MainTex, i.uv);
                    float opacitySample = 1 - tex2D(_MainTex, i.uv).w;

                    #if USE_FADEIN

                        float maxMove = unity_DeltaTime.x * _regrowSpeed;
                        float difference = coneCheckOut - opacitySample;
                        if (abs(difference) > maxMove)
                            coneCheckOut = opacitySample + sign(difference) * maxMove;

                        if (difference < 0 && coneCheckOut < _maxRegrowAmount)
                            coneCheckOut = opacitySample;


                    #else
                        
                        coneCheckOut = max(coneCheckOut, opacitySample - unity_DeltaTime.x * _regrowSpeed);

                        //if (coneCheckOut - oldOpacitySample <= 0 && opacitySample < _maxRegrowAmount)
                        //if (opacitySample > _maxRegrowAmount && coneCheckOut < _maxRegrowAmount)
                        if (coneCheckOut - opacitySample < 0 && coneCheckOut < _maxRegrowAmount)
                            coneCheckOut = opacitySample;

                    #endif

                    coneCheckOut = clamp(coneCheckOut, 0, 1);

                #endif

				//return float4(coneCheckOut,coneCheckOut,coneCheckOut,1);
                //return float4(lerp(color.rgb * _unKnownColor, color.rgb, coneCheckOut), color.a);
                return float4(_unKnownColor.rgb, (1 - coneCheckOut));
                return float4(_unKnownColor.rgb, (1 - coneCheckOut) * _unKnownColor.a);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _regrowSpeed;
            float _maxRegrowAmount;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                color.w = 1 - color.w;
                return color;
                //color.w-= unity_DeltaTime.z;
                if (color.w > _maxRegrowAmount)
                {
                    color.w -= unity_DeltaTime.x * _regrowSpeed;
                    color.w = clamp(color.w, _maxRegrowAmount, 1);
                }
                
                return color;
            }
            ENDCG
        }
    }
}
