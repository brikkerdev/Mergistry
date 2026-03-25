Shader "Mergistry/SH_MagnetGolem"
{
    Properties
    {
        _BodyColor   ("Body Color",    Color)   = (0.47, 0.56, 0.61, 1)
        _EyeColor    ("Eye Color",     Color)   = (1.00, 0.42, 0.10, 1)
        _PullActive  ("Pull Active",   Float)   = 0.0
        _FlashAmount ("Hit Flash",     Float)   = 0.0
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
            fixed4 _EyeColor;
            float  _PullActive;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Body: large rounded square
                float body = sdRoundBox(uv, float2(0.28, 0.28), 0.06);
                float alpha = sdfAlpha(body, 0.015);
                clip(alpha - 0.01);

                // Voronoi metallic texture
                float2 voro = voronoi(uv * 5.0 + 0.5);
                float metallic = 1.0 - smoothstep(0.0, 0.15, voro.x);
                fixed3 bodyCol = _BodyColor.rgb + metallic * 0.12;

                // Eyes: two orange circles with glow
                float eyeL = sdCircle(uv - float2(-0.10, 0.08), 0.055);
                float eyeR = sdCircle(uv - float2( 0.10, 0.08), 0.055);
                float eyes = opUnion(eyeL, eyeR);
                float eyeAlpha = sdfAlpha(eyes, 0.008) * step(body, 0.0);
                bodyCol = lerp(bodyCol, _EyeColor.rgb, eyeAlpha);

                // Eye glow contribution
                float eyeGlowL = glow(max(eyeL, 0.0), 0.015, 1.4);
                float eyeGlowR = glow(max(eyeR, 0.0), 0.015, 1.4);
                bodyCol += (eyeGlowL + eyeGlowR) * _EyeColor.rgb * 0.4;

                // Pull rings (animated, emanating outward)
                if (_PullActive > 0.1)
                {
                    float maxR = 0.55;
                    [unroll]
                    for (int k = 0; k < 3; k++)
                    {
                        float t = frac(_GameTime * 1.2 + k * 0.33);
                        float ringR = t * maxR;
                        float ringD = sdRing(uv, ringR, 0.008);
                        float ringA = sdfAlpha(ringD, 0.006) * (1.0 - t) * _PullActive;
                        bodyCol += _EyeColor.rgb * ringA * 0.5;
                        alpha = max(alpha, ringA * (1.0 - t) * 0.6);
                    }
                }

                bodyCol = lerp(bodyCol, fixed3(1,1,1), _FlashAmount);
                return fixed4(bodyCol, alpha);
            }
            ENDCG
        }
    }
}
