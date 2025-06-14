Shader"Custom/MirrorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
ZTest Always
Cull Off
ZWrite Off
            Fog {
Mode Off}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;

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
    o.uv = float2(1 - v.uv.x, v.uv.y); // ÁÂ¿ì ¹ÝÀü
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    return tex2D(_MainTex, i.uv);
}
            ENDCG
        }
    }
}
