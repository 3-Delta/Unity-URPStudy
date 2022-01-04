using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// https://answer.uwa4d.com/question/5f20e9972a9f497246652475
public class ShaderReplacement : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass {
        private readonly ShaderTagId replaceTagId = new ShaderTagId("ShaderReplacement");
        public LayerMask layerMask = -1;
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(replaceTagId, ref renderingData,SortingCriteria.CommonOpaque);
            drawingSettings.enableDynamicBatching = true;
            drawingSettings.perObjectData = PerObjectData.None;

            FilteringSettings filterSettings = new FilteringSettings( RenderQueueRange.all, layerMask);
            context.DrawRenderers(renderingData.cullResults,ref drawingSettings,ref filterSettings);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


