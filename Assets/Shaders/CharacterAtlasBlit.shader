Shader "SimpleSurvival/CharacterAtlasBlit"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "black" {}
        _DetailTex ("Detail", 2D) = "black" {}
        _DetailTiling ("Detail Tiling/Offset", Vector) = (1,1,0,0)
        _TintColor ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);   SAMPLER(sampler_MaskTex);
            TEXTURE2D(_DetailTex); SAMPLER(sampler_DetailTex);
            float4 _DetailTiling;
            half4 _TintColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = float2(IN.uv.x, 1.0 - IN.uv.y);

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv);

                half3 tinted = baseColor.rgb * _TintColor.rgb;
                half3 result = lerp(baseColor.rgb, tinted, mask.g);

                float2 detailUV = frac(uv * _DetailTiling.xy + _DetailTiling.zw);
                half4 detailColor = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, detailUV);
                result = lerp(result, detailColor.rgb, mask.b);

                return half4(result, baseColor.a);
            }
            ENDHLSL
        }
    }
}