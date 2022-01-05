using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class GrabPassFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class GrabSettings {
        public string texureName = "GrabPass";
        public Material material;
        [Range(0.5f, 2f)]
        public float renderScale = 1f;
    }
    
    class GrabPass : ScriptableRenderPass {
        public GrabSettings settings;
        
        public RenderTargetHandle to;
        public RenderTargetHandle from;
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            cameraTextureDescriptor.msaaSamples = 1;
            cameraTextureDescriptor.depthBufferBits = 0;
            cameraTextureDescriptor.width = (int)(cameraTextureDescriptor.width * settings.renderScale);
            cameraTextureDescriptor.height = (int)(cameraTextureDescriptor.height * settings.renderScale);

            to = new RenderTargetHandle() {
                id = Shader.PropertyToID(settings.texureName)
            };
            cmd.GetTemporaryRT(to.id, cameraTextureDescriptor, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get(settings.texureName);
            
            Blit(cmd, from.Identifier(), to.Identifier(), settings.material);
            // 其实可以判断能否Copy
            Blit(cmd, to.Identifier(), from.Identifier());
            cmd.ReleaseTemporaryRT(to.id);
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    private GrabPass m_ScriptablePass;
    
    public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingOpaques;
    public GrabSettings settings;

    public override void Create()
    {
        m_ScriptablePass = new GrabPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.settings = settings;
        m_ScriptablePass.renderPassEvent = renderEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


