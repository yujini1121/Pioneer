using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    [ExecuteInEditMode]
    public class FOWImageEffect : MonoBehaviour
    {
        Camera cam;

        //public bool isGL;
        private void Awake()
        {
            //isGL = SystemInfo.graphicsDeviceVersion.Contains("OpenGL");
            SetCamera();
        }

        void SetCamera()
        {
            if (cam)
                return;
            cam = GetComponent<Camera>();
            cam.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        }

        private void OnPreRender()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            SetCamera();
#endif
            if (!FogOfWarWorld.instance)
                return;

            if (!FogOfWarWorld.instance.is2D)
            {
                Matrix4x4 camToWorldMatrix = cam.cameraToWorldMatrix;

                //Matrix4x4 projectionMatrix = renderingData.cameraData.camera.projectionMatrix;
                //Matrix4x4 inverseProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true).inverse;

                //inverseProjectionMatrix[1, 1] *= -1;

                FogOfWarWorld.instance.FogOfWarMaterial.SetMatrix("_camToWorldMatrix", camToWorldMatrix);
                //FogOfWarWorld.instance.fowMat.SetMatrix("_inverseProjectionMatrix", inverseProjectionMatrix);
            }
            else
            {
                FogOfWarWorld.instance.FogOfWarMaterial.SetFloat("_cameraSize", cam.orthographicSize);
                FogOfWarWorld.instance.FogOfWarMaterial.SetVector("_cameraPosition", cam.transform.position);
                FogOfWarWorld.instance.FogOfWarMaterial.SetFloat("_cameraRotation", Mathf.DeltaAngle(0, cam.transform.eulerAngles.z));
            }
        }

        [ImageEffectOpaque]
        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
#if UNITY_EDITOR
            Graphics.Blit(src, dest);
            if (!Application.isPlaying)
                return;
#endif
            if (!FogOfWarWorld.instance || !FogOfWarWorld.instance.enabled)
            {
                Graphics.Blit(src, dest);
                return;
            }

            Graphics.Blit(src, dest, FogOfWarWorld.instance.FogOfWarMaterial);
        }
    }
}