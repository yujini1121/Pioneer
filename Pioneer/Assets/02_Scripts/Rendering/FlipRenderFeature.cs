using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.Burst.Intrinsics.X86.Avx;

public class FlipRenderFeature : ScriptableRendererFeature
{
    class FlipPass : ScriptableRenderPass
    {
        private Material flipMaterial;
        private RTHandle tempRT;
        private RTHandle source;

        public FlipPass(Material material)
        {
            flipMaterial = material;
        }

        public void Setup(RTHandle sourceRT)
        {
            source = sourceRT;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            tempRT = RTHandles.Alloc(
                cameraTextureDescriptor.width,
                cameraTextureDescriptor.height,
                depthBufferBits: 0,
                colorFormat: cameraTextureDescriptor.graphicsFormat,
                name: "_TempFlipRT"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Flip Pass");

            Blitter.BlitCameraTexture(cmd, source, tempRT, flipMaterial, 0);
            Blitter.BlitCameraTexture(cmd, tempRT, source, flipMaterial, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (tempRT != null)
                RTHandles.Release(tempRT);
        }
    }

    public Material flipMaterial;
    FlipPass pass;

    public override void Create()
    {
        pass = new FlipPass(flipMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(pass);
    }
}
