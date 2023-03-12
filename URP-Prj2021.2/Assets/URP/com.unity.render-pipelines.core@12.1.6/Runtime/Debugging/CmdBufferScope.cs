using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering {
    public struct CmdBufferScope : IDisposable {
        public CommandBuffer cmdBuffer { get; private set; }
        bool m_Disposed;

        public CmdBufferScope(string cmdName = "") {
            this.cmdBuffer = CommandBufferPool.Get(cmdName);
            m_Disposed = false;
        }

        public void Dispose() {
            Dispose(true);
        }

        void Dispose(bool disposing) {
            if (m_Disposed) {
                return;
            }

            if (disposing) {
                CommandBufferPool.Release(this.cmdBuffer);
            }

            m_Disposed = true;
        }
    }
}
