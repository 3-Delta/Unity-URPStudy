using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Serialization;

public class GrabPassFeature : ScriptableRendererFeature {
    [System.Serializable]
    public class GrabSettings {
        public Downsampling sample = Downsampling._2xBilinear;
        public Material material;
    }

    class GrabPass : CopyColorPass {
        public const string rtName = "_GrabPass";

        public RenderTargetHandle to;

        public GrabPass(RenderPassEvent renderEvent, Material material) : base(renderEvent, material) {
            to = new RenderTargetHandle() {
                id = Shader.PropertyToID(rtName)
            };
        }

        public void Setup(RenderTargetIdentifier from, Downsampling sample) {
            base.Setup(from, to, sample);
        }
    }
    
    public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    public GrabSettings settings;
    
    private GrabPass pass;

    public override void Create() {
        pass = new GrabPass(renderEvent, settings.material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        pass.Setup(renderer.cameraColorTarget, settings.sample);
        renderer.EnqueuePass(pass);
    }
}
