Shader "Mergistry/SH_Floor"
{
    Properties
    {
        _CellColor   ("Cell Color",   Color) = (0.13, 0.15, 0.20, 1)
        _BorderColor ("Border Color", Color) = (0.20, 0.23, 0.32, 1)
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.04
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _CellColor;
            fixed4 _BorderColor;
            float  _BorderWidth;

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
                // Draw a border around the cell using UV distance from edges
                float2 borderDist = min(i.uv, 1.0 - i.uv);
                float  onBorder   = step(borderDist.x, _BorderWidth) +
                                    step(borderDist.y, _BorderWidth);
                onBorder = saturate(onBorder);

                // Subtle inner shading (slightly lighter in center)
                float2 centered = i.uv - 0.5;
                float  inner    = 1.0 - dot(centered, centered) * 0.6;
                fixed4 cell = _CellColor * (0.85 + inner * 0.15);

                return lerp(cell, _BorderColor, onBorder);
            }
            ENDCG
        }
    }
}
