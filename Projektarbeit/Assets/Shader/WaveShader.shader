Shader "Unlit/Wave"
{
    Properties
    {
        _ColorA ("Color A", Color) = (1, 1, 1, 1)
        _ColorB ("Color B", Color) = (1, 1, 1, 1)
        _WaveAmp ("Wave Amplitude", Range(0, 0.2)) = 0.1
        _WaveSpeed ("Wave Speed", Range(0, 0.2)) = 0.1
        _LineColor ("Line Color", Color) = (0, 0, 0, 1)
        _LineWidth ("Line Width", Range(0.001, 0.05)) = 0.01
        _GridSize ("Grid Size", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            #define TAU 6.28318530718

            float4 _ColorA;
            float4 _ColorB;
            float _WaveAmp;
            float _WaveSpeed;
            float4 _LineColor;
            float _LineWidth;
            float _GridSize;

            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normals : NORMAL;
                float4 uv0 : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float wave_combined : TEXCOORD2;
            };

            Interpolators vert (MeshData v)
            {
                Interpolators o;

                // Wave movement in X and Y directions based on UV
                float wave_y = cos((v.uv0.y - _Time.y * _WaveSpeed) * TAU * 5);
                float wave_x = cos((v.uv0.x - _Time.y * _WaveSpeed) * TAU * 5);

                float combined = wave_x * wave_y;

                // Apply vertex displacement
                v.vertex.y += combined * _WaveAmp;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normals);
                o.uv = v.uv0;
                o.wave_combined = combined * 0.5 + 0.5; // Normalize to [0,1]
                return o;
            }

            float4 frag (Interpolators i) : SV_Target
            {
                // Base color based on wave height
                float t = saturate(i.wave_combined);
                float4 baseCol = lerp(_ColorA, _ColorB, t);

                // Wireframe-like grid effect using UVs
                float2 grid = frac(i.uv * _GridSize);
                float h_line = step(grid.x, _LineWidth);
                float v_line = step(grid.y, _LineWidth);
                float line_mask = saturate(h_line + v_line); // Clamp to [0,1]

                // Blend grid color over base color
                float4 finalCol = lerp(baseCol, _LineColor, line_mask);
                return finalCol;
            }
            ENDCG
        }
    }
}