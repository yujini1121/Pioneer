using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrayOverlayPass : ScriptableRenderPass
{
    private Material material;
    private RenderTargetIdentifier source;
    private RenderTargetHandle tempTexture;

    public GrayOverlayPass(Material material)
    {
        this.material = material;
        tempTexture.Init("_TempRT");
        this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(RenderTargetIdentifier source)
    {
        this.source = source;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("GrayOverlayPass");

        // 임시 렌더 텍스처 생성
        RenderTextureDescriptor cameraTextureDesc = renderingData.cameraData.cameraTargetDescriptor;
        cmd.GetTemporaryRT(tempTexture.id, cameraTextureDesc);

        // 소스 텍스처를 임시 텍스처로 복사
        Blit(cmd, source, tempTexture.Identifier(), material);

        // 임시 텍스처를 다시 카메라 타겟으로 복사
        Blit(cmd, tempTexture.Identifier(), source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(tempTexture.id);
    }
}