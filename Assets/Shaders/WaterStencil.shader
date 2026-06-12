Shader "SimpleSurvival/WaterStencil"
{
    Properties
    {
        _TintColor       ("Tint Color", Color) = (1,1,1,1)
        _Layer1          ("Layer 1 (Shallow)", 2D) = "white" {}
        _LayerWet        ("Layer Wet (Deep)", 2D) = "white" {}
        _LayerMix        ("Layer Mix (Noise)", 2D) = "gray" {}
        _DistortionMap   ("Distortion Map (OffsetX = Speed)", 2D) = "black" {}
        _DistortionPower ("Distortion Power", Range(0,1)) = 0.5

        [HideInInspector] _Stencil    ("Stencil Ref", Float) = 1
        [HideInInspector] _StencilOp  ("Stencil Op", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Stencil
        {
            Ref      [_Stencil]
            Comp     Always
            Pass     [_StencilOp]
        }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Layer1_ST;
                float4 _LayerWet_ST;
                float4 _LayerMix_ST;
                float4 _DistortionMap_ST;
                half4  _TintColor;
                half   _DistortionPower;
            CBUFFER_END

            TEXTURE2D(_Layer1);        SAMPLER(sampler_Layer1);
            TEXTURE2D(_LayerWet);      SAMPLER(sampler_LayerWet);
            TEXTURE2D(_LayerMix);      SAMPLER(sampler_LayerMix);
            TEXTURE2D(_DistortionMap); SAMPLER(sampler_DistortionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvWorld    : TEXCOORD0;
                float  fogFactor  : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posIn.positionCS;
                OUT.uvWorld    = posIn.positionWS.xz;
                OUT.fogFactor  = ComputeFogFactor(posIn.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                float speed = _DistortionMap_ST.z;
                float t1 = _Time.y * speed;
                float t2 = _Time.y * speed * 0.85;

                float2 dUv1 = IN.uvWorld * _DistortionMap_ST.xy + float2(t1, t1 * 0.7) + float2(0, _DistortionMap_ST.w);
                float2 dUv2 = IN.uvWorld * _DistortionMap_ST.xy * 1.35 + float2(-t2 * 0.9, t2 * 0.6);

                half2 flow1 = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, dUv1).rg * 2.0 - 1.0;
                half2 flow2 = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, dUv2).rg * 2.0 - 1.0;
                float2 distort = (flow1 + flow2) * 0.5 * _DistortionPower * 0.1;

                float2 uv1   = IN.uvWorld * _Layer1_ST.xy   + _Layer1_ST.zw   + distort;
                float2 uv2   = IN.uvWorld * _LayerWet_ST.xy + _LayerWet_ST.zw + distort * 1.4;
                float2 uvMix = IN.uvWorld * _LayerMix_ST.xy + _LayerMix_ST.zw + distort * 0.5;

                half3 layer1 = SAMPLE_TEXTURE2D(_Layer1, sampler_Layer1, uv1).rgb;
                half3 layerW = SAMPLE_TEXTURE2D(_LayerWet, sampler_LayerWet, uv2).rgb;
                half  mask   = SAMPLE_TEXTURE2D(_LayerMix, sampler_LayerMix, uvMix).r;

                half  depthMix = smoothstep(0.35, 0.75, mask);
                half3 color    = lerp(layer1, layerW, depthMix);

                half  ripple    = saturate(abs(flow1.x + flow2.y) * 0.7);
                half  highlight = pow(ripple, 5.0) * 0.8;
                color += highlight;

                color *= _TintColor.rgb;
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
