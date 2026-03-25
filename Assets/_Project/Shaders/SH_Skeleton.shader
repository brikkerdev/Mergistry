Shader "Mergistry/SH_Skeleton"
{
    Properties
    {
        _Color    ("Body Color", Color) = (0.90, 0.90, 0.90, 1)
        _EyeColor ("Eye Color",  Color) = (0.15, 0.15, 0.20, 1)
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
            fixed4 _EyeColor;

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

                // Body: rectangle
                float body = sdBox(uv - float2(0, -0.06), float2(0.25, 0.26));

                // Head: circle
                float head = sdCircle(uv - float2(0, 0.27), 0.17);

                float shape = opUnion(body, head);

                float alpha = sdfAlpha(shape, 0.015) * _Color.a;
                clip(alpha - 0.01);

                // Eye sockets: two small dark circles
                float eyeL = sdCircle(uv - float2(-0.09, 0.27), 0.05);
                float eyeR = sdCircle(uv - float2( 0.09, 0.27), 0.05);
                float eyes = opUnion(eyeL, eyeR);

                // Blend eyes: if inside eye region, use eye color
                float eyeBlend = sdfAlpha(eyes, 0.008);
                fixed3 col = lerp(_Color.rgb, _EyeColor.rgb, eyeBlend * step(shape, 0));

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
