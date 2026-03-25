Shader "Mergistry/SH_MushroomBomb"
{
    Properties
    {
        _BodyColor   ("Body Color",  Color) = (0.95, 0.80, 0.20, 1)
        _TimerNorm   ("Timer Normalized (0=safe,1=boom)", Float) = 0.0
        _FlashAmount ("Hit Flash",   Float) = 0.0
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
            float  _TimerNorm;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Pulse scale increases as timer approaches 0
                float pulseSpeed = lerp(3.0, 14.0, _TimerNorm);
                float scalePulse = 1.0 + sin(_GameTime * pulseSpeed) * 0.04 * _TimerNorm;
                uv /= scalePulse;

                // Cap: semicircle (top half of a circle)
                float cap = sdCircle(uv - float2(0, 0.10), 0.22);
                cap = opIntersection(cap, -uv.y - 0.02); // keep top half (y > -0.02 inside circle)
                // Reframe: cap = union of circle clipped to upper
                float capFull = sdCircle(uv - float2(0, 0.10), 0.22);
                float halfPlane = uv.y + 0.08; // positive above this y
                cap = max(capFull, -halfPlane); // intersection: inside circle AND above line

                // Stem: rounded rectangle
                float stem = sdRoundBox(uv - float2(0, -0.18), float2(0.07, 0.14), 0.02);

                float shape = opUnion(cap, stem);

                float alpha = sdfAlpha(shape, 0.015);
                clip(alpha - 0.01);

                // Colour: lerp yellow → orange → red with timer
                fixed3 safeCol = fixed3(0.95, 0.80, 0.20);
                fixed3 dangerCol = fixed3(0.90, 0.15, 0.10);
                fixed3 bodyCol = lerp(safeCol, dangerCol, _TimerNorm);

                // Spots on cap: 3 fixed positions
                float spot1 = sdCircle(uv - float2( 0.06, 0.22), 0.04);
                float spot2 = sdCircle(uv - float2(-0.08, 0.16), 0.035);
                float spot3 = sdCircle(uv - float2( 0.12, 0.12), 0.03);
                float spots = opUnion(opUnion(spot1, spot2), spot3);
                float spotBlend = sdfAlpha(spots, 0.006) * step(capFull, 0);
                bodyCol = lerp(bodyCol, bodyCol + fixed3(0.2, 0.2, 0.1), spotBlend * 0.5);

                // Hit flash
                bodyCol = lerp(bodyCol, fixed3(1,1,1), _FlashAmount);

                return fixed4(bodyCol, alpha);
            }
            ENDCG
        }
    }
}
