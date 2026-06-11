Shader "SimpleSurvival/VertexLit"
{
    Properties
    {
        _Color      ("Tint Color", Color) = (1,1,1,1)
        _MaskTex    ("Dissolve Mask (R)", 2D) = "white" {}
        [MainTexture] _MainTex ("Main Texture", 2D) = "white" {}
        _AlphaR     ("Alpha Texture (R)", 2D) = "white" {}
        _EmitTex    ("Emission Texture", 2D) = "black" {}
        _EmitValue  ("Emission Value", Range(0,1)) = 0
        _Shininess  ("Shininess", Range(0.5,50)) = 8
        _Dissolve   ("Dissolve", Range(0,1)) = 0
        _Cutoff     ("Alpha Cutoff", Range(0,1)) = 0.5
        _SpecColor  ("Specular Color", Color) = (0.3,0.3,0.3,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MaskTex_ST;
                float4 _EmitTex_ST;
                half4  _Color;
                half4  _SpecColor;
                half   _EmitValue;
                half   _Shininess;
                half   _Dissolve;
                half   _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaR);   SAMPLER(sampler_AlphaR);
            TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);
            TEXTURE2D(_EmitTex);  SAMPLER(sampler_EmitTex);

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
                half3  diffuse    : TEXCOORD1;
                half3  specular   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs  posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs    nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posIn.positionCS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor  = ComputeFogFactor(posIn.positionCS.z);

                half3 N = normalize(nrmIn.normalWS);
                half3 V = normalize(GetWorldSpaceViewDir(posIn.positionWS));
                Light L = GetMainLight();

                half  ndotl = saturate(dot(N, L.direction));
                half3 H     = normalize(L.direction + V);
                half  spec  = pow(saturate(dot(N, H)), _Shininess) * ndotl;

                half3 ambient = SampleSH(N);
                OUT.diffuse   = L.color * ndotl + ambient;
                OUT.specular  = L.color * spec * _SpecColor.rgb;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                half  alpha    = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uv).r;
                half  noise    = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).r;
                clip(alpha - _Cutoff);
                clip(noise - _Dissolve);

                half3 albedo   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _Color.rgb;
                half3 emission = SAMPLE_TEXTURE2D(_EmitTex, sampler_EmitTex, IN.uv).rgb * _EmitValue;

                half3 color = albedo * IN.diffuse + IN.specular + emission;
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MaskTex_ST;
                float4 _EmitTex_ST;
                half4  _Color;
                half4  _SpecColor;
                half   _EmitValue;
                half   _Shininess;
                half   _Dissolve;
                half   _Cutoff;
            CBUFFER_END

            TEXTURE2D(_AlphaR);   SAMPLER(sampler_AlphaR);
            TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);

            float3 _LightDirection;

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
            };

            Varyings shadowVert(Attributes IN)
            {
                Varyings OUT;
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

            half4 shadowFrag(Varyings IN) : SV_TARGET
            {
                half a = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uv).r;
                half n = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).r;
                clip(a - _Cutoff);
                clip(n - _Dissolve);
                return 0;
            }
            ENDHLSL
        }
    }
}
