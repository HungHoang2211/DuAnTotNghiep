Shader "SimpleSurvival/VertexLit"
{
    Properties
    {
        _Color      ("Tint Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Main Texture", 2D) = "white" {}

        [Toggle(USE_ALPHA_CLIP)] _UseAlphaClip ("Use Alpha Clip", Float) = 0
        _AlphaR     ("Alpha Texture (R)", 2D) = "white" {}
        _Cutoff     ("Alpha Cutoff", Range(0,1)) = 0.5

        [Toggle(EMISSION)] _UseEmission ("Use Emission", Float) = 0
        _EmitTex    ("Emission Texture", 2D) = "black" {}
        _EmitValue  ("Emission Value", Range(0,2)) = 0

        [Toggle(SPECULAR)] _UseSpecular ("Use Specular", Float) = 0
        _SpecColor  ("Specular Color", Color) = (0.4,0.4,0.4,1)
        _Shininess  ("Shininess", Range(0.5,50)) = 8

        [Toggle(DISSOLVE)] _UseDissolve ("Use Dissolve", Float) = 0
        _DissolveNoise ("Dissolve Noise (R)", 2D) = "white" {}
        _Dissolve   ("Dissolve", Range(0,1)) = 0
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _AlphaR_ST;
            float4 _EmitTex_ST;
            float4 _DissolveNoise_ST;
            half4  _Color;
            half4  _SpecColor;
            half   _Cutoff;
            half   _EmitValue;
            half   _Shininess;
            half   _Dissolve;
        CBUFFER_END

        TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaR);         SAMPLER(sampler_AlphaR);
        TEXTURE2D(_EmitTex);        SAMPLER(sampler_EmitTex);
        TEXTURE2D(_DissolveNoise);  SAMPLER(sampler_DissolveNoise);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature_local USE_ALPHA_CLIP
            #pragma shader_feature_local EMISSION
            #pragma shader_feature_local SPECULAR
            #pragma shader_feature_local DISSOLVE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half3  lighting   : TEXCOORD1;
                half3  specular   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
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
                half3 diffuse = mainLight.color * ndotl * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(N);
                OUT.lighting  = diffuse + ambient;

                #ifdef SPECULAR
                    half3 V = normalize(GetWorldSpaceViewDir(posIn.positionWS));
                    half3 H = normalize(mainLight.direction + V);
                    half  spec = pow(saturate(dot(N, H)), _Shininess) * ndotl * mainLight.shadowAttenuation;
                    OUT.specular = mainLight.color * _SpecColor.rgb * spec * IN.color.r;
                #else
                    OUT.specular = half3(0,0,0);
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                #ifdef USE_ALPHA_CLIP
                    half alpha = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uv).r;
                    clip(alpha - _Cutoff);
                #endif

                #ifdef DISSOLVE
                    half noise = SAMPLE_TEXTURE2D(_DissolveNoise, sampler_DissolveNoise, IN.uv).r;
                    clip(noise - _Dissolve);
                #endif

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _Color.rgb;
                half3 color  = albedo * IN.lighting + IN.specular;

                #ifdef EMISSION
                    half3 emit = SAMPLE_TEXTURE2D(_EmitTex, sampler_EmitTex, IN.uv).rgb * _EmitValue;
                    color += emit;
                #endif

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma target 3.0
            #pragma shader_feature_local USE_ALPHA_CLIP
            #pragma shader_feature_local DISSOLVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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
                #ifdef USE_ALPHA_CLIP
                    half alpha = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uv).r;
                    clip(alpha - _Cutoff);
                #endif

                #ifdef DISSOLVE
                    half noise = SAMPLE_TEXTURE2D(_DissolveNoise, sampler_DissolveNoise, IN.uv).r;
                    clip(noise - _Dissolve);
                #endif

                return 0;
            }
            ENDHLSL
        }
    }
}
