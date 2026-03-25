Shader "Mergistry/SH_Player"
{
    Properties
    {
        _Color ("Color", Color) = (0.90, 0.85, 0.40, 1)
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
                float2 uv = i.uv - 0.5; // [-0.5, 0.5]

                // Body: circle slightly above center
                float body = sdCircle(uv - float2(0, 0.04), 0.36);

                // Cloak: downward-pointing triangle
                float cloak = sdTriangle(uv - float2(0, -0.12), 0.28, 0.22);

                float d = opUnion(body, cloak);

                float alpha = sdfAlpha(d, 0.015) * _Color.a;
                clip(alpha - 0.01);

                // Slight inner highlight
                float highlight = smoothstep(0.10, -0.10, sdCircle(uv - float2(-0.08, 0.12), 0.20));
                fixed3 col = _Color.rgb + fixed3(0.15, 0.15, 0.08) * highlight;

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
