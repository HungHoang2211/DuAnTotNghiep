Shader "SimpleSurvival/Grass"
{
    Properties
    {
        [MainTexture] _MainTex ("Main Tex", 2D) = "white" {}
        _AlphaR      ("Alpha Texture (R)", 2D) = "white" {}
        _AmbientBoost ("Ambient Floor (0..1)", Range(0,1)) = 0.3

        _Settings    ("Wind (Speed, Amplitude, Frequency)", Vector) = (0.2, 0.1, 0.5, 0)
        _BladeHeight ("Blade Height (m, for auto wind mask)", Float) = 0.3

        [Toggle(USE_BORDER_COLOR)] _UseBorderColor ("Use Border Color", Float) = 0
        _BorderColor ("Border Color", Color) = (0,0,0,1)
        _BorderWidth ("Border Width", Range(0.01, 0.3)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _AlphaR_ST;
            half4  _Settings;
            half4  _BorderColor;
            half   _AmbientBoost;
            half   _BladeHeight;
            half   _BorderWidth;
        CBUFFER_END

        TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaR);   SAMPLER(sampler_AlphaR);

        float3 ApplyWind(float3 positionOS, float3 positionWS, half vertexColorR)
        {
            half autoMask = saturate(positionOS.y / max(_BladeHeight, 0.001h));
            half windMask = autoMask * vertexColorR;

            half speed     = _Settings.x;
            half amplitude = _Settings.y;
            half frequency = _Settings.z;

            half phase = frequency * (positionWS.x + positionWS.z) + speed * _Time.y;
            half2 wave;
            wave.x = sin(phase);
            wave.y = cos(phase * 0.8h);

            positionWS.x += windMask * amplitude * wave.x;
            positionWS.z += windMask * amplitude * wave.y * 0.6h;
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

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
            #pragma shader_feature_local USE_BORDER_COLOR

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
                float2 uvMain     : TEXCOORD0;
                float2 uvAlpha    : TEXCOORD1;
                half3  lighting   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS = ApplyWind(IN.positionOS.xyz, positionWS, IN.color.r);

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uvMain     = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvAlpha    = TRANSFORM_TEX(IN.uv, _AlphaR);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);

                half3 N = normalize(TransformObjectToWorldNormal(IN.normalOS));
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndotl = saturate(dot(N, mainLight.direction));
                half halfL = ndotl * 0.5h + 0.5h;
                half3 diffuse = mainLight.color * halfL * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(N);
                OUT.lighting  = diffuse + ambient;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                half mask = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, IN.uvAlpha).r;
                clip(mask - 0.5h);

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMain).rgb;

                #ifdef USE_BORDER_COLOR
                    half edge = 1.0h - smoothstep(0.5h, 0.5h + _BorderWidth, mask);
                    albedo = lerp(albedo, _BorderColor.rgb, edge * _BorderColor.a);
                #endif

                half3 lit   = albedo * IN.lighting;
                half3 color = lerp(lit, albedo, _AmbientBoost);

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0h);
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

            float3 _LightDirection;

            struct AttributesS
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
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
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS = ApplyWind(IN.positionOS.xyz, positionWS, IN.color.r);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
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
                clip(mask - 0.5h);
                return 0;
            }
            ENDHLSL
        }
    }
}
