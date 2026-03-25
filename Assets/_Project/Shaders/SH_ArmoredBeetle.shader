Shader "Mergistry/SH_ArmoredBeetle"
{
    Properties
    {
        _BodyColor   ("Body Color",    Color) = (0.22, 0.28, 0.31, 1)
        _ArmorColor  ("Armor Color",   Color) = (0.55, 0.65, 0.70, 1)
        _ArmorPoints ("Armor Points",  Int)   = 2
        _ShieldFlash ("Shield Flash",  Float) = 0.0
        _FlashAmount ("Hit Flash",     Float) = 0.0
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
            fixed4 _ArmorColor;
            int    _ArmorPoints;
            float  _ShieldFlash;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Body: hexagon
                float body = sdHexagon(uv, 0.28);
                float alpha = sdfAlpha(body, 0.015);
                clip(alpha - 0.01);

                // Fake specular (directional highlight)
                float2 lightDir = normalize(float2(-0.5, 0.7));
                float specular = pow(max(dot(normalize(uv), lightDir), 0.0), 6) * 0.25;

                fixed3 bodyCol = _BodyColor.rgb + specular;

                // Eyes: two red dots
                float eyeL = sdCircle(uv - float2(-0.09, 0.10), 0.038);
                float eyeR = sdCircle(uv - float2( 0.09, 0.10), 0.038);
                float eyes = opUnion(eyeL, eyeR);
                float eyeBlend = sdfAlpha(eyes, 0.006) * step(body, 0.0);
                bodyCol = lerp(bodyCol, fixed3(0.9, 0.1, 0.1), eyeBlend);

                // Decorative leg hints: 6 short segments from hex faces (static)
                float legs = 1e9;
                [unroll]
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * 3.14159 / 3.0 + 0.52;
                    float2 start = float2(cos(ang), sin(ang)) * 0.26;
                    float2 end   = float2(cos(ang), sin(ang)) * 0.38;
                    legs = opUnion(legs, sdSegment(uv, start, end) - 0.012);
                }
                float legA = sdfAlpha(legs, 0.008);
                bodyCol = lerp(bodyCol, _BodyColor.rgb * 0.8, legA * (1.0 - step(body, 0.0)));
                alpha = max(alpha, legA * 0.85);

                // Armor diamond indicators above body
                [unroll]
                for (int a = 0; a < 2; a++)
                {
                    float2 dPos = float2((a - 0.5) * 0.18, 0.38);
                    float2 rotP = opRotate(uv - dPos, 0.7854); // 45 degrees
                    float diamond = sdBox(rotP, float2(0.06, 0.06));
                    float dAlpha  = sdfAlpha(diamond, 0.008);
                    fixed3 dCol = (a < _ArmorPoints) ? _ArmorColor.rgb : _BodyColor.rgb * 0.5;
                    bodyCol = lerp(bodyCol, dCol, dAlpha * step(1.0 - step(body, 0.0), 0.5));
                    alpha = max(alpha, dAlpha);
                }

                // Shield flash: white halo around body
                if (_ShieldFlash > 0.0)
                {
                    float shieldD = abs(body) - 0.018;
                    float shieldA = sdfAlpha(shieldD, 0.010) * _ShieldFlash;
                    bodyCol = lerp(bodyCol, fixed3(1,1,1), shieldA);
                    alpha = max(alpha, shieldA);
                }

                bodyCol = lerp(bodyCol, fixed3(1,1,1), _FlashAmount);
                return fixed4(bodyCol, alpha);
            }
            ENDCG
        }
    }
}
