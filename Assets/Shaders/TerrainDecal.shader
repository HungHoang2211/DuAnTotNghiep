Shader "SimpleSurvival/TerrainDecal"
{
    Properties
    {
        [MainTexture] _MainTex ("Main Texture", 2D) = "white" {}
        _Mask        ("Mask Noise", 2D) = "black" {}
        _Color       ("Tint", Color) = (1,1,1,1)
        _Smooth1     ("Smooth 1 (Edge Soft)", Range(-1,1)) = -0.36
        _Smooth2     ("Smooth 2 (Edge Hard)", Range(-1,1)) = 0.69
        [Toggle(USE_UV_TILING)] _UVTileEnable ("Use UV for Texture", Float) = 0
        [Toggle(USE_UV_MASK)]   _UVMaskTileEnable ("Use UV for Mask", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Geometry-6"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "Decal"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local USE_UV_TILING
            #pragma shader_feature_local USE_UV_MASK
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Mask_ST;
                half4  _Color;
                half   _Smooth1;
                half   _Smooth2;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_Mask);     SAMPLER(sampler_Mask);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMesh     : TEXCOORD0;
                float2 uvWorld    : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posIn = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posIn.positionCS;
                OUT.uvMesh     = IN.uv;
                OUT.uvWorld    = posIn.positionWS.xz;
                OUT.fogFactor  = ComputeFogFactor(posIn.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                #ifdef USE_UV_TILING
                    float2 uvMain = IN.uvMesh * _MainTex_ST.xy + _MainTex_ST.zw;
                #else
                    float2 uvMain = IN.uvWorld * _MainTex_ST.xy + _MainTex_ST.zw;
                #endif

                #ifdef USE_UV_MASK
                    float2 uvMask = IN.uvMesh * _Mask_ST.xy + _Mask_ST.zw;
                #else
                    float2 uvMask = IN.uvWorld * _Mask_ST.xy + _Mask_ST.zw;
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain) * _Color;
                half  noise  = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uvMask).r;
                half  alpha  = smoothstep(_Smooth1, _Smooth2, noise) * albedo.a * _Color.a;

                half3 color = MixFog(albedo.rgb, IN.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
