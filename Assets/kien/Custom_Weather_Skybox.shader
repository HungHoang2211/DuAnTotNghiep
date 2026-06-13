Shader "Custom/Weather_Skybox"
{
    Properties
    {
        // Các biến đặt tên chính xác để Script của bạn có thể SetColor() vào được
        _SkyTint ("Sky Top Color (Trời Đỉnh)", Color) = (0.3, 0.6, 0.9, 1.0)
        _HorizonColor ("Sky Horizon Color (Chân Trời)", Color) = (0.7, 0.8, 0.9, 1.0)
        _GroundColor ("Ground Color (Mặt Đất)", Color) = (0.2, 0.2, 0.2, 1.0)
        
        _Exponent ("Sky Gradient Softness", Range(0.1, 5.0)) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off 
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            uniform float4 _SkyTint;
            uniform float4 _HorizonColor;
            uniform float4 _GroundColor;
            uniform float _Exponent;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Lấy hướng nhìn từ tâm bầu trời ra ngoài
                o.viewDir = v.texcoord; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Chuẩn hóa hướng nhìn góc Y (từ -1.0 đất cho đến +1.0 đỉnh trời)
                float3 d = normalize(i.viewDir);
                float y = d.y;

                fixed4 finalColor = fixed4(0,0,0,1);

                if (y >= 0.0)
                {
                    // Vùng nửa trên bầu trời: Nội suy giữa Chân trời và Đỉnh trời
                    float blend = pow(y, _Exponent);
                    finalColor.rgb = lerp(_HorizonColor.rgb, _SkyTint.rgb, blend);
                }
                else
                {
                    // Vùng nửa dưới: Nội suy từ Chân trời xuống Mặt đất để tạo cảm giác mù (Fog) mềm mại
                    float blend = pow(-y, _Exponent);
                    finalColor.rgb = lerp(_HorizonColor.rgb, _GroundColor.rgb, blend);
                }

                return finalColor;
            }
            ENDCG
        }
    }
    Fallback "Skybox/Procedural"
}