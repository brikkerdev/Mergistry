Shader "Mergistry/SH_Zone_Ice"
{
    Properties
    {
        _Intensity      ("Intensity",       Float) = 1.0
        _CrackProgress  ("Crack Progress",  Float) = 1.0
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
            float _CrackProgress;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float edgeFade = smoothstep(0.0, 0.10, uv.x) * smoothstep(1.0, 0.90, uv.x)
                               * smoothstep(0.0, 0.10, uv.y) * smoothstep(1.0, 0.90, uv.y);

                // Voronoi for crack pattern and specular
                float2 voro = voronoi(uv * 6.0);
                float edgeDist = voro.x;
                float cellID   = voro.y;

                // Cracks: thin dark lines along voronoi edges
                float crackMask = smoothstep(0.0, 0.05 * _CrackProgress, edgeDist);
                // Animate crack propagation radially from center
                float radial = smoothstep(0.70, 0.0, length(uv - 0.5));
                float crackA = (1.0 - crackMask) * _CrackProgress * radial;

                // Specular highlights on cell faces
                float specular = pow(edgeDist, 2.5) * 0.30;

                float alpha = (0.40 + specular) * _Intensity * edgeFade;

                // Ice colour: white-blue gradient, slight per-cell variation
                fixed3 baseIce = fixed3(0.80, 0.92, 1.00);
                fixed3 deepIce = fixed3(0.50, 0.78, 0.95);
                fixed3 col = lerp(deepIce, baseIce, edgeDist * 2.0);
                col += fixed3(0.8, 0.9, 1.0) * specular;
                // Cracks: dark lines
                col = lerp(col, fixed3(0.12, 0.22, 0.40), crackA * 0.8);

                return fixed4(col, clamp(alpha, 0, 0.65));
            }
            ENDCG
        }
    }
}
