Shader "SimpleSurvival/Water"
{
    Properties
    {
        _ShallowColor   ("Shallow Color", Color) = (0.5, 0.85, 0.9, 0.3)
        _DeepColor      ("Deep Color", Color) = (0.05, 0.2, 0.25, 0.95)
        _DepthRange     ("Color Depth Range (m)", Range(0.1, 20)) = 3.0
        _AlphaRange     ("Alpha Depth Range (m)", Range(0.1, 20)) = 1.5

        _NormalA        ("Normal Map A", 2D) = "bump" {}
        _NormalB        ("Normal Map B", 2D) = "bump" {}
        _NormalScrollA  ("Scroll A (xy)", Vector) = (0.03, 0.02, 0, 0)
        _NormalScrollB  ("Scroll B (xy)", Vector) = (-0.02, 0.04, 0, 0)
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.5

        _FoamColor      ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamWidth      ("Foam Width (m)", Range(0, 3)) = 0.4
        _FoamNoise      ("Foam Noise", 2D) = "white" {}
        _FoamScroll     ("Foam Scroll", Vector) = (0.02, 0.01, 0, 0)
        _FoamCutoff     ("Foam Cutoff", Range(0, 1)) = 0.5

        _SpecColor      ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess      ("Shininess", Range(8, 256)) = 64
        _SpecStrength   ("Specular Strength", Range(0, 4)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalA_ST;
                float4 _NormalB_ST;
                float4 _FoamNoise_ST;
                half4  _ShallowColor;
                half4  _DeepColor;
                half4  _FoamColor;
                half4  _SpecColor;
                half4  _NormalScrollA;
                half4  _NormalScrollB;
                half4  _FoamScroll;
                half   _DepthRange;
                half   _AlphaRange;
                half   _NormalStrength;
                half   _FoamWidth;
                half   _FoamCutoff;
                half   _Shininess;
                half   _SpecStrength;
            CBUFFER_END

            TEXTURE2D(_NormalA);   SAMPLER(sampler_NormalA);
            TEXTURE2D(_NormalB);   SAMPLER(sampler_NormalB);
            TEXTURE2D(_FoamNoise); SAMPLER(sampler_FoamNoise);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
                half3  normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posIn.positionCS;
                OUT.positionWS = posIn.positionWS;
                OUT.screenPos  = ComputeScreenPos(posIn.positionCS);
                OUT.normalWS   = nrmIn.normalWS;
                OUT.fogFactor  = ComputeFogFactor(posIn.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                float2 worldUV = IN.positionWS.xz;

                float2 uvA = worldUV * _NormalA_ST.xy + _NormalA_ST.zw + _Time.y * _NormalScrollA.xy;
                float2 uvB = worldUV * _NormalB_ST.xy + _NormalB_ST.zw + _Time.y * _NormalScrollB.xy;

                half3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uvA));
                half3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalB, sampler_NormalB, uvB));
                half3 nLocal = normalize(half3((nA.xy + nB.xy) * _NormalStrength, 1.0));

                half3 N = normalize(half3(IN.normalWS.x + nLocal.x, IN.normalWS.y, IN.normalWS.z + nLocal.y));
                half3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float  rawDepth = SampleSceneDepth(screenUV);
                float  sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float  waterEye = IN.screenPos.w;
                float  depthDiff = max(0, sceneEye - waterEye);

                half  depthFade = saturate(depthDiff / _DepthRange);
                half  alphaFade = saturate(depthDiff / _AlphaRange);
                half4 waterCol  = lerp(_ShallowColor, _DeepColor, depthFade);

                float2 foamUV  = worldUV * _FoamNoise_ST.xy + _FoamNoise_ST.zw + _Time.y * _FoamScroll.xy;
                half   foamNoise = SAMPLE_TEXTURE2D(_FoamNoise, sampler_FoamNoise, foamUV).r;
                half   foamMask  = 1.0 - saturate(depthDiff / _FoamWidth);
                half   foam      = step(_FoamCutoff, foamMask * foamNoise) * foamMask;

                Light mainLight = GetMainLight();
                half3 H = normalize(mainLight.direction + V);
                half  spec = pow(saturate(dot(N, H)), _Shininess) * _SpecStrength;
                half3 specular = mainLight.color * _SpecColor.rgb * spec;

                half3 rgb   = waterCol.rgb + specular;
                rgb = lerp(rgb, _FoamColor.rgb, foam);
                half  alpha = lerp(waterCol.a, 1.0, foam);
                alpha = max(alpha, alphaFade * waterCol.a);

                rgb = MixFog(rgb, IN.fogFactor);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
