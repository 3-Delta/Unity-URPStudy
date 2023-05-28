using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Rendering.SelfUniversal {
    // This could be multi-threaded if profiling shows need
    [BurstCompile]
    // 基数排序 https://www.cnblogs.com/skywang12345/p/3603669.html
    public unsafe struct RadixSortJob : IJob {
        public NativeArray<uint> keys;
        public NativeArray<int> indices;

        public void Execute() {
            // 容量申请256，刚好是8bit最大
            var bucket256NA = new NativeArray<int>(0xFF + 1, Allocator.Temp);
            var halfLength = indices.Length / 2;

            for (var i = 0; i < halfLength; i++) {
                indices[i] = i;
            }

            // 因为uint是4个8bit, 所以执行4个循环
            for (var i = 0; i < 4; i++) {
                int currentOffset;
                int nextOffset;

                // 重置buckets
                for (var j = 0; j < 256; j++) {
                    bucket256NA[j] = 0;
                }

                if (i % 2 == 0) // 偶数
                {
                    currentOffset = 0;
                    nextOffset = halfLength;
                }
                else {
                    currentOffset = halfLength;
                    nextOffset = 0;
                }

                for (var j = 0; j < halfLength; j++) {
                    var lightZVS = keys[currentOffset + j];
                    var bucketIndex = (lightZVS >> (8 * i));
                    // 限制在[0, 255]之间
                    bucketIndex &= 0xFF;

                    // 统计每个数字出现的次数
                    bucket256NA[(int)bucketIndex]++;
                }

                // 前缀和算法
                // 让bucket256NA[j]表示bucket256NA[0] + ... bucket256NA[n-1]总和
                // 作用就是：j的个数表示将来重新整理顺序之后的元素下标
                for (var j = 1; j < 256; j++) {
                    bucket256NA[j] += bucket256NA[j - 1];
                }

                for (var j = halfLength - 1; j >= 0; j--) {
                    var lightZVS = keys[currentOffset + j];
                    var bucketIndex = (lightZVS >> (8 * i));
                    bucketIndex &= 0xFF;

                    var newIndex = bucket256NA[(int)bucketIndex] - 1;
                    bucket256NA[(int)bucketIndex]--;
                    // todo finalIndex有可能会有越界的可能,比如keys是个257*2长度的数组，前半部分元素都是1，那么某个下标的bucket就是257
                    // 上面bucket256NA[(int)bucketIndex]-1就可能超过halfLength
                    // 最终nextOffset + bucket256NA[(int)bucketIndex]-1就可能越界

                    // 前后翻转
                    var finalIndex = nextOffset + newIndex;
                    keys[finalIndex] = lightZVS;
                    
                    // 最终结果就是：indices会成为一个有序的数组，只不过indices内部存储的是下标，也就是排序后数值所在的之前位置的下标
                    indices[finalIndex] = indices[currentOffset + j];
                }
            }

            bucket256NA.Dispose();
        }
    }
}
