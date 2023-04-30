Shader "Narranoid/XEF Depth Convert"
{
	Properties
	{
		_DepthTexture("Depth Texture", 2D) = "black" {}
		
		_DepthWidth("Depth Texture Width", Int) = 512
		_DepthHeight("Depth Texture Height", Int) = 424
		
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

			sampler2D _DepthTexture;
			float4 _DepthTexture_ST;

			int _DepthWidth;
			int _DepthHeight;
			int _ColorWidth;
			int _ColorHeight;


			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _DepthTexture);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				int2 colorRes = uint2(_ColorWidth, _ColorHeight);
				int2 depthRes = uint2(_DepthWidth, _DepthHeight);
				float2 depthUV = getSensorUVFromCameraUV(i.uv, colorRes, depthRes);

				float depthData = tex2D(_DepthTexture, depthUV);
				return getDepthInMeters(depthData);
			}
			ENDCG
		}
	}
}
