Shader "Custom/TopDownSplatGround"
{
    Properties
    {
        _Control ("Splatmap (RGBA)", 2D) = "red" {}
        _Splat0 ("Layer 0", 2D) = "white" {}
        _Splat1 ("Layer 1", 2D) = "white" {}
        _Splat2 ("Layer 2", 2D) = "white" {}
        _Splat3 ("Layer 3", 2D) = "white" {}
        _Tile0 ("Tile 0 (m)", Float) = 2
        _Tile1 ("Tile 1 (m)", Float) = 2
        _Tile2 ("Tile 2 (m)", Float) = 3
        _Tile3 ("Tile 3 (m)", Float) = 3
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

            TEXTURE2D(_Control); SAMPLER(sampler_Control);
            TEXTURE2D(_Splat0);  SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1);
            TEXTURE2D(_Splat2);
            TEXTURE2D(_Splat3);

            CBUFFER_START(UnityPerMaterial)
                float _Tile0, _Tile1, _Tile2, _Tile3;
                float4 _TerrainOrigin;
                float4 _TerrainSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float  fogCoord    : TEXCOORD2;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogCoord    = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Tọa độ tương đối so với vị trí plane (object space XZ)
                // -> Texture & splatmap "dính" vào plane, dời plane thì cảnh đi theo.
                float3 planeWS = mul(UNITY_MATRIX_M, float4(0,0,0,1)).xyz;
                float2 localXZ = IN.positionWS.xz - planeWS.xz;

                // UV splatmap: 0..1 trải đều toàn map
                float2 cuv = (localXZ - _TerrainOrigin.xz) / _TerrainSize.xz;
                half4 ctrl = SAMPLE_TEXTURE2D(_Control, sampler_Control, cuv);

                // UV tiling theo local -> tile cũng đi theo plane
                half3 c0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, localXZ / _Tile0).rgb;
                half3 c1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, localXZ / _Tile1).rgb;
                half3 c2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, localXZ / _Tile2).rgb;
                half3 c3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, localXZ / _Tile3).rgb;

                half total = ctrl.r + ctrl.g + ctrl.b + ctrl.a + 1e-4h;
                half3 albedo = (c0*ctrl.r + c1*ctrl.g + c2*ctrl.b + c3*ctrl.a) / total;

                // Lighting đơn giản: main light + ambient (SH)
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