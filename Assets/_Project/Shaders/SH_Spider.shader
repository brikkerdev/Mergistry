Shader "Mergistry/SH_Spider"
{
    Properties
    {
        _Color ("Color", Color) = (0.50, 0.50, 0.55, 1)
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // Thick segment: segment distance minus half-thickness
            float thickSegment(float2 p, float2 a, float2 b, float thickness)
            {
                return sdSegment(p, a, b) - thickness;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Body: ellipse
                float body = sdEllipse(uv, 0.30, 0.18);

                // 8 legs (4 pairs, left+right mirrored)
                float legs = 1e9;
                // Pair 1 (upper)
                legs = opUnion(legs, thickSegment(uv, float2( 0.28,  0.08), float2( 0.46,  0.22), 0.025));
                legs = opUnion(legs, thickSegment(uv, float2(-0.28,  0.08), float2(-0.46,  0.22), 0.025));
                // Pair 2
                legs = opUnion(legs, thickSegment(uv, float2( 0.28,  0.02), float2( 0.47,  0.06), 0.025));
                legs = opUnion(legs, thickSegment(uv, float2(-0.28,  0.02), float2(-0.47,  0.06), 0.025));
                // Pair 3
                legs = opUnion(legs, thickSegment(uv, float2( 0.28, -0.04), float2( 0.47, -0.10), 0.025));
                legs = opUnion(legs, thickSegment(uv, float2(-0.28, -0.04), float2(-0.47, -0.10), 0.025));
                // Pair 4 (lower)
                legs = opUnion(legs, thickSegment(uv, float2( 0.25, -0.10), float2( 0.40, -0.26), 0.025));
                legs = opUnion(legs, thickSegment(uv, float2(-0.25, -0.10), float2(-0.40, -0.26), 0.025));

                float shape = opUnion(body, legs);

                float alpha = sdfAlpha(shape, 0.015) * _Color.a;
                clip(alpha - 0.01);

                // Slightly darker legs than body
                float inBody = sdfAlpha(body, 0.010);
                fixed3 col = _Color.rgb * lerp(0.75, 1.0, inBody);

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
