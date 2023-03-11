using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.SelfUniversal
{
    class ShaderData : IDisposable
    {
        static ShaderData m_Instance = null;
        
        ComputeBuffer m_LightDataCompBuffer = null; // ShaderInput.LightData
        ComputeBuffer m_LightIndicesCompBuffer = null; // int

        ComputeBuffer m_AdditionalLightShadowParamsCompBuffer = null; // Vector4
        ComputeBuffer m_AdditionalLightShadowSliceMatricesCompBuffer = null; // Vector4

        ShaderData()
        {
        }

        internal static ShaderData instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new ShaderData();

                return m_Instance;
            }
        }

        public void Dispose()
        {
            DisposeBuffer(ref this.m_LightDataCompBuffer);
            DisposeBuffer(ref this.m_LightIndicesCompBuffer);
            DisposeBuffer(ref this.m_AdditionalLightShadowParamsCompBuffer);
            DisposeBuffer(ref this.m_AdditionalLightShadowSliceMatricesCompBuffer);
        }

        internal ComputeBuffer GetLightDataBuffer(int size)
        {
            return GetOrUpdateBuffer<ShaderInput.LightData>(ref this.m_LightDataCompBuffer, size);
        }

        internal ComputeBuffer GetLightIndicesBuffer(int size)
        {
            return GetOrUpdateBuffer<int>(ref this.m_LightIndicesCompBuffer, size);
        }

        internal ComputeBuffer GetAdditionalLightShadowParamsStructuredBuffer(int size)
        {
            return GetOrUpdateBuffer<Vector4>(ref this.m_AdditionalLightShadowParamsCompBuffer, size);
        }

        internal ComputeBuffer GetAdditionalLightShadowSliceMatricesStructuredBuffer(int size)
        {
            return GetOrUpdateBuffer<Matrix4x4>(ref this.m_AdditionalLightShadowSliceMatricesCompBuffer, size);
        }

        ComputeBuffer GetOrUpdateBuffer<T>(ref ComputeBuffer buffer, int size) where T : struct
        {
            if (buffer == null)
            {
                buffer = new ComputeBuffer(size, Marshal.SizeOf<T>());
            }
            else if (size > buffer.count)
            {
                buffer.Dispose();
                buffer = new ComputeBuffer(size, Marshal.SizeOf<T>());
            }

            return buffer;
        }

        void DisposeBuffer(ref ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Dispose();
                buffer = null;
            }
        }
    }
}
