Shader "NibiruXR/FlipY"
{
	Properties
	{
		[KeywordEnum(Left, Right)] _EyeType("Eye", int) = 1
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			int _EyeType;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				// -1,1 => -1,0
				if (_EyeType == 0) {
					o.vertex.x = o.vertex.x * 0.5f - 0.5f;
				}
				// -1,1 => 0,1
				if (_EyeType == 1) {
					o.vertex.x = o.vertex.x * 0.5f + 0.5f;
				}

				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_Target
			{
				i.uv.x = i.uv.x;
			    if (_EyeType == 0) {
					i.uv.x = i.uv.x * 0.5f;
				} 

				if (_EyeType == 1) {
					i.uv.x = i.uv.x * 0.5f + 0.5f;
				}
				i.uv.y = 1.0 - i.uv.y;
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
