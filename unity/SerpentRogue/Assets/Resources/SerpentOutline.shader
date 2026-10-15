// Black cartoon outline ("inverted hull"): draws the mesh a bit fatter,
// showing only its BACK faces. It peeks out around the edges of the real mesh.
Shader "SerpentRogue/Outline"
{
    Properties
    {
        _OutlineColor ("Outline color", Color) = (0.04, 0.04, 0.07, 1)
        _Outline ("Outline width", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float _Outline;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                v.vertex.xyz += normalize(v.normal) * _Outline;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
