Shader "Hidden/FullScreen/FOW/Blur"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_local IS_2D IS_3D

            #pragma vertex Vert
            #pragma fragment Frag

            #include_with_pragmas "FogOfWarLogic.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #if UNITY_VERSION <= 202310
            uniform float4 _BlitTexture_TexelSize;
            #endif

            float _maxDistance;
            float2 _fowTiling;
            float _fowScrollSpeed;
            float4 _unKnownColor;

            float _blurStrength;
            //float _blurPixelOffset;
            float _blurPixelOffsetMin;
            float _blurPixelOffsetMax;
            int _blurSamples;
            float _samplePeriod;

            float4x4 _camToWorldMatrix;
            float4x4 _inverseProjectionMatrix;

            float4 Frag (Varyings i) : SV_Target
            {
                //float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, i.texcoord, _BlitMipLevel);

                float2 pos;
                float height;
            #if IS_2D
                
                pos = (i.texcoord * float2(2,2) - float2(1,1)) * _cameraSize * float2(_BlitTexture_TexelSize.z / _BlitTexture_TexelSize.w, 1);
                pos+= _cameraPosition;
                FOW_Rotate_Degrees_float(pos, _cameraPosition, -_cameraRotation, pos);
                height = 0;
            #elif IS_3D
                float2 uv = i.texcoord;

            #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(i.texcoord);
            #else
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
            #endif

                float3 vpos = ComputeViewSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_P);
                if (vpos.z > _maxDistance)
                    return color;

                vpos.z*=-1;
                float4 worldPos = mul(_camToWorldMatrix, float4(vpos, 1));

                GetFowSpacePosition(worldPos.xyz, pos, height);
            #endif

                float coneCheckOut = 0;
                FOW_Sample_float(pos, height, coneCheckOut);

                float2 offset;
                float4 blurColor = color;
                float randomStart = frac(sin(dot(i.texcoord, float2(12.9898, 78.233))) * 43758.5453) * 6.283185;    //random 0-1 * tau
                float distanceFromCenter;
                int numSamples = min(_blurSamples, 100);
                for (int s = 0; s < numSamples; s++)
                {
                    sincos(_samplePeriod * s, offset.x, offset.y);
                    distanceFromCenter = frac(sin(dot(float2(randomStart, randomStart) * s, float2(12.9898, 78.233))) * 43758.5453) * (_blurPixelOffsetMax - _blurPixelOffsetMin) + _blurPixelOffsetMin;
                    blurColor += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, i.texcoord + (offset * distanceFromCenter * float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y)), _BlitMipLevel);
                    //blurColor += tex2D(_BlitTexture, i.texcoord + (offset * float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * _blurPixelOffset));
                }
                blurColor /= (numSamples + 1);
                OutOfBoundsCheck(pos, color);
                OutOfBoundsCheck(pos, blurColor);
                blurColor = lerp(color, blurColor, _blurStrength);

                return float4(lerp(blurColor.rgb * _unKnownColor.rgb, color.rgb, coneCheckOut), color.a);
            }
            ENDHLSL
        }
    }
}