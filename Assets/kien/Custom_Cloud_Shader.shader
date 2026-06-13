Shader "Custom/Weather_Clouds"
{
    Properties
    {
        // Tên biến bắt buộc là _Color để khớp với thuộc tính "matClouds.color" trong C#
        _Color ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudThreshold ("Cloud Density (Mật độ mây)", Range(0.0, 1.0)) = 0.5
        _CloudScale ("Cloud Size (Kích thước cụm mây)", Range(1.0, 20.0)) = 5.0
        _SpeedX ("Speed X (Tốc độ trôi X)", Float) = 0.02
        _SpeedZ ("Speed Z (Tốc độ trôi Z)", Float) = 0.01
    }
    SubShader
    {
        // Đặt trong hàng đợi Queue Transparent để mây có khoảng trong suốt nhìn xuyên qua được bầu trời
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            float4 _Color;
            float _CloudThreshold;
            float _CloudScale;
            float _SpeedX;
            float _SpeedZ;

            // Hàm tạo nhiễu ngẫu nhiên (Pseudo-noise) để tự sinh hình khối mây
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(hash(i + float2(0.0,0.0)), hash(i + float2(1.0,0.0)), u.x),
                            lerp(hash(i + float2(0.0,1.0)), hash(i + float2(1.0,1.0)), u.x), u.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // Lấy vị trí mây trong không gian thế giới để tính toán hướng trôi bồng bềnh
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Tính toán tọa độ dịch chuyển dựa trên thời gian thực
                float2 skyUV = i.worldPos.xz * (0.01 * _CloudScale);
                skyUV.x += _Time.y * _SpeedX;
                skyUV.y += _Time.y * _SpeedZ;

                // Trộn nhiều tầng nhiễu để mây nhìn tự nhiên hơn
                float n = noise(skyUV) * 0.5 + noise(skyUV * 2.0) * 0.25 + noise(skyUV * 4.0) * 0.125;

                fixed4 col = _Color;
                
                // Thuật toán cắt viền mây (Alpha Clipping mềm) dựa trên mật độ mây cấu hình
                float alpha = smoothstep(_CloudThreshold, _CloudThreshold + 0.1, n);
                col.a *= alpha;

                return col;
            }
            ENDCG
        }
    }
}