using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.SelfUniversal
{
    [BurstCompile]
    struct LightExtractionJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<VisibleLight> orderedLights;

        public NativeArray<LightType> lightTypesNA;

        public NativeArray<float> radiusesNA;

        public NativeArray<float3> directionsWSNA;

        public NativeArray<float3> positionsWSNA;

        public NativeArray<float> coneRadiusesNA;

        public void Execute(int index)
        {
            var light = this.orderedLights[index];
            var localToWorldMatrix = (float4x4)light.localToWorldMatrix;
            this.lightTypesNA[index] = light.lightType;
            this.radiusesNA[index] = light.range;
            this.directionsWSNA[index] = localToWorldMatrix.c2.xyz;
            this.positionsWSNA[index] = localToWorldMatrix.c3.xyz;
            this.coneRadiusesNA[index] = math.tan(math.radians(light.spotAngle * 0.5f)) * light.range;
        }
    }
}
