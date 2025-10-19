using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FOW
{
    public class FogOfWarPass : ScriptableRenderPass
    {
        public static FogOfWarPass instance;
        public bool EffectEnabled = true;

        private string m_ProfilerTag;

        public FogOfWarPass(string tag)
        {
            m_ProfilerTag = tag;
        }

        #region SHARED
        void SetShaderProperties(Camera camera)
        {
            if (!FogOfWarWorld.instance.is2D)
            {
                Matrix4x4 camToWorldMatrix = camera.cameraToWorldMatrix;

                //Matrix4x4 projectionMatrix = renderingData.cameraData.camera.projectionMatrix;
                //Matrix4x4 inverseProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true).inverse;

                //inverseProjectionMatrix[1, 1] *= -1;

                FogOfWarWorld.instance.FogOfWarMaterial.SetMatrix("_camToWorldMatrix", camToWorldMatrix);
                //FogOfWarWorld.instance.fowMat.SetMatrix("_inverseProjectionMatrix", inverseProjectionMatrix);
            }
            else
            {
                FogOfWarWorld.instance.FogOfWarMaterial.SetFloat("_cameraSize", camera.orthographicSize);
                FogOfWarWorld.instance.FogOfWarMaterial.SetVector("_cameraPosition", camera.transform.position);
                FogOfWarWorld.instance.FogOfWarMaterial.SetFloat("_cameraRotation", Mathf.DeltaAngle(0, camera.transform.eulerAngles.z));
            }
        }
        #endregion

        #region COMPATIBILITY MODE

        private RenderTargetIdentifier source;
        private RenderTargetIdentifier destination;
        private static readonly int temporaryRTId = Shader.PropertyToID("_FowTempRT");
        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");

#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            instance = this;
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            //blitTargetDescriptor.depthBufferBits = 0;

            var renderer = renderingData.cameraData.renderer;

#if UNITY_2022_2_OR_NEWER
            source = renderer.cameraColorTargetHandle;
#else
            source = renderer.cameraColorTarget;
#endif

            cmd.GetTemporaryRT(temporaryRTId, blitTargetDescriptor);
            destination = new RenderTargetIdentifier(temporaryRTId);
        }

#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (FogOfWarWorld.instance == null || !FogOfWarWorld.instance.enabled || !EffectEnabled)
            {
                //Debug.Log("returning");
                return;
            }
            if (renderingData.cameraData.camera.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.DepthNormals;

            SetShaderProperties(renderingData.cameraData.camera);

            cmd.SetGlobalTexture(kBlitTexturePropertyId, source);
            // This uniform needs to be set for user materials with shaders relying on core Blit.hlsl to work as expected
            cmd.SetGlobalVector(kBlitScaleBiasPropertyId, new Vector4(1, 1, 0, 0));

            cmd.Blit(source, destination, FogOfWarWorld.instance.FogOfWarMaterial, 0);
            cmd.Blit(destination, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (temporaryRTId != -1)
                cmd.ReleaseTemporaryRT(temporaryRTId);
        }
        #endregion

        #region RENDER GRAPH
#if UNITY_6000_0_OR_NEWER
        const string m_PassName = "FOW_Pass";

        public void SetupRenderGraph()
        {
            requiresIntermediateTexture = true;
            instance = this;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            //return;
            // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
            // The active color and depth textures are the main color and depth buffers that the camera renders into
            var resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (FogOfWarWorld.instance == null || !FogOfWarWorld.instance.enabled || !EffectEnabled)
            {
                //Debug.Log("returning");
                return;
            }
            if (cameraData.camera.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay)
                return;

            //This should never happen since we set m_Pass.requiresIntermediateTexture = true;
            //Unless you set the render event to AfterRendering, where we only have the BackBuffer. 
            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError($"Skipping render pass. BlitAndSwapColorRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
                return;
            }

            // The destination texture is created here, 
            // the texture is created with the same dimensions as the active color texture
            var source = resourceData.activeColorTexture;

            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = $"CameraColor-{m_PassName}";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            SetShaderProperties(cameraData.camera);

            RenderGraphUtils.BlitMaterialParameters para = new(source, destination, FogOfWarWorld.instance.FogOfWarMaterial, 0);
            renderGraph.AddBlitPass(para, passName: m_PassName);

            //FrameData allows to get and set internal pipeline buffers. Here we update the CameraColorBuffer to the texture that we just wrote to in this pass. 
            //Because RenderGraph manages the pipeline resources and dependencies, following up passes will correctly use the right color buffer.
            //This optimization has some caveats. You have to be careful when the color buffer is persistent across frames and between different cameras, such as in camera stacking.
            //In those cases you need to make sure your texture is an RTHandle and that you properly manage the lifecycle of it.
            resourceData.cameraColor = destination;
        }
#endif
        #endregion
    }
}