using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrayOverlayFeature : ScriptableRendererFeature
{
    public Material material;
    private GrayOverlayPass pass;

    /// <inheritdoc/>
    public override void Create()
    {
        pass = new GrayOverlayPass(material);
        pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.Setup(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(pass);
    }
}


