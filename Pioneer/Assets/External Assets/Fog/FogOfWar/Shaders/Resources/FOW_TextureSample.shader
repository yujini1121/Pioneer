Shader "Hidden/FullScreen/FOW/TextureSample"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_local IS_2D IS_3D

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            //unity 2020 normal texture is VS, not WS. you can just remove this if you care about the extra varients.
            #pragma multi_compile_fragment _ _VS_NORMAL

            #pragma vertex Vert
            #pragma fragment Frag

            #include_with_pragmas "FogOfWarLogic.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #if UNITY_VERSION <= 202310
            uniform float4 _BlitTexture_TexelSize;
            #endif
            
            float4x4 _inverseProjectionMatrix;

            float _maxDistance;
            float2 _fowTiling;
            float2 _fowScrollSpeed;
            float4 _unKnownColor;

            sampler2D _fowTexture;
            bool _skipTriplanar;
            float3 _fowAxis;

            float4x4 _camToWorldMatrix;

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
                float2 uvSample = pos + (_Time.yy * _fowScrollSpeed);
                float4 fogColor = tex2D(_fowTexture, uvSample * _fowTiling);
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

                float3 normal = SampleSceneNormals(uv);
            #if _VS_NORMAL
                //this was required in unity 2020.3.28. when updating to 2020.3.48, its no longer required. not sure what version fixes it exactly.
                normal.z*=-1;   //unity can suck my.....
                normal = mul((float3x3)_camToWorldMatrix, normal);
                return float4(1, 1, 1, 1);
            #endif

                float3 powResult = pow(abs(normal), 8);
                float dotResult = dot(powResult, float3(1, 1, 1));
                //float3 lerpVals = round(powResult / dotResult);
                float3 lerpVals = (powResult / dotResult);
                if (_skipTriplanar)
                    lerpVals = _fowAxis;
                //uvSample = lerp(lerp(worldPos.xz, worldPos.yz, lerpVals.x), worldPos.xy, lerpVals.z) + (_Time * _fowScrollSpeed);
                float2 uvSample1 = worldPos.yz + (_Time.yy * _fowScrollSpeed);
                float2 uvSample2 = worldPos.xz + (_Time.yy * _fowScrollSpeed);
                float2 uvSample3 = worldPos.xy + (_Time.yy * _fowScrollSpeed);

                float4 fogColor = tex2D(_fowTexture, uvSample1 * _fowTiling) * lerpVals.x;
                fogColor += tex2D(_fowTexture, uvSample2 * _fowTiling) * lerpVals.y;
                fogColor += tex2D(_fowTexture, uvSample3 * _fowTiling) * lerpVals.z;
            #endif

                float coneCheckOut = 0;
                FOW_Sample_float(pos, height, coneCheckOut);

                //float4 fogColor = tex2D(_fowTexture, uvSample * _fowTiling) * lerpVals.x;
                fogColor = lerp(color, fogColor, _unKnownColor.w);
                OutOfBoundsCheck(pos, color);
                OutOfBoundsCheck(pos, fogColor);
                return float4(lerp(fogColor.rgb * _unKnownColor.rgb, color.rgb, coneCheckOut), color.a);
            }
            ENDHLSL
        }
    }
}