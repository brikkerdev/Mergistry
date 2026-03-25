Shader "Mergistry/SH_Zone_Poison"
{
    Properties
    {
        _Intensity    ("Intensity",    Float) = 1.0
        _BubblePhase  ("Bubble Phase", Float) = 0.0
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

            float _Intensity;
            float _BubblePhase;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 uvC = uv - 0.5; // centered

                // Domain warped boundary
                float2 warpedUV = domainWarp(uv * 3.0, 0.06 + sin(_GameTime * 0.5) * 0.03);
                float n = valueNoise(warpedUV);

                float edgeFade = smoothstep(0.0, 0.12, uv.x) * smoothstep(1.0, 0.88, uv.x)
                               * smoothstep(0.0, 0.12, uv.y) * smoothstep(1.0, 0.88, uv.y);

                float alpha = (0.25 + n * 0.10) * _Intensity * edgeFade;

                fixed3 col = lerp(fixed3(0.20, 0.55, 0.05), fixed3(0.46, 1.00, 0.02), n * 0.5);

                // Bubbles: 6 rising
                [unroll]
                for (int b = 0; b < 6; b++)
                {
                    float bS = 0.12 + hash21(float2(b, 2.3)) * 0.08;
                    float bX = hash21(float2(b, 4.1)) * 0.8 + 0.1;
                    float bT = frac(_GameTime * bS + hash21(float2(b, 7.9)) + _BubblePhase);
                    float bY = bT;
                    float bR = 0.03 + hash21(float2(b, 11.2)) * 0.025;
                    float bD = sdCircle(uv - float2(bX, bY), bR);
                    float bA = sdfAlpha(bD, 0.006) * edgeFade * _Intensity;
                    col += fixed3(0.5, 1.0, 0.3) * bA * 0.6;
                    alpha = max(alpha, bA * 0.7);
                }

                return fixed4(col, clamp(alpha, 0, 0.75));
            }
            ENDCG
        }
    }
}
