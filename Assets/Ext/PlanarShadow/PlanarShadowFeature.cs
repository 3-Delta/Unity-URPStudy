using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// https://zhuanlan.zhihu.com/p/266174847
// https://zhuanlan.zhihu.com/p/31504088
// https://github.com/czy-moyu/PlanarShadow-URP
public class PlanarShadowFeature : ScriptableRendererFeature {
    [Serializable]
    public class PlanarShadowSettings {
        public Vector3 planeNormalWS = Vector3.forward;
        public Material shadowMat;
        public StencilStateData stencilSettings = new StencilStateData();
    }

    class PlanarShadowRenderPass : ScriptableRenderPass {
        private ProfilingSampler profilingSampler;
        private Material shadowMat;
        private RenderStateBlock m_RenderStateBlock;

        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            // 分别指定stencil和depth的override
            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;
        }
        
        public PlanarShadowRenderPass(Material shadowMat) {
            this.profilingSampler = new ProfilingSampler("PlanarShadow");
            this.shadowMat = shadowMat;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmdBuffer = CommandBufferPool.Get(profilingSampler.name);
            using (new ProfilingScope(cmdBuffer, profilingSampler)) {
                cmdBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, shadowMat);
            }

            context.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Clear();
            CommandBufferPool.Release(cmdBuffer);
        }
    }

    public PlanarShadowSettings settings = new PlanarShadowSettings();
    private PlanarShadowRenderPass scriptablePass;

    public override void Create() {
        scriptablePass = new PlanarShadowRenderPass(settings.shadowMat);
        
        if (settings.stencilSettings.overrideStencilState)
        {
            scriptablePass.SetStencilState(settings.stencilSettings.stencilReference,
                settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);
            
        }
        

        // Configures where the render pass should be injected.
        scriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(scriptablePass);
    }
}
