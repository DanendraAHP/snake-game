// Cartoon "toon" shading: the light is split into 3 flat bands instead of a
// smooth gradient. Works in both the Built-in pipeline and URP because it does
// its own simple lighting (light direction comes from the script: _SR_LightDir).
Shader "SerpentRogue/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Emission ("Flat (unlit) amount", Range(0,1)) = 0
        _EmissionColor ("Glow color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _SR_LightDir;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Emission;
            float4 _EmissionColor;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * tex2D(_MainTex, i.uv);
                float3 l = normalize(_SR_LightDir.xyz + float3(0, 0.0001, 0));
                float ndl = dot(normalize(i.normal), l);
                float band = ndl > 0.35 ? 1.0 : (ndl > -0.2 ? 0.72 : 0.5);
                float3 col = lerp(baseColor.rgb * band, baseColor.rgb, _Emission) + _EmissionColor.rgb;
                return float4(col, 1);
            }
            ENDCG
        }
    }
}
