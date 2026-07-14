Shader "SimpleSurvival/TopDownSplatGround"
{
    Properties
    {
        _Control  ("Splatmap 1 (RGBA = layer 0..3)", 2D) = "red" {}
        _Control2 ("Splatmap 2 (RGBA = layer 4..7)", 2D) = "black" {}

        _Splat0 ("Layer 0", 2D) = "white" {}
        _Splat1 ("Layer 1", 2D) = "white" {}
        _Splat2 ("Layer 2", 2D) = "white" {}
        _Splat3 ("Layer 3", 2D) = "white" {}
        _Splat4 ("Layer 4", 2D) = "white" {}
        _Splat5 ("Layer 5", 2D) = "white" {}
        _Splat6 ("Layer 6", 2D) = "white" {}
        _Splat7 ("Layer 7", 2D) = "white" {}

        _Tile0 ("Tile 0 (m)", Float) = 2
        _Tile1 ("Tile 1 (m)", Float) = 2
        _Tile2 ("Tile 2 (m)", Float) = 3
        _Tile3 ("Tile 3 (m)", Float) = 3
        _Tile4 ("Tile 4 (m)", Float) = 2
        _Tile5 ("Tile 5 (m)", Float) = 2
        _Tile6 ("Tile 6 (m)", Float) = 3
        _Tile7 ("Tile 7 (m)", Float) = 3

        _TerrainOrigin ("Terrain Origin (x,_,z)", Vector) = (0,0,0,0)
        _TerrainSize   ("Terrain Size (x,_,z)",   Vector) = (60,0,60,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Control);  SAMPLER(sampler_Control);
            TEXTURE2D(_Control2);
            TEXTURE2D(_Splat0);   SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1);
            TEXTURE2D(_Splat2);
            TEXTURE2D(_Splat3);
            TEXTURE2D(_Splat4);
            TEXTURE2D(_Splat5);
            TEXTURE2D(_Splat6);
            TEXTURE2D(_Splat7);

            CBUFFER_START(UnityPerMaterial)
                float _Tile0, _Tile1, _Tile2, _Tile3;
                float _Tile4, _Tile5, _Tile6, _Tile7;
                float4 _TerrainOrigin;
                float4 _TerrainSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float2 groundXZ    : TEXCOORD1;
                float  fogCoord    : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogCoord    = ComputeFogFactor(p.positionCS.z);
                OUT.groundXZ    = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 cuv = (IN.groundXZ - _TerrainOrigin.xz) / _TerrainSize.xz;
                half4 ctrl1 = SAMPLE_TEXTURE2D(_Control,  sampler_Control, cuv);
                half4 ctrl2 = SAMPLE_TEXTURE2D(_Control2, sampler_Control, cuv);

                half3 c0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, IN.groundXZ / _Tile0).rgb;
                half3 c1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, IN.groundXZ / _Tile1).rgb;
                half3 c2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, IN.groundXZ / _Tile2).rgb;
                half3 c3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, IN.groundXZ / _Tile3).rgb;
                half3 c4 = SAMPLE_TEXTURE2D(_Splat4, sampler_Splat0, IN.groundXZ / _Tile4).rgb;
                half3 c5 = SAMPLE_TEXTURE2D(_Splat5, sampler_Splat0, IN.groundXZ / _Tile5).rgb;
                half3 c6 = SAMPLE_TEXTURE2D(_Splat6, sampler_Splat0, IN.groundXZ / _Tile6).rgb;
                half3 c7 = SAMPLE_TEXTURE2D(_Splat7, sampler_Splat0, IN.groundXZ / _Tile7).rgb;

                half total = ctrl1.r + ctrl1.g + ctrl1.b + ctrl1.a +
                             ctrl2.r + ctrl2.g + ctrl2.b + ctrl2.a + 1e-4h;

                half3 albedo = (c0 * ctrl1.r + c1 * ctrl1.g + c2 * ctrl1.b + c3 * ctrl1.a +
                                c4 * ctrl2.r + c5 * ctrl2.g + c6 * ctrl2.b + c7 * ctrl2.a) / total;

                Light ml = GetMainLight();
                half3 n = normalize(IN.normalWS);
                half ndotl = saturate(dot(n, ml.direction));
                half3 lighting = ml.color * ndotl + SampleSH(n);

                half3 col = albedo * lighting;
                col = MixFog(col, IN.fogCoord);
                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}