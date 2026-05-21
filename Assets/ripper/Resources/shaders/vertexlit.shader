Shader "RustZ/VertexLit" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_AlphaR ("Alpha Texture", 2D) = "white" {}
		_EmitTex ("Emission Texture", 2D) = "white" {}
		_EmitValue ("Emission Value", Range(0, 1)) = 0
		_Shininess ("Shininess", Range(0.5, 50)) = 8
		_Dissolve ("Dissolve", Range(0, 1)) = 0
		_Outline ("Outline Width", Float) = 1
		_OutlineColor ("Outline Color", Vector) = (0,1,0,1)
		[HideInInspector] _Mode ("__mode", Float) = 0
		[HideInInspector] _SrcBlend ("__src", Float) = 1
		[HideInInspector] _DstBlend ("__dst", Float) = 0
		[HideInInspector] _ZWrite ("__zw", Float) = 1
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector] _StencilComp ("Stencil Comparison", Float) = 0
		[HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
	//CustomEditor "VertexLitGUI"
}