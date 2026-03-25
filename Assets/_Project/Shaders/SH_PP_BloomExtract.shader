Shader "Mergistry/PP/SH_PP_BloomExtract"
{
    Properties
    {
        _MainTex   ("Source", 2D)    = "white" {}
        _Threshold ("Threshold",  Float) = 0.80
        _SoftKnee  ("Soft Knee",  Float) = 0.50
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float     _Threshold;
            float     _SoftKnee;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col  = tex2D(_MainTex, i.uv);
                float  br   = max(col.r, max(col.g, col.b));
                float  contrib = smoothstep(_Threshold - _SoftKnee, _Threshold + _SoftKnee, br);
                return fixed4(col.rgb * contrib, 1.0);
            }
            ENDCG
        }
    }
}
