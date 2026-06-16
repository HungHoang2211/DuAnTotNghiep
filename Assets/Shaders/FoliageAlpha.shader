Shader "SimpleSurvival/FoliageAlpha"
{
    Properties
    {
        [MainTexture] _MainTex ("Main Tex", 2D) = "white" {}
        _AlphaR  ("Alpha Texture (R)", 2D) = "white" {}
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _AmbientBoost ("Ambient Floor (0..1)", Range(0,1)) = 0.25

        [HideInInspector] _Stencil     ("Stencil ID", Float) = 1
        [HideInInspector] _StencilComp ("Stencil Comp", Float) = 8
        [HideInInspector] _StencilOp   ("Stencil Op", Float) = 2
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

        Stencil
        {
            Ref      [_Stencil]
            Comp     [_StencilComp]
            Pass     [_StencilOp]
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _AlphaR_ST;
                half   _Dissolve;
                half   _AmbientBoost;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaR);   SAMPLER(sampler_AlphaR);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half3  lighting   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posIn.positionCS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor  = ComputeFogFactor(posIn.positionCS.z);

                half3 N = normalize(nrmIn.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(posIn.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndotl = saturate(dot(N, mainLight.direction));
                half halfL = ndotl * 0.5 + 0.5;
                half3 diffuse = mainLight.color * halfL * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(N);
                OUT.lighting  = diffuse + ambient;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                half alpha = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uv).r;
                clip(alpha - 0.5);

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                half3 lit    = albedo * IN.lighting;
                half3 color  = lerp(lit, albedo, _AmbientBoost);

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0 - _Dissolve);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _AlphaR_ST;
                half   _Dissolve;
                half   _AmbientBoost;
            CBUFFER_END

            TEXTURE2D(_AlphaR);  SAMPLER(sampler_AlphaR);

            float3 _LightDirection;

            struct AttributesS
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct VaryingsS
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            VaryingsS shadowVert(AttributesS IN)
            {
                VaryingsS OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 shadowFrag(VaryingsS IN) : SV_TARGET
            {
                clip(0.5 - _Dissolve);
                half alpha = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uv).r;
                clip(alpha - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
}
