
#ifndef XEF_SHADER_UTILS_INCLUDED
#define XEF_SHADER_UTILS_INCLUDED

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

float2 getSensorUVFromCameraUV(float2 cameraUV, uint2 cameraRes, uint2 sensorRes)
{
	uint x = cameraUV.r * cameraRes.x;
	uint y = cameraUV.g * cameraRes.y;

	// Linear regression model deltas
	float delta1 = -0.02814 * x - 0.00704 * y + 298.656;
	float delta2 = -0.00190 * x + 0.00971 * y + 26.472;

	// Fixed value deltas
	//float delta1 = 270.0;
	//float delta2 = 30.0;

	float x2 = (x - delta1) / 3.0;
	float y2 = (y / 3.0) + delta2;
	return float2(x2 / sensorRes.x, y2 / sensorRes.y)
		+ float2(0.043, 0.0); // This offset is required for some reason?
}

float getDepthInMeters(float depthIn)
{
	// 1) Convert 8000 ushort to 1.0
	float depthOut = depthIn * 8.191875;
	// 8.191875 = 65535 / 8000
	// 65535 = unsigned 16 bit max
	// 8000 = max depth from sensor (in mm)

	// 2) Convert range * 8.0 so we have the value in meters
	return depthOut * 8.0;
}

#endif // XEF_SHADER_UTILS_INCLUDED