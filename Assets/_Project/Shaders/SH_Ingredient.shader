Shader "Mergistry/SH_Ingredient"
{
    Properties
    {
        _Color       ("Color Primary",   Color) = (0.90, 0.30, 0.20, 1)
        _Color2      ("Color Secondary", Color) = (1.00, 0.65, 0.10, 1)
        _ElementType ("Element Type",    Int)   = 0   // 0=Ignis 1=Aqua 2=Toxin 3=Lux 4=Umbra
        _IdlePhase   ("Idle Phase",      Float) = 0.0
        _Selected    ("Selected",        Float) = 0.0
        _Dissolve    ("Dissolve",        Float) = 0.0
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
            fixed4 _Color2;
            int    _ElementType;
            float  _IdlePhase;
            float  _Selected;
            float  _Dissolve;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Idle wobble
                uv.y += sin(_GameTime * 2.0 + _IdlePhase) * 0.018;
                uv.x += cos(_GameTime * 1.7 + _IdlePhase) * 0.008;

                // Dissolve
                float dissolveNoise = valueNoise(uv * 8.0 + float2(1.3, 2.7));
                clip(dissolveNoise - _Dissolve);

                float d = sdCircle(uv, 0.40);
                float alpha = sdfAlpha(d, 0.015) * _Color.a;
                clip(alpha - 0.01);

                fixed3 col = _Color.rgb;

                // ── Per-element fill ─────────────────────────────────────────
                if (_ElementType == 0) // Ignis: fbm fire
                {
                    float2 fireUV = uv * 4.0 + float2(0, -_GameTime * 2.0);
                    float fire = fbm(fireUV, 3);
                    col = lerp(_Color.rgb, _Color2.rgb, fire);
                    col = lerp(col, fixed3(1,1,0.8), fire * fire * 0.5);
                }
                else if (_ElementType == 1) // Aqua: wave pattern
                {
                    float wave = sin(uv.x * 10.0 + _GameTime * 3.0) * 0.04;
                    float grad = (uv.y + wave + 0.4) / 0.8;
                    col = lerp(_Color2.rgb, _Color.rgb, clamp(grad, 0, 1));
                    float stripe = step(sin((uv.y + wave) * 14.0 + _GameTime), 0.75);
                    col += fixed3(0.3, 0.4, 0.5) * stripe * 0.15;
                }
                else if (_ElementType == 2) // Toxin: bubble + domain warp
                {
                    float2 warpUV = domainWarp(uv * 3.0, 0.15 + sin(_GameTime * 0.5) * 0.05);
                    float wobble = valueNoise(warpUV) * 0.3;
                    col = lerp(_Color.rgb, _Color2.rgb, wobble);
                    // Bubbles
                    [unroll]
                    for (int b = 0; b < 4; b++)
                    {
                        float bPhase = _GameTime * (0.5 + b * 0.2) + b * 1.7;
                        float2 bPos = float2(hash21(float2(b, 3.1)) * 0.5 - 0.25,
                                             fmod(bPhase * 0.15, 0.7) - 0.35);
                        float bD = sdCircle(uv - bPos, 0.025 + hash21(float2(b, 7)) * 0.015);
                        col += fixed3(0.5, 1.0, 0.3) * sdfAlpha(bD, 0.005) * 0.5;
                    }
                }
                else if (_ElementType == 3) // Lux: bright star + rays
                {
                    float rayA = atan2(uv.y, uv.x);
                    float rayPat = step(sin(rayA * 4.0 + _GameTime * 2.0), 0.3);
                    float radFade = 1.0 - length(uv) / 0.4;
                    col = lerp(_Color.rgb, _Color2.rgb, rayPat * radFade);
                    col += fixed3(1.0, 1.0, 0.8) * radFade * radFade * 0.4;
                }
                else // Umbra (4): spiral warp + purple depth
                {
                    float2 spiralUV = opRotate(uv, _GameTime * 1.2 + length(uv) * 4.0);
                    float swirl = gradientNoise(spiralUV * 4.0);
                    col = lerp(_Color.rgb, _Color2.rgb, swirl);
                }

                // Inner highlight
                float hi = smoothstep(0.15, -0.05, sdCircle(uv - float2(-0.12, 0.14), 0.22));
                col += fixed3(0.25, 0.25, 0.25) * hi * 0.4;

                // Selected glow outline
                if (_Selected > 0.1)
                {
                    float edgeD = abs(d) - 0.012;
                    float edgeA = sdfAlpha(edgeD, 0.008) * _Selected;
                    col = lerp(col, fixed3(1.0, 1.0, 0.8), edgeA * 0.9);
                    alpha = max(alpha, edgeA);
                }

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
