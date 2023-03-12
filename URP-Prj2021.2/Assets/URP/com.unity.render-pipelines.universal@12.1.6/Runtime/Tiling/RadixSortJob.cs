using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Rendering.SelfUniversal
{
    // This could be multi-threaded if profiling shows need
    [BurstCompile]
    // 基数排序
    unsafe struct RadixSortJob : IJob
    {
        public NativeArray<uint> keys;
        public NativeArray<int> indices;

        public void Execute()
        {
            var countsNA = new NativeArray<int>(256, Allocator.Temp);
            var halfLength = indices.Length / 2;

            for (var i = 0; i < halfLength; i++)
            {
                indices[i] = i;
            }

            // 偶奇偶奇
            for (var i = 0; i < 4; i++)
            {
                int currentOffset, nextOffset;

                for (var j = 0; j < 256; j++)
                {
                    countsNA[j] = 0;
                }

                if (i % 2 == 0) // 偶数
                {
                    currentOffset = 0;
                    nextOffset = halfLength;
                }
                else
                {
                    currentOffset = halfLength;
                    nextOffset = 0;
                }

                for (var j = 0; j < halfLength; j++)
                {
                    var lightZVS = keys[currentOffset + j];
                    var bucket = (lightZVS >> (8 * i)) & 0xFF;
                    countsNA[(int)bucket]++;
                }

                for (var j = 1; j < 256; j++)
                {
                    countsNA[j] += countsNA[j - 1];
                }

                for (var j = halfLength - 1; j >= 0; j--)
                {
                    var lightZVS = keys[currentOffset + j];
                    var bucket = (lightZVS >> (8 * i)) & 0xFF;
                    var newIndex = countsNA[(int)bucket] - 1;
                    countsNA[(int)bucket]--;
                    
                    keys[nextOffset + newIndex] = lightZVS;
                    indices[nextOffset + newIndex] = indices[currentOffset + j];
                }
            }

            countsNA.Dispose();
        }
    }
}
