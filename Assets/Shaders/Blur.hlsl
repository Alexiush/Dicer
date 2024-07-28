float gauss(float x, float y, float sigma)
{
    return  1.0f / (2.0f * PI * sigma * sigma) * exp(-(x * x + y * y) / (2.0f * sigma * sigma));
}

void Blur_float(UnityTexture2D Texture, float texelX, float texelY, float2 UV, float Blur, UnitySamplerState Sampler, out float4 Out_c, out float Out_a)
{
    float4 col = float4(0.0, 0.0, 0.0, 0.0);
    float kernelSum = 0.0;

    int upper = ((Blur - 1) / 2);
    int lower = -upper;

    for (int x = lower; x <= upper; ++x)
    {
        for (int y = lower; y <= upper; ++y)
        {
            kernelSum++;

            float2 offset = float2(texelX * x, texelY * y);
            col += Texture.Sample(Sampler, UV + offset);
        }
    }

    col /= kernelSum;
    Out_c = float4(col.r, col.g, col.b, col.a);
	Out_a = col.a;
}

void BlurInverse_float(UnityTexture2D Texture, float texelX, float texelY, float2 UV, float Blur, UnitySamplerState Sampler, out float4 Out_c, out float Out_a)
{
    float4 col = float4(0.0, 0.0, 0.0, 0.0);
    float kernelSum = 0.0;

    int upper = ((Blur - 1) / 2);
    int lower = -upper;

    for (int x = lower; x <= upper; ++x)
    {
        for (int y = lower; y <= upper; ++y)
        {
            float weight = gauss(x, y, 0.8);
            kernelSum += weight;

            float2 offset = float2(texelX * x, texelY * y);
			float4 sample = Texture.Sample(Sampler, UV + offset);
            col += float4(sample.r, sample.g, sample.b, 1.0 - sample.a) * weight;
        }
    }

    col /= kernelSum;
    Out_c = float4(col.r, col.g, col.b, col.a);
	Out_a = col.a;
}