Shader "Mergistry/SH_MirrorSlime"
{
    Properties
    {
        _CopiedColor    ("Copied Potion Color", Color)  = (0.5, 0.5, 0.5, 0)   // a=0 → neutral (hue shift)
        _HueShiftSpeed  ("Hue Shift Speed",     Float)  = 0.3
        _FlashAmount    ("Hit Flash",            Float)  = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "AlchemistSDF.cginc"

            fixed4 _CopiedColor;
            float  _HueShiftSpeed;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float t = _GameTime;

                // Animated blob positions (4 blobs)
                float2 b0 = float2( sin(t * 0.7)  * 0.10,  cos(t * 0.5)  * 0.08);
                float2 b1 = float2( cos(t * 0.9)  * 0.09,  sin(t * 0.6)  * 0.10);
                float2 b2 = float2( sin(t * 0.6 + 1.2) * 0.11, cos(t * 0.8 + 0.7) * 0.07);
                float2 b3 = float2( cos(t * 0.5 + 2.1) * 0.08, sin(t * 0.7 + 1.5) * 0.09);

                float d0 = sdCircle(uv - b0, 0.18);
                float d1 = sdCircle(uv - b1, 0.16);
                float d2 = sdCircle(uv - b2, 0.15);
                float d3 = sdCircle(uv - b3, 0.14);

                float shape = opSmoothUnion(
                    opSmoothUnion(d0, d1, 0.14),
                    opSmoothUnion(d2, d3, 0.12),
                    0.12);

                float alpha = sdfAlpha(shape, 0.015);
                clip(alpha - 0.01);

                // Colour: hue shift when no copied potion, otherwise use potion color
                fixed3 col;
                if (_CopiedColor.a > 0.5)
                {
                    col = _CopiedColor.rgb;
                }
                else
                {
                    float hue = frac(t * _HueShiftSpeed);
                    col = hsvToRgb(hue, 0.70, 0.88);
                }

                // Slight inner gradient
                float inner = smoothstep(0.0, -0.15, shape);
                col = lerp(col, col * 0.6 + fixed3(0.2, 0.2, 0.25), inner * 0.4);

                col = lerp(col, fixed3(1,1,1), _FlashAmount);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
