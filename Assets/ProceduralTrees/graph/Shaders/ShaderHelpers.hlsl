float smoothstep(float a,float b,float alpha)
{
    float t = alpha * alpha * (3.0 - 2.0 * alpha);
    return lerp(a,b,t);
}

float interpolate_noise(float a,float b,float alpha) //the function used by the 3D noise computation
{
    return smoothstep(a,b,alpha);
}

float LinearDepthToRawDepth(float linearDepth)
{
    return (1.0f - (linearDepth * _ZBufferParams.y)) / (linearDepth * _ZBufferParams.x);
}

float aces(float v)
{
    // Apply tonemapping curve
    // Narkowicz 2016, "ACES Filmic Tone Mapping Curve"
    // https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return saturate((v * (a * v + b)) / (v * (c * v + d) + e));
}

float3 aces(float3 v)
{
    return float3(aces(v.x),aces(v.y),aces(v.z));
}

//noise functions
float3 hash3( float3 p ) // replace this by something better
{
	p = float3( dot(p,float3(127.1,311.7, 74.7)),
		  dot(p,float3(269.5,183.3,246.1)),
		  dot(p,float3(113.5,271.9,124.6)));

	return -1.0 + 2.0*frac(sin(p)*43758.5453123);
}

float hash(float3 p)
{
    return frac( -1.0 + 2.0*frac(sin(dot(p,float3(127.1,311.7, 74.7)))*43758.5453123));
}

float noise(float3 p)
{
    float3 alpha = frac(p);
    float3 pMin = trunc(p);
    float3 pMax = pMin + float3(1,1,1);
    float a = hash(float3(pMin.x,pMax.y,pMin.z));
    float b = hash(float3(pMin.x,pMax.y,pMax.z));
    float c = hash(pMax);
    float d = hash(float3(pMax.x,pMax.y,pMin.z));
    
    float e = hash(pMin);
    float f = hash(float3(pMin.x,pMin.y,pMax.z));
    float g = hash(float3(pMax.x,pMin.y,pMax.z));
    float h = hash(float3(pMax.x,pMin.y,pMin.z));

    float eh = interpolate_noise(e,h,alpha.x);
    float fg = interpolate_noise(f,g,alpha.x);
    float efgh = interpolate_noise(eh,fg,alpha.z);

    float ad = interpolate_noise(a,d,alpha.x);
    float bc = interpolate_noise(b,c,alpha.x);
    float abcd = interpolate_noise(ad,bc,alpha.z);

    return interpolate_noise(efgh,abcd,alpha.y);
}

float fractal_noise(float3 p,int levels,float roughness,float lacunarity)
{
    float value = 0;
    float tiling = 1;
    float intensity = 1;
    float totalIntensity = 1;
    for (int i = 1; i<=levels;i++)
    {
        value += noise(p*tiling)*intensity;
        intensity *= roughness;
        totalIntensity+=intensity;
        tiling*=lacunarity;
    }
    return value/totalIntensity;
}
