Shader "Custom/CircleFillFromCenter"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (0, 0.5, 1, 1)
        _Progress("Progress", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.001, 0.1)) = 0.01
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            half4 _BaseColor;
            float _Progress;
            float _Smoothness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Chuyển đổi không gian tọa độ từ Object sang Clip Space
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Đưa tâm UV (0.5, 0.5) về gốc tọa độ (0,0)
                float2 centerUV = input.uv - float2(0.5, 0.5);
                
                // 2. Tính khoảng cách từ tâm ra rìa (nhân 2 để bán kính tối đa tại góc là ~1.4, cạnh là 1.0)
                // Hoặc dùng độ dài vector để tính bán kính chuẩn của hình tròn nội tiếp (R tối đa = 0.5 * 2 = 1.0)
                float dist = length(centerUV) * 2.0;

                // 3. Sử dụng smoothstep để tạo hiệu ứng fill mượt mà dựa trên _Progress
                // Khi dist < (_Progress - _Smoothness) -> trả về 1 (fill đầy)
                // Khi dist > _Progress -> trả về 0 (trong suốt)
                float alphaMask = 1.0 - smoothstep(_Progress - _Smoothness, _Progress, dist);

                // 4. Trả về màu sắc kết hợp với mặt nạ độ trong suốt (Alpha)
                half4 finalColor = _BaseColor;
                finalColor.a *= alphaMask;

                return finalColor;
            }
            ENDHLSL
        }
    }
}