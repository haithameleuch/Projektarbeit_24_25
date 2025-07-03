Shader "Custom/FogShader"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Color ("Fog Color", Color) = (1, 1, 1, 1)
        _Alpha ("Transparency", Range(0,1)) = 0.5
        _Speed ("Scroll Speed", Float) = 0.2
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
            float _Speed;

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
                float2 uv = input.uv;
                uv.y += _Time.y * _Speed;

                float noise = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;
                float alpha = noise * _Alpha;

                return float4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}