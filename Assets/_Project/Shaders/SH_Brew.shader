Shader "Mergistry/SH_Brew"
{
    Properties
    {
        _Color         ("Liquid Color",   Color) = (0.90, 0.35, 0.10, 1)
        _FillHeight    ("Fill Height",    Float) = 0.66   // 0.33 / 0.66 / 1.0
        _GlowIntensity ("Glow Intensity", Float) = 0.6
        _Level         ("Level",          Int)   = 1
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
            float  _FillHeight;
            float  _GlowIntensity;
            int    _Level;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Flask shape (body + bulge + neck + stopper)
                float body    = sdRoundBox(uv - float2(0, -0.05), float2(0.24, 0.26), 0.06);
                float bulge   = sdCircle(uv - float2(0, -0.20), 0.26);
                float neck    = sdRoundBox(uv - float2(0, 0.28), float2(0.10, 0.09), 0.04);
                float stopper = sdBox(uv - float2(0, 0.40), float2(0.12, 0.04));
                float flask   = opUnion(opUnion(body, bulge), opUnion(neck, stopper));

                float alpha = sdfAlpha(flask, 0.015) * _Color.a;
                clip(alpha - 0.01);

                // Liquid fill: animated wave surface
                float wave = sin(uv.x * 12.0 + _GameTime * 4.0) * 0.015;
                float fillY = -0.44 + _FillHeight * 0.82; // map fill to flask coordinates
                float inLiquid = step(uv.y, fillY + wave) * step(flask, 0.0);

                // Colour: liquid bright at top, deeper below
                float depthGrad = clamp((fillY - uv.y) / 0.4, 0.0, 1.0);
                fixed3 liquidCol = lerp(_Color.rgb * 1.2, _Color.rgb * 0.6, depthGrad);

                // Glass / flask body colour
                fixed3 glassCol = fixed3(0.78, 0.82, 0.90);

                // Bubbles inside liquid
                fixed3 bubbleContrib = fixed3(0, 0, 0);
                [unroll]
                for (int b = 0; b < 4; b++)
                {
                    float bSpeed = 0.18 + hash21(float2(b, 1.5)) * 0.10;
                    float bX = (hash21(float2(b, 3.7)) - 0.5) * 0.30;
                    float bY = fmod(_GameTime * bSpeed + hash21(float2(b, 9.1)), 0.70) * _FillHeight - 0.40;
                    float bD = sdCircle(uv - float2(bX, bY), 0.014 + hash21(float2(b,5)) * 0.010);
                    float bA = sdfAlpha(bD, 0.006) * inLiquid;
                    bubbleContrib += fixed3(0.8, 0.9, 1.0) * bA * 0.5;
                }

                // Specular highlight on glass (left fake reflection)
                float specU = uv.x + 0.14;
                float specV = uv.y - 0.05;
                float specD = sdRoundBox(float2(specU, specV), float2(0.018, 0.12), 0.010);
                float specA = sdfAlpha(specD, 0.008) * step(flask, 0.0) * 0.35;

                // Compose
                fixed3 col = lerp(glassCol * 0.88, liquidCol + bubbleContrib, inLiquid);
                col += fixed3(1,1,1) * specA;

                // Stopper darker
                float onStopper = sdfAlpha(stopper, 0.005);
                col = lerp(col, _Color.rgb * 0.5, onStopper);

                // Level dots below flask
                [unroll]
                for (int l = 0; l < 3; l++)
                {
                    float2 dotPos = float2((l - 1) * 0.14, -0.48);
                    float dotD = sdCircle(uv - dotPos, 0.028);
                    fixed3 dotCol = (l < _Level) ? _Color.rgb : fixed3(0.3, 0.3, 0.3);
                    float dotA = sdfAlpha(dotD, 0.007);
                    col = lerp(col, dotCol, dotA);
                    alpha = max(alpha, dotA);
                }

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
