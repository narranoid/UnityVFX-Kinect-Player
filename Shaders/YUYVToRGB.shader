Shader "Narranoid/YUYV To RGB"
{
	Properties
	{
		_YUYVTexture("YUYV Texture", 2D) = "white" {}
		_DepthTexture("Depth Texture", 2D) = "black" {}
		_TextureWidth("Texture Width", Int) = 1920
		_TextureHeight("Texture Height", Int) = 1080
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


			sampler2D _YUYVTexture;
			float4 _YUYVTexture_ST;

			sampler2D _DepthTexture;
			float4 _DepthTexture_ST;

			int _TextureWidth;
			int _TextureHeight;


			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _YUYVTexture);
				return o;
			}

			int clip255(int val)
			{
				if (val > 255)
					{ return 255; }
				else if (val < 0)
					{ return   0; }
				return val;
			}

			fixed3 getRGBFromYUYV(fixed4 yuyvData, bool isSecondPixel)
			{
				int y = 255.0 * ((isSecondPixel) ? yuyvData.b : yuyvData.r);
				int u = 255.0 * yuyvData.g;
				int v = 255.0 * yuyvData.a;

				int c = y - 16;
				int d = u - 128;
				int e = v - 128;

				int r = clip255((298 * c + 409 * e + 128) >> 8); // red
				int g = clip255((298 * c - 100 * d - 208 * e + 128) >> 8); // green
				int b = clip255((298 * c + 516 * d + 128) >> 8); // blue
				
				// 1 / 255 = 0.0039215686274509803921568627451
				return fixed3(
					((fixed)r),
					((fixed)g),
					((fixed)b)
				) * 0.0039215686274509803921568627451;
			}

			float2 colorToDepthUV(uint x, uint y)
			{
				// Linear regression model deltas
				float delta1 = -0.02814 * x - 0.00704 * y + 298.656;
				float delta2 = -0.00190 * x + 0.00971 * y + 26.472;

				// Fixed value deltas
				//float delta1 = 270.0;
				//float delta2 = 30.0;

				float x2 = (x - delta1) / 3.0;
				float y2 = (y / 3.0) + delta2;
				return float2(x2 / 512.0, y2 / 424.0)
					+ float2(0.043, 0.0); // This offset is required for some reason?
			}

			float4 frag(v2f i) : SV_Target
			{
				uint x = i.uv.r * _TextureWidth;
				uint y = i.uv.g * _TextureHeight;
				uint px = y * _TextureWidth + x;

				float2 depthUV = colorToDepthUV(x, y);

				fixed4 yuyvData = tex2D(_YUYVTexture, i.uv);
				float2 depthData = tex2D(_DepthTexture, depthUV);
				fixed3 result = getRGBFromYUYV(yuyvData, (px % 2 > 0));

				//return fixed4(result.r, result.g, result.b, depthData.r);
				//return float4(result.r, result.g, result.b, 1.0 - (depthData.r * 32.0));
				return float4(result.r, result.g, result.b, 1.0 - (depthData.r * 32.0));
			}
			ENDCG
		}
	}
}
