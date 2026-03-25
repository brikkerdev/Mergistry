Shader "Mergistry/PP/SH_PP_BloomCombine"
{
    Properties
    {
        _MainTex       ("Scene",          2D)    = "white" {}
        _BloomTex      ("Bloom Blurred",  2D)    = "black" {}
        _BloomIntensity("Bloom Intensity",Float) = 1.5
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
            sampler2D _BloomTex;
            float     _BloomIntensity;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 scene = tex2D(_MainTex, i.uv);
                fixed4 bloom = tex2D(_BloomTex, i.uv);
                fixed3 combined = scene.rgb + bloom.rgb * _BloomIntensity;
                // Tone-map to prevent over-saturation (simple Reinhard)
                combined = combined / (combined + 1.0);
                return fixed4(combined, scene.a);
            }
            ENDCG
        }
    }
}
