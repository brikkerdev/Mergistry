Shader "Mergistry/SH_Brew"
{
    Properties
    {
        _Color ("Color", Color) = (0.90, 0.35, 0.10, 1)
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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Flask body: rounded rectangle
                float body = sdRoundBox(uv - float2(0, -0.05), float2(0.24, 0.26), 0.06);

                // Flask bulge: larger circle at bottom
                float bulge = sdCircle(uv - float2(0, -0.20), 0.26);

                // Neck: narrow rounded box at top
                float neck = sdRoundBox(uv - float2(0, 0.28), float2(0.10, 0.09), 0.04);

                // Stopper: small box at very top
                float stopper = sdBox(uv - float2(0, 0.40), float2(0.12, 0.04));

                float flask = opUnion(opUnion(body, bulge), opUnion(neck, stopper));

                float alpha = sdfAlpha(flask, 0.015) * _Color.a;
                clip(alpha - 0.01);

                // Inner glow / highlight
                float innerGlow = smoothstep(0.10, -0.10, sdCircle(uv - float2(-0.07, 0.05), 0.15));
                fixed3 col = _Color.rgb + fixed3(0.20, 0.20, 0.20) * innerGlow;

                // Darker at stopper
                float onStopper = sdfAlpha(stopper, 0.005);
                col = lerp(col, _Color.rgb * 0.6, onStopper);

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
