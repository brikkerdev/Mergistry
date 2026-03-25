Shader "Mergistry/SH_Ingredient"
{
    Properties
    {
        _Color ("Color", Color) = (0.90, 0.30, 0.20, 1)
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

                float d = sdCircle(uv, 0.44);

                float alpha = sdfAlpha(d, 0.015) * _Color.a;
                clip(alpha - 0.01);

                // Inner highlight
                float highlight = smoothstep(0.15, -0.05, sdCircle(uv - float2(-0.12, 0.14), 0.22));
                fixed3 col = lerp(_Color.rgb, _Color.rgb + fixed3(0.25, 0.25, 0.25), highlight * 0.5);

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
