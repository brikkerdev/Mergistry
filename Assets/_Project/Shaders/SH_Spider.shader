Shader "Mergistry/SH_Spider"
{
    Properties
    {
        _Color       ("Color",      Color) = (0.26, 0.26, 0.28, 1)
        _LegPhase    ("Leg Phase",  Float) = 0.0   // per-instance phase offset
        _FlashAmount ("Hit Flash",  Float) = 0.0
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

            fixed4 _Color;
            float  _LegPhase;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            float thickSeg(float2 p, float2 a, float2 b, float t)
            {
                return sdSegment(p, a, b) - t;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float  t  = _GameTime * 3.0 + _LegPhase;

                // Body: ellipse
                float body = sdEllipse(uv, 0.30, 0.18);

                // 8 animated legs — tips oscillate vertically
                float legs = 1e9;
                // pair 1 (upper)
                legs = opUnion(legs, thickSeg(uv, float2( 0.28,  0.08), float2( 0.46,  0.22 + sin(t + 0.0) * 0.04), 0.024));
                legs = opUnion(legs, thickSeg(uv, float2(-0.28,  0.08), float2(-0.46,  0.22 + sin(t + 0.5) * 0.04), 0.024));
                // pair 2
                legs = opUnion(legs, thickSeg(uv, float2( 0.28,  0.02), float2( 0.47,  0.06 + sin(t + 1.0) * 0.04), 0.024));
                legs = opUnion(legs, thickSeg(uv, float2(-0.28,  0.02), float2(-0.47,  0.06 + sin(t + 1.5) * 0.04), 0.024));
                // pair 3
                legs = opUnion(legs, thickSeg(uv, float2( 0.28, -0.04), float2( 0.47, -0.10 + sin(t + 2.0) * 0.04), 0.024));
                legs = opUnion(legs, thickSeg(uv, float2(-0.28, -0.04), float2(-0.47, -0.10 + sin(t + 2.5) * 0.04), 0.024));
                // pair 4 (lower)
                legs = opUnion(legs, thickSeg(uv, float2( 0.25, -0.10), float2( 0.40, -0.26 + sin(t + 3.0) * 0.04), 0.024));
                legs = opUnion(legs, thickSeg(uv, float2(-0.25, -0.10), float2(-0.40, -0.26 + sin(t + 3.5) * 0.04), 0.024));

                float shape = opUnion(body, legs);
                float alpha = sdfAlpha(shape, 0.015) * _Color.a;
                clip(alpha - 0.01);

                float inBody = step(body, 0.0);
                fixed3 col = _Color.rgb * lerp(0.78, 1.0, inBody);

                // Eye cluster: 4 red dots (2×2)
                float2 eyePositions[4];
                eyePositions[0] = float2(-0.06,  0.06);
                eyePositions[1] = float2( 0.06,  0.06);
                eyePositions[2] = float2(-0.06, -0.02);
                eyePositions[3] = float2( 0.06, -0.02);
                [unroll]
                for (int e = 0; e < 4; e++)
                {
                    float ed = sdCircle(uv - eyePositions[e], 0.025);
                    float eA = sdfAlpha(ed, 0.006) * inBody;
                    col = lerp(col, fixed3(0.85, 0.10, 0.10), eA);
                }

                col = lerp(col, fixed3(1,1,1), _FlashAmount);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
