using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MirrorEffect : ScriptableRendererFeature
{
    class MirrorPass : ScriptableRenderPass
    {
        private Material material;

        public MirrorPass(Material mat)
        {
            material = mat;
            renderPassEvent = RenderPassEvent.AfterRendering;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("MirrorPass");

            RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

            cmd.SetGlobalTexture("_MainTex", source);

            cmd.Blit(source, source, material);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public Shader mirrorShader;
    private Material mirrorMaterial;
    private MirrorPass mirrorPass;

    public override void Create()
    {
        if (mirrorShader == null)
            return;

        mirrorMaterial = CoreUtils.CreateEngineMaterial(mirrorShader);
        mirrorPass = new MirrorPass(mirrorMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (mirrorPass != null)
            renderer.EnqueuePass(mirrorPass);
    }
}
