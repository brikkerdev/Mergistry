Shader "Mergistry/SH_Necromancer"
{
    Properties
    {
        _BodyColor       ("Body Color",       Color) = (0.42, 0.11, 0.60, 1)
        _VortexSpeed     ("Vortex Speed",     Float) = 1.5
        _VortexIntensity ("Vortex Intensity", Float) = 0.5
        _ResurrectFlash  ("Resurrect Flash",  Float) = 0.0
        _FlashAmount     ("Hit Flash",        Float) = 0.0
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
            float  _VortexSpeed;
            float  _VortexIntensity;
            float  _ResurrectFlash;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Hood: upward triangle
                float hood = sdTriangle(uv - float2(0, 0.18), 0.18, 0.16);
                // Body: rounded rectangle
                float bodyShape = sdRoundBox(uv - float2(0, -0.08), float2(0.13, 0.20), 0.03);
                // Blend together
                float shape = opSmoothUnion(hood, bodyShape, 0.05);

                float alpha = sdfAlpha(shape, 0.015);
                clip(alpha - 0.01);

                fixed3 bodyCol = _BodyColor.rgb;

                // Rune pattern inside body: scrolling grid of marks
                float runeScroll = _GameTime * 2.5;
                float runeH = step(sin(uv.y * 22.0 + runeScroll), 0.86);
                float runeV = step(sin(uv.x * 18.0), 0.84);
                float runes = runeH * runeV * step(bodyShape, 0.0);
                bodyCol += fixed3(0.55, 0.30, 0.75) * runes * 0.25;

                // Eyes: two bright glowing dots
                float eyeL = sdCircle(uv - float2(-0.045, 0.14), 0.022);
                float eyeR = sdCircle(uv - float2( 0.045, 0.14), 0.022);
                float eyes = opUnion(eyeL, eyeR);
                float eyeBlend = sdfAlpha(eyes, 0.006) * step(shape, 0.0);
                bodyCol = lerp(bodyCol, fixed3(0.9, 0.7, 1.0), eyeBlend);

                // Vortex: rotated gradient noise annular zone around body
                float outerR = length(uv);
                float vortexMask = smoothstep(0.55, 0.35, outerR) * smoothstep(0.28, 0.40, outerR);
                if (vortexMask > 0.01 && _VortexIntensity > 0.0)
                {
                    float2 rotUV = opRotate(uv, _GameTime * _VortexSpeed);
                    float vortexNoise = gradientNoise(rotUV * 4.0);
                    fixed3 vortexCol = hsvToRgb(0.78 + vortexNoise * 0.1, 0.8, 0.9);
                    float vortexA = vortexMask * _VortexIntensity * vortexNoise;
                    bodyCol = lerp(bodyCol, vortexCol, vortexA * 0.6);
                    alpha = max(alpha, vortexA * 0.5);
                }

                // Resurrect flash: green glow burst
                if (_ResurrectFlash > 0.01)
                {
                    fixed3 greenFlash = fixed3(0.2, 1.0, 0.4);
                    bodyCol = lerp(bodyCol, greenFlash, _ResurrectFlash * 0.6);
                    float flashRing = sdRing(uv, _ResurrectFlash * 0.4, 0.015);
                    float fRA = sdfAlpha(flashRing, 0.010) * (1.0 - _ResurrectFlash);
                    bodyCol = lerp(bodyCol, greenFlash, fRA);
                    alpha = max(alpha, fRA);
                }

                bodyCol = lerp(bodyCol, fixed3(1,1,1), _FlashAmount);
                return fixed4(bodyCol, alpha);
            }
            ENDCG
        }
    }
}
