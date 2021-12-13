namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Applies relevant settings before rendering transparent objects
    /// </summary>

    internal class TransparentSettingsPass : ScriptableRenderPass
    {
        // 半透明物体是否接受阴影
        // 其实是通过在renderevent渲染半透明物体之前，传送这个bool
        // 关键的是：renderevent必须控制在RenderPassEvent.BeforeRenderingTransparents
        bool m_shouldReceiveShadows;

        const string m_ProfilerTag = "Transparent Settings Pass";


        public TransparentSettingsPass(RenderPassEvent evt, bool shadowReceiveSupported)
        {
            renderPassEvent = evt;
            m_shouldReceiveShadows = shadowReceiveSupported;
        }

        public bool Setup(ref RenderingData renderingData)
        {
            // Currently we only need to enqueue this pass when the user
            // doesn't want transparent objects to receive shadows
            return !m_shouldReceiveShadows;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get a command buffer...
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            // Toggle light shadows enabled based on the renderer setting set in the constructor
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, m_shouldReceiveShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, m_shouldReceiveShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, m_shouldReceiveShadows);

            // Execute and release the command buffer...
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
