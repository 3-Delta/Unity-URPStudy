using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering.SelfUniversal;

public class RadixSort : MonoBehaviour {
    public bool incrOrDe = true;
    public List<uint> array = new List<uint>();

    [ContextMenu(nameof(AUTO))]
    private void AUTO() {
        for (int i = 0; i < array.Count; ++i) {
            array[i] = (uint)i + 1;
        }
    }

    [ContextMenu(nameof(Begin))]
    private void Begin() {
        var keysNA = new NativeArray<uint>(array.Count * 2, Allocator.TempJob);
        for (int i = 0; i < array.Count; ++i) {
            keysNA[i] = this.array[i];
        }

        using var indicesNA = new NativeArray<int>(array.Count * 2, Allocator.TempJob);

        var zSortJob = new RadixSortJob {
            // Floats can be sorted bitwise with no special handling if positive floats only
            // keys在基数排序之后无用了
            keys = keysNA,
            indices = indicesNA
        };

        var zSortJobHandle = zSortJob.Schedule();

        zSortJobHandle.Complete();

        for (int i = 0, length = array.Count * 2; i < length; ++i) {
            Debug.LogError(indicesNA[i]);
        }

        keysNA.Dispose(zSortJobHandle);
    }
}
