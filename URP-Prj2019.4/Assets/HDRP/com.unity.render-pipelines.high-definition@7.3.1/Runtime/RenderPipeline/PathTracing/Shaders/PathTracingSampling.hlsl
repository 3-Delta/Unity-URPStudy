#include "Assets/HDRP/com.unity.render-pipelines.high-definition@7.3.1/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"

float GetSample(uint2 coord, uint index, uint dim)
{
    // If we go past the number of stored samples per dim, just shift all to the next pair of dimensions
    dim += (index / 256) * 2;

    return GetBNDSequenceSample(coord, index, dim);
}
