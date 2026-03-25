Shader "Mergistry/SH_Zone_Water"
{
    Properties
    {
        _Intensity  ("Intensity",  Float) = 1.0
        _WaveSpeed  ("Wave Speed", Float) = 1.5
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

            float _Intensity;
            float _WaveSpeed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Dual sine wave
                float wave = sin(uv.x * 10.0 + _GameTime * _WaveSpeed)
                           * sin(uv.y * 8.0  + _GameTime * _WaveSpeed * 0.7);

                // Simple caustic: voronoi distance for highlight lines
                float2 voro = voronoi(uv * 4.5 + float2(_GameTime * 0.3, 0));
                float caustic = smoothstep(0.12, 0.0, voro.x) * 0.35;

                float edgeFade = smoothstep(0.0, 0.10, uv.x) * smoothstep(1.0, 0.90, uv.x)
                               * smoothstep(0.0, 0.10, uv.y) * smoothstep(1.0, 0.90, uv.y);

                float alpha = (0.30 + wave * 0.08 + caustic) * _Intensity * edgeFade;

                fixed3 baseCol = fixed3(0.18, 0.62, 0.96);
                fixed3 col = baseCol + fixed3(0.4, 0.5, 0.8) * caustic;

                return fixed4(col, clamp(alpha, 0, 0.70));
            }
            ENDCG
        }
    }
}
