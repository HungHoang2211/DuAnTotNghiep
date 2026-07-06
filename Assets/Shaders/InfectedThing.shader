Shader "SimpleSurvival/InfectedThing"
{
    Properties
    {
        _Color   ("Tint Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Main Texture", 2D) = "white" {}
        _AlphaR  ("Alpha Texture (R)", 2D) = "white" {}
        _Cutoff  ("Alpha Cutoff", Range(0,1)) = 0.5

        _Settings    ("Breathing (Speed, Amplitude, Frequency)", Vector) = (1.0, 0.02, 2.0, 0)
        _BulgeHeight ("Bulge Height (m)", Float) = 0.7344

        [Toggle(USE_UV_SHAKE)] _UseUVShake ("Use UV to shake?", Float) = 0
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _AlphaR_ST;
            half4  _Color;
            half4  _Settings;
            half   _Cutoff;
            half   _BulgeHeight;
        CBUFFER_END

        TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaR);   SAMPLER(sampler_AlphaR);

        float3 ApplyBreathing(float3 positionOS, float3 normalOS, float2 uv)
        {
            half heightMask = saturate(positionOS.y / max(_BulgeHeight, 0.001h));

            half speed     = _Settings.x;
            half amplitude = _Settings.y;
            half frequency = _Settings.z;

            #ifdef USE_UV_SHAKE
                half phase = frequency * (uv.x + uv.y) + speed * _Time.y;
            #else
                half phase = frequency * (positionOS.x + positionOS.z) + speed * _Time.y;
            #endif

            half wave = sin(phase);
            return positionOS + normalOS * (wave * amplitude * heightMask);
        }
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
            #pragma shader_feature_local USE_UV_SHAKE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uvMain      : TEXCOORD0;
                float2 uvAlpha     : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = ApplyBreathing(IN.positionOS.xyz, IN.normalOS, IN.uv);

                VertexPositionInputs posIn = GetVertexPositionInputs(posOS);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = posIn.positionCS;
                OUT.uvMain      = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvAlpha     = TRANSFORM_TEX(IN.uv, _AlphaR);
                OUT.normalWS    = nrmIn.normalWS;
                OUT.shadowCoord = TransformWorldToShadowCoord(posIn.positionWS);
                OUT.fogFactor   = ComputeFogFactor(posIn.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                half mask = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uvAlpha).r;
                clip(mask - _Cutoff);

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMain).rgb * _Color.rgb;

                half3 N = normalize(IN.normalWS);
                Light mainLight = GetMainLight(IN.shadowCoord);

                half ndotl = saturate(dot(N, mainLight.direction));
                half3 diffuse = mainLight.color * ndotl * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(N);

                half3 color = albedo * (diffuse + ambient);
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
            #pragma shader_feature_local USE_UV_SHAKE

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
                float2 uvAlpha    : TEXCOORD0;
            };

            VaryingsS shadowVert(AttributesS IN)
            {
                VaryingsS OUT;

                float3 posOS = ApplyBreathing(IN.positionOS.xyz, IN.normalOS, IN.uv);
                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                OUT.uvAlpha = TRANSFORM_TEX(IN.uv, _AlphaR);
                return OUT;
            }

            half4 shadowFrag(VaryingsS IN) : SV_TARGET
            {
                half mask = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uvAlpha).r;
                clip(mask - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
