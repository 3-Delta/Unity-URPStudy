using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// https://zhuanlan.zhihu.com/p/266174847
// https://zhuanlan.zhihu.com/p/31504088
public class PlanarShadow : ScriptableRendererFeature
{
    [Serializable]
    public class PlanarShadowSettings {
        public Vector3 planeNormalWS = Vector3.forward;
        public Material shadowMat;
        public StencilStateData stencilSettings = new StencilStateData();
    }
    
    class PlanarShadowRenderPass : ScriptableRenderPass {
        public PlanarShadowRenderPass() {
        }

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
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }
    
    public PlanarShadowSettings setting = new PlanarShadowSettings();
    private PlanarShadowRenderPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new PlanarShadowRenderPass();

        // Configures where the render pass should be injected.
        scriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(scriptablePass);
    }
}


