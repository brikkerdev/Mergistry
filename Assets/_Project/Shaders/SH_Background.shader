Shader "Mergistry/SH_Background"
{
    Properties
    {
        _ColorBottom ("Color Bottom", Color) = (0.04, 0.05, 0.12, 1)
        _ColorTop    ("Color Top",    Color) = (0.08, 0.10, 0.22, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorBottom;
            fixed4 _ColorTop;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Subtle vignette-style gradient: dark at edges, lighter near center-top
                float t = i.uv.y;
                fixed4 col = lerp(_ColorBottom, _ColorTop, t);
                // Faint radial darkening at corners
                float2 centered = i.uv - 0.5;
                float vignette = 1.0 - dot(centered, centered) * 0.8;
                col.rgb *= max(vignette, 0.5);
                return col;
            }
            ENDCG
        }
    }
}
