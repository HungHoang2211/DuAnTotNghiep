Shader "SimpleSurvival/CharacterDollLit"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Main Tex", 2D) = "white" {}

        _StudioLightDirection ("Studio Light Direction", Vector) = (0.3, 0.6, 0.7, 0)
        _StudioLightColor ("Studio Light Color", Color) = (1,1,1,1)
        _StudioAmbientColor ("Studio Ambient Color", Color) = (0.35,0.35,0.35,1)

        [Toggle(SPECULAR)] _UseSpecular ("Use Specular (Rim)", Float) = 0
        _Shininess ("Rim Power", Range(0.5,50)) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

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
            #pragma shader_feature_local SPECULAR

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _StudioLightDirection;
                half4 _StudioLightColor;
                half4 _StudioAmbientColor;
                half _Shininess;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 lighting : TEXCOORD1;
                half3 rim : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrmIn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posIn.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                half3 N = normalize(nrmIn.normalWS);
                half3 L = normalize(_StudioLightDirection.xyz);
                half ndotl = saturate(dot(N, L));
                half halfL = ndotl * 0.5 + 0.5;
                half3 diffuse = _StudioLightColor.rgb * halfL;
                OUT.lighting = diffuse + _StudioAmbientColor.rgb;

                #ifdef SPECULAR
                    half3 V = normalize(GetWorldSpaceViewDir(posIn.positionWS));
                    half fresnel = pow(saturate(dot(N, V)), _Shininess);
                    OUT.rim = fresnel * IN.color.r;
                #else
                    OUT.rim = half3(0,0,0);
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _Color.rgb;
                half3 color = albedo * IN.lighting + IN.rim;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}