Shader "SimpleSurvival/BuildGrid"
{
    Properties
    {
        _GridTex ("Grid Color (RGB)", 2D) = "white" {}
        _GridAlpha ("Grid Alpha (mask)", 2D) = "white" {}
        _CellSize ("Cell Size (unit)", Float) = 2
        _Color ("Tint", Color) = (0.3, 1, 0.45, 1)
        _Intensity ("Intensity", Range(0,3)) = 1
        _GridOrigin ("Grid Origin (x,_,z)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_GridTex);   SAMPLER(sampler_GridTex);   // foundation_grid (màu)
            TEXTURE2D(_GridAlpha); SAMPLER(sampler_GridAlpha); // foundation_grid_Alpha (mặt nạ)

            CBUFFER_START(UnityPerMaterial)
                float  _CellSize;
                float4 _Color;
                float  _Intensity;
                float4 _GridOrigin;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 1 lần lặp = 1 ô = _CellSize unit, canh theo world
                float2 uv = (IN.positionWS.xz - _GridOrigin.xz) / _CellSize;

                half3 gridColor = SAMPLE_TEXTURE2D(_GridTex,   sampler_GridTex,   uv).rgb;
                half  mask      = SAMPLE_TEXTURE2D(_GridAlpha, sampler_GridAlpha, uv).r;

                half3 col   = gridColor * _Color.rgb * _Intensity;
                half  alpha = mask * _Color.a;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}