Shader "Mergistry/SH_Skeleton"
{
    Properties
    {
        _Color        ("Body Color", Color) = (0.96, 0.90, 0.80, 1)
        _EyeColor     ("Eye Color",  Color) = (0.95, 0.95, 0.40, 1)
        _EyePulsePhase("Eye Pulse Phase", Float) = 0.0
        _FlashAmount  ("Hit Flash",  Float) = 0.0
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
            float  _EyePulsePhase;
            float  _FlashAmount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // Idle bob
                float bob = sin(_GameTime * 1.8 + _EyePulsePhase) * 0.007;
                uv.y -= bob;

                // Body: vertical rounded rectangle
                float body = sdRoundBox(uv - float2(0, -0.06), float2(0.15, 0.24), 0.03);
                // Head: circle
                float head = sdCircle(uv - float2(0, 0.24), 0.13);
                float shape = opUnion(body, head);

                float alpha = sdfAlpha(shape, 0.015) * _Color.a;
                clip(alpha - 0.01);

                // Slight body colour variation via noise
                float boneNoise = valueNoise(uv * 10.0) * 0.04;
                fixed3 col = _Color.rgb + boneNoise;

                // Rib lines: 3 horizontal segments inside torso
                [unroll]
                for (int r = 0; r < 3; r++)
                {
                    float ribY = -0.02 - r * 0.09;
                    float rib = sdSegment(uv, float2(-0.10, ribY), float2(0.10, ribY)) - 0.008;
                    float ribA = sdfAlpha(rib, 0.005) * step(body, 0.0);
                    col = lerp(col, _Color.rgb * 0.60, ribA * 0.8);
                }

                // Eyes: pulsating glow
                float eyePulse = pulse(_GameTime * (2.0 + _EyePulsePhase * 0.3), 1.0, 2.0);
                float eyeL = sdCircle(uv - float2(-0.05, 0.25), 0.028);
                float eyeR = sdCircle(uv - float2( 0.05, 0.25), 0.028);
                float eyes = opUnion(eyeL, eyeR);
                float eyeBlend = sdfAlpha(eyes, 0.007) * step(shape, 0.0);
                col = lerp(col, _EyeColor.rgb * (0.7 + eyePulse * 0.3), eyeBlend);

                col = lerp(col, fixed3(1,1,1), _FlashAmount);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
