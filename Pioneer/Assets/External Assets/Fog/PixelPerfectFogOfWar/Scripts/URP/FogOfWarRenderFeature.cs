using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FOW
{
    public class FogOfWarRenderFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        [Tooltip("This is required for 'Texture Color' fog, but can increase gpu usage on mobile.")]
        public bool EnableNormals;
        FogOfWarPass fowPass;

        public override void Create()
        {
            fowPass = new FogOfWarPass(name);
            fowPass.renderPassEvent = renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            fowPass.renderPassEvent = renderPassEvent;
#if UNITY_6000_0_OR_NEWER
            fowPass.SetupRenderGraph();
#endif
            if (EnableNormals)
                fowPass.ConfigureInput(ScriptableRenderPassInput.Normal);
            else
                fowPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(fowPass);
        }
    }
}