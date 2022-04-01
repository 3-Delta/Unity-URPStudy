using System;
using UnityEngine;
using UnityEngine.Rendering;

public class Using<T> where T : IDisposable {
    public T value { get; protected set; }

    public virtual void Dispose() { }
}

public class UsingContextCmdBuffer : Using<CommandBuffer>, IDisposable {
    private ScriptableRenderContext context;

    public UsingContextCmdBuffer(ScriptableRenderContext context) {
        this.context = context;
        this.value = CommandBufferPool.Get(ToString() + (GetHashCode().ToString()));
    }

    public override void Dispose() {
        context.ExecuteCommandBuffer(value);
        value.Clear();
        CommandBufferPool.Release(value);
    }
}

// public class TestUsingCmdBuffer {
//     public static void Test() {
//         ScriptableRenderContext context = new ScriptableRenderContext();
//         using (UsingContextCmdBuffer ing = new UsingContextCmdBuffer(context)) {
//             // ing.value.DrawMesh(null, null);
//         }
//     }
// }
