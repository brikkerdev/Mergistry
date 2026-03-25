Shader "Mergistry/SH_Player"
{
    Properties
    {
        _Color          ("Cloak Color",          Color) = (0.25, 0.35, 0.55, 1)
        _LastPotionColor("Last Potion Color",    Color) = (0.25, 0.35, 0.55, 0)  // a=0 → no recent throw
        _FlashAmount    ("Hit Flash",            Float) = 0.0
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
            fixed4 _LastPotionColor;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Idle bob
                float bob = sin(_GameTime * 2.2) * 0.008;
                uv.y -= bob;

                // Head: circle slightly above center
                float head = sdCircle(uv - float2(0, 0.20), 0.14);

                // Cloak: downward triangle body
                float cloak = sdTriangle(uv - float2(0, -0.06), 0.28, 0.22);
                // Wavy cloak bottom
                float wavyBottom = uv.y + 0.28 + sin(uv.x * 9.0 + _GameTime * 2.5) * 0.03;
                cloak = opUnion(cloak, -wavyBottom * 10.0); // keep above wave

                float body = opSmoothUnion(head, cloak, 0.04);
                float alpha = sdfAlpha(body, 0.015);
                clip(alpha - 0.01);

                // Flow noise inside cloak
                float2 flowUV = uv * 3.0 + float2(_GameTime * 0.5, _GameTime * 0.3);
                float flowNoise = fbm(flowUV, 2);

                // Blend cloak color with last potion color based on flow noise & potion alpha
                fixed3 cloakCol = lerp(_Color.rgb, _LastPotionColor.rgb, flowNoise * _LastPotionColor.a * 0.7);

                // Head skin tone
                float inHead = step(head, 0.0);
                fixed3 skinCol = fixed3(0.96, 0.86, 0.72);
                fixed3 col = lerp(cloakCol, skinCol, inHead);

                // Eyes
                float eyeL = sdCircle(uv - float2(-0.048, 0.22), 0.022);
                float eyeR = sdCircle(uv - float2( 0.048, 0.22), 0.022);
                float eyes = opUnion(eyeL, eyeR);
                float eyeBlend = sdfAlpha(eyes, 0.006) * step(body, 0.0);
                col = lerp(col, fixed3(0.1, 0.08, 0.12), eyeBlend);

                // Inner highlight on head
                float highlight = smoothstep(0.08, -0.06, sdCircle(uv - float2(-0.05, 0.24), 0.10));
                col += skinCol * highlight * 0.10 * inHead;

                col = lerp(col, fixed3(1,1,1), _FlashAmount);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
