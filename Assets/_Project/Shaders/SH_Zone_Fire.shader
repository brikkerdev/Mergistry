Shader "Mergistry/SH_Zone_Fire"
{
    Properties
    {
        _Intensity        ("Intensity",          Float) = 1.0
        _LifetimeNorm     ("Lifetime Normalized",Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One   // additive
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
            float _LifetimeNorm;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv; // [0,1]

                // Upward-flowing fire fbm
                float2 fireUV = float2(uv.x * 3.0, uv.y * 6.0 - _GameTime * 2.0);
                float n = fbm(fireUV, 3);

                // Edge fade (soft edges from all sides)
                float edgeFade = smoothstep(0.0, 0.12, uv.x) * smoothstep(1.0, 0.88, uv.x)
                               * smoothstep(0.0, 0.10, uv.y) * smoothstep(1.0, 0.90, uv.y);

                // Alpha: brighter at top, flame tips up
                float alpha = n * _Intensity * edgeFade * smoothstep(0.0, 0.25, uv.y)
                            * (1.0 - _LifetimeNorm * 0.5);

                // Fire color ramp: black → dark red → orange → yellow → near-white
                fixed3 c0 = fixed3(0.05, 0.00, 0.00);
                fixed3 c1 = fixed3(0.80, 0.15, 0.00);
                fixed3 c2 = fixed3(1.00, 0.50, 0.05);
                fixed3 c3 = fixed3(1.00, 0.90, 0.50);

                fixed3 col = lerp(c0, c1, smoothstep(0.0, 0.3, n));
                col = lerp(col, c2, smoothstep(0.3, 0.6, n));
                col = lerp(col, c3, smoothstep(0.6, 0.9, n));

                return fixed4(col, clamp(alpha, 0, 0.85));
            }
            ENDCG
        }
    }
}
