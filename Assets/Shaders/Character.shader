Shader "SimpleSurvival/Character"
{
    Properties
    {
        _Color      ("Tint Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Main Tex", 2D) = "white" {}
        _MaskTex    ("Mask (R:Spec G:Rim B:Emit)", 2D) = "black" {}
        _EmitTex    ("Emission Texture", 2D) = "black" {}
        _EmitValue  ("Emission Value", Range(0,2)) = 0

        _RimColor   ("Rim Color (RGB) Power (A)", Color) = (1,1,1,3)
        _RimStrength("Rim Strength", Range(0,2)) = 1

        _EdgeColor  ("XRay Edge Color", Color) = (0.4,0.7,1,1)
        _EdgePower  ("XRay Edge Power", Range(0.1,8)) = 2

        [HideInInspector] _Shininess ("Shininess", Range(0.5,50)) = 16
        _SpecColor  ("Specular Color", Color) = (0.4,0.4,0.4,1)
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
            half4  _Color;
            half4  _SpecColor;
            half4  _RimColor;
            half4  _EdgeColor;
            half   _RimStrength;
            half   _EdgePower;
            half   _EmitValue;
            half   _Shininess;
        CBUFFER_END

        TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
        TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);
        TEXTURE2D(_EmitTex);  SAMPLER(sampler_EmitTex);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZTest LEqual
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half3  normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = posIn.positionCS;
                OUT.positionWS  = posIn.positionWS;
                OUT.normalWS    = nrmIn.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor   = ComputeFogFactor(posIn.positionCS.z);
                OUT.shadowCoord = GetShadowCoord(posIn);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                half3 albedo   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _Color.rgb;
                half3 mask     = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).rgb;
                half3 emission = SAMPLE_TEXTURE2D(_EmitTex, sampler_EmitTex, IN.uv).rgb;

                half3 N = normalize(IN.normalWS);
                half3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 L = mainLight.direction;
                half  shadowAtten = mainLight.shadowAttenuation;

                half  ndotl = saturate(dot(N, L));
                half  halfL = ndotl * 0.5 + 0.5;
                half3 diffuse = albedo * (mainLight.color * halfL * shadowAtten + SampleSH(N));

                half3 H = normalize(L + V);
                half  spec = pow(saturate(dot(N, H)), _Shininess) * ndotl * shadowAtten;
                half3 specular = _SpecColor.rgb * spec * mask.r;

                half  fresnel = pow(1.0 - saturate(dot(N, V)), _RimColor.a);
                half3 rim     = _RimColor.rgb * fresnel * _RimStrength * mask.g;

                half3 emit  = emission * _EmitValue * mask.b;

                half3 color = diffuse + specular + rim + emit;
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "XRay"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZTest Greater
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex xrayVert
            #pragma fragment xrayFrag
            #pragma target 3.0

            struct AttributesX
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsX
            {
                float4 positionCS : SV_POSITION;
                half3  normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            VaryingsX xrayVert(AttributesX IN)
            {
                VaryingsX OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = posIn.positionCS;
                OUT.positionWS = posIn.positionWS;
                OUT.normalWS   = nrmIn.normalWS;
                return OUT;
            }

            half4 xrayFrag(VaryingsX IN) : SV_TARGET
            {
                half3 N = normalize(IN.normalWS);
                half3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                half  fresnel = pow(1.0 - saturate(dot(N, V)), _EdgePower);
                half  alpha   = _EdgeColor.a * lerp(0.35, 1.0, fresnel);
                return half4(_EdgeColor.rgb, alpha);
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

            float3 _LightDirection;

            struct AttributesS
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsS
            {
                float4 positionCS : SV_POSITION;
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
                return OUT;
            }

            half4 shadowFrag(VaryingsS IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
