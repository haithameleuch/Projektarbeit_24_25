Shader "Custom/FogShader"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}

        _Color ("Fog Color", Color) = (1, 1, 1, 1)
        _Alpha ("Transparency", Range(0,1)) = 0.5

        _Speed1 ("Scroll Speed Layer 1", Float) = 0.2
        _Speed2 ("Scroll Speed Layer 2", Float) = 0.1
        _Scale2 ("Layer 2 UV Scale", Float) = 2.0

        _PulseSpeed1 ("Pulse Speed Layer 1", Float) = 2.0
        _PulseSpeed2 ("Pulse Speed Layer 2", Float) = 3.1

        _EmissiveMin ("Emission Min", Float) = 2.0
        _EmissiveMax ("Emission Max", Float) = 4.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float _Alpha;

            float _Speed1;
            float _Speed2;
            float _Scale2;

            float _PulseSpeed1;
            float _PulseSpeed2;

            float _EmissiveMin;
            float _EmissiveMax;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv1 = input.uv;
                float2 uv2 = input.uv * _Scale2;

                // Vertical scroll
                uv1.y += _Time.y * _Speed1;
                uv2.y += _Time.y * _Speed2;

                // Sample noise layers
                float n1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1).r;
                float n2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv2).r;

                float noise = (n1 + n2 * 0.6) / 1.6;
                float alpha = noise * _Alpha;

                // Independent pulsing speeds
                float pulse1 = lerp(_EmissiveMin, _EmissiveMax, 0.5 + 0.5 * sin(_Time.y * _PulseSpeed1));
                float pulse2 = lerp(_EmissiveMin, _EmissiveMax, 0.5 + 0.5 * sin(_Time.y * _PulseSpeed2));
                float pulse = (pulse1 + pulse2 * 0.6) / 1.6;

                float3 emissiveColor = _Color.rgb * noise * pulse;

                return float4(emissiveColor, alpha);
            }
            ENDHLSL
        }
    }
}