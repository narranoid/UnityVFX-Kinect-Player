Shader "Narranoid/XEF Body Index Convert"
{
	Properties
	{
		_BodyIndexTexture("Body Index Texture", 2D) = "black" {}
		
		_BodyIndexWidth("Body Index Texture Width", Int) = 512
		_BodyIndexHeight("Body Index Texture Height", Int) = 424
		
		_ColorWidth("Color Texture Width", Int) = 1920
		_ColorHeight("Color Texture Height", Int) = 1080
	}

	SubShader
	{
		Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "XEFShaderUtils.cginc"

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

			sampler2D _BodyIndexTexture;
			float4 _BodyIndexTexture_ST;

			int _BodyIndexWidth;
			int _BodyIndexHeight;
			int _ColorWidth;
			int _ColorHeight;


			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _BodyIndexTexture);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				int2 colorRes = uint2(_ColorWidth, _ColorHeight);
				int2 bodyIndexRes = uint2(_BodyIndexWidth, _BodyIndexHeight);
				float2 bodyIndexUV = getSensorUVFromCameraUV(i.uv, colorRes, bodyIndexRes);

				float bodyIndexData = tex2D(_BodyIndexTexture, bodyIndexUV);
				return bodyIndexData;
			}
			ENDCG
		}
	}
}
