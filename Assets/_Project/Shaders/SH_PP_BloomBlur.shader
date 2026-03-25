Shader "Mergistry/PP/SH_PP_BloomBlur"
{
    Properties
    {
        _MainTex    ("Source",       2D)      = "white" {}
        _TexelSize  ("Texel Size",   Vector)  = (0.001, 0.001, 0, 0)
        _BlurOffset ("Blur Offset",  Float)   = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off

        // Pass 0: Kawase horizontal/vertical (single pass both, use _TexelSize direction)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2    _TexelSize;
            float     _BlurOffset;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 off = _TexelSize * _BlurOffset;

                // Kawase: sample 5 points (centre + 4 diagonal corners)
                fixed4 sum  = tex2D(_MainTex, uv);
                sum += tex2D(_MainTex, uv + float2( off.x,  off.y));
                sum += tex2D(_MainTex, uv + float2(-off.x,  off.y));
                sum += tex2D(_MainTex, uv + float2( off.x, -off.y));
                sum += tex2D(_MainTex, uv + float2(-off.x, -off.y));
                return sum * 0.2;
            }
            ENDCG
        }
    }
}
