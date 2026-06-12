Shader "SimpleSurvival/Blood"
{
    Properties
    {
        _TintColor  ("Tint Color", Color) = (0.5, 0.0, 0.0, 0.5)
        [MainTexture] _MainTex ("Particle Texture", 2D) = "white" {}
        _Detail     ("Detail Noise", 2D) = "gray" {}
        _DetailTile ("Detail Tiling", Float) = 6
        _EdgeMin    ("Edge Min", Range(0,1)) = 0.05
        _EdgeSoft   ("Edge Soft", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Geometry-1"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"    = "Plane"
        }
        LOD 100

        Pass
        {
            Name "Blood"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

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
                half4  _TintColor;
                half   _DetailTile;
                half   _EdgeMin;
                half   _EdgeSoft;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_Detail);   SAMPLER(sampler_Detail);

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
                half4  color      : COLOR;
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
                OUT.color      = IN.color;
                OUT.fogFactor  = ComputeFogFactor(posIn.positionCS.z);

                half3 N = normalize(nrmIn.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(posIn.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndotl = saturate(dot(N, mainLight.direction));
                half3 diffuse = mainLight.color * ndotl * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(N);
                OUT.lighting  = diffuse + ambient;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                half4 baseTex   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half  detail    = SAMPLE_TEXTURE2D(_Detail, sampler_Detail, IN.uv * _DetailTile).r;

                half rawAlpha = baseTex.r * IN.color.a * detail;
                half alpha    = smoothstep(_EdgeMin, _EdgeMin + _EdgeSoft, rawAlpha);

                half3 rgb = _TintColor.rgb * IN.color.rgb * IN.lighting;
                half  finalAlpha = alpha * _TintColor.a;
                rgb += (1.0 - finalAlpha) * finalAlpha * rgb.r;

                rgb = MixFog(rgb, IN.fogFactor);
                return half4(rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
