namespace UnityEngine.Rendering.SelfUniversal
{
    /// <summary>
    /// Applies relevant settings before rendering transparent objects
    /// </summary>

    internal class TransparentSettingsPass : ScriptableRenderPass
    {
        // 半透明物体接收阴影
        bool _mShouldTransparentReceiveShadowsTransparent;

        const string m_ProfilerTag = "Transparent Settings Pass";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);

        public TransparentSettingsPass(RenderPassEvent evt, bool shadowTransparentReceive)
        {
            base.profilingSampler = new ProfilingSampler(nameof(TransparentSettingsPass));
            renderPassEvent = evt;
            this._mShouldTransparentReceiveShadowsTransparent = shadowTransparentReceive;
        }

        public bool Setup(ref RenderingData renderingData)
        {
            // Currently we only need to enqueue this pass when the user
            // doesn't want transparent objects to receive shadows
            return !this._mShouldTransparentReceiveShadowsTransparent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get a command buffer...
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // Toggle light shadows enabled based on the renderer setting set in the constructor
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, this._mShouldTransparentReceiveShadowsTransparent);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, this._mShouldTransparentReceiveShadowsTransparent);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, this._mShouldTransparentReceiveShadowsTransparent);
            }

            // Execute and release the command buffer...
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
