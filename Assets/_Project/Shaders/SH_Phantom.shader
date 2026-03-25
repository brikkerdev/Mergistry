Shader "Mergistry/SH_Phantom"
{
    Properties
    {
        _BodyColor      ("Body Color",       Color) = (0.85, 0.88, 0.95, 1)
        _Flicker        ("Visibility (0-1)", Float) = 0.6
        _TeleportFlash  ("Teleport Flash",   Float) = 0.0
        _FlashAmount    ("Hit Flash",        Float) = 0.0
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

            fixed4 _BodyColor;
            float  _Flicker;
            float  _TeleportFlash;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Main body: circle with wavy bottom
                float body = sdCircle(uv, 0.22);

                // Wavy shroud bottom (subtract upper plane to get ghost shape)
                float bottom = uv.y + 0.05 + sin(uv.x * 8.0 + _GameTime * 3.0) * 0.04;
                // Ghost: body below y=0.12 replaced by wavy edge
                float ghost = min(body, max(sdCircle(uv - float2(0, 0.10), 0.22), -bottom));

                float alpha = sdfAlpha(body, 0.015) * _Flicker;
                clip(alpha - 0.01);

                fixed3 bodyCol = _BodyColor.rgb;

                // Eyes: two constant-brightness dots (contrast with flickering body)
                float eyeL = sdCircle(uv - float2(-0.07, 0.04), 0.032);
                float eyeR = sdCircle(uv - float2( 0.07, 0.04), 0.032);
                float eyes = opUnion(eyeL, eyeR);
                float eyeBlend = sdfAlpha(eyes, 0.006) * step(body, 0.0);
                bodyCol = lerp(bodyCol, fixed3(0.1, 0.12, 0.25), eyeBlend);

                // Trail ghost: two offset circles with very low alpha
                float trail1 = sdCircle(uv - float2(0.08, -0.06), 0.18);
                float trail2 = sdCircle(uv - float2(0.15, -0.12), 0.14);
                float trailA1 = sdfAlpha(trail1, 0.015) * _Flicker * 0.25;
                float trailA2 = sdfAlpha(trail2, 0.015) * _Flicker * 0.12;

                // Teleport ring shockwave
                if (_TeleportFlash > 0.01)
                {
                    float ringR = _TeleportFlash * 0.5;
                    float ringD = sdRing(uv, ringR, 0.012 * (1.0 - _TeleportFlash));
                    float ringA = sdfAlpha(ringD, 0.008) * (1.0 - _TeleportFlash);
                    alpha = max(alpha, ringA);
                    bodyCol = lerp(bodyCol, fixed3(1,1,1), ringA);
                }

                bodyCol = lerp(bodyCol, fixed3(1,1,1), _FlashAmount);

                // Blend trail into output (additive-ish)
                alpha = max(alpha, trailA1 * 0.4);

                return fixed4(bodyCol, alpha);
            }
            ENDCG
        }
    }
}
