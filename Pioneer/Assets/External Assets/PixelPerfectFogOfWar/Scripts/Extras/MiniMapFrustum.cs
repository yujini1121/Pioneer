using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    public class MiniMapFrustum : MonoBehaviour
    {
        public Collider MapCollider;
        public float RayDistance = 100;

        public int ResolutionX = 256;
        public int ResolutionY = 256;
        public UnityEngine.UI.RawImage RawImageComponent;
        public Color LineColor = Color.white;
        public float LineWidth = .05f;

        private Plane fallbackPlane;
        private Material blitMaterial;
        private RenderTexture Frustum_RT;
        public RenderTexture GetFrustumRT() { return Frustum_RT; }
        private Vector4 _worldBounds;
        private Vector3[] points;
        private Vector2[] screenPositions;
        private Vector2[] UVs;
        private Vector2 frustumCenterUV;

//        [Header("Debug")]
//#if UNITY_EDITOR
//        public bool[] ExcludeDraw = new bool[4];
//#endif

        private void Start()
        {
            //Debug.Log(blitMaterial.shader.name);
            blitMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            blitMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
            blitMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            blitMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            blitMaterial.SetInt("_ZWrite", 0);
            blitMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            //blitMaterial = new Material(Shader.Find("Sprites/Default"));
            points = new Vector3[4];
            screenPositions = new Vector2[4];
            UVs = new Vector2[4];
            InitFrustumRT();
            fallbackPlane = new Plane();
            fallbackPlane.SetNormalAndPosition(FogOfWarWorld.UpVector, MapCollider.transform.position);
        }

        void InitFrustumRT()
        {
            Frustum_RT = new RenderTexture(ResolutionX, ResolutionY, 0);
            Frustum_RT.format = RenderTextureFormat.ARGBHalf;
            Frustum_RT.antiAliasing = 8;
            Frustum_RT.filterMode = FilterMode.Trilinear;
            Frustum_RT.anisoLevel = 9;
            Frustum_RT.Create();
            RenderTexture.active = Frustum_RT;

            Material mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            mat.SetPass(0);

            GL.Begin(GL.TRIANGLES);
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            GL.End();
            if (RawImageComponent != null)
                RawImageComponent.texture = Frustum_RT;
        }

        private RaycastHit rayHit;
        private Vector3 GetWorldSpaceFrustomCorner(Vector2 ScreenPosition)
        {
            Ray ray = Camera.main.ScreenPointToRay(ScreenPosition);
            if (MapCollider.Raycast(ray, out rayHit, RayDistance))
                return rayHit.point;
            else
            {
                fallbackPlane.Raycast(ray, out float enter);
                return ray.GetPoint(enter);
            }
        }

        private void SetScreenPositions()
        {
            screenPositions[0].x = 0;
            screenPositions[0].y = 0;

            screenPositions[1].x = 0;
            screenPositions[1].y = Screen.height;

            screenPositions[2].x = Screen.width;
            screenPositions[2].y = Screen.height;

            screenPositions[3].x = Screen.width;
            screenPositions[3].y = 0;
        }

        private void Update()
        {
            SetScreenPositions();
            points[0] = GetWorldSpaceFrustomCorner(screenPositions[0]);
            points[1] = GetWorldSpaceFrustomCorner(screenPositions[1]);
            points[2] = GetWorldSpaceFrustomCorner(screenPositions[2]);
            points[3] = GetWorldSpaceFrustomCorner(screenPositions[3]);
            _worldBounds = FogOfWarWorld.instance.GetBoundsVectorForShader();

            frustumCenterUV.x = 0;
            frustumCenterUV.y = 0;
            for (int i = 0; i < 4; i++)
            {
                UVs[i] = GetUV(points[i]);
                frustumCenterUV += UVs[i];
            }
            frustumCenterUV /= 4;
            //Debug.Log(frustumCenterUV);

            DrawMiniMapFrustum();
        }

        Vector2 GetUV(Vector3 WorldPosition)
        {
            Vector2 Position = FogOfWarWorld.instance.GetFowPositionFromWorldPosition(WorldPosition);
            Vector2 uv = new Vector2((((Position.x - _worldBounds.y) + (_worldBounds.x / 2)) / _worldBounds.x),
                 (((Position.y - _worldBounds.w) + (_worldBounds.z / 2)) / _worldBounds.z));

            return uv;
        }

        void DrawMiniMapFrustum()
        {
            GL.PushMatrix();
            blitMaterial.SetPass(0);
            GL.LoadOrtho();
            RenderTexture.active = Frustum_RT;
            GL.Begin(GL.TRIANGLES);
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            GL.End();
            GL.Begin(GL.QUADS);
            GL.Color(LineColor);
            Vector2 CrossVector1;
            Vector2 CrossVector2;


            //GL.Vertex(new Vector3(frustumCenterUV.x - .01f, frustumCenterUV.y -.01f, 0));
            //GL.Vertex(new Vector3(frustumCenterUV.x - .01f, frustumCenterUV.y + .01f, 0));
            //GL.Vertex(new Vector3(frustumCenterUV.x + .01f, frustumCenterUV.y +.01f, 0));
            //GL.Vertex(new Vector3(frustumCenterUV.x + .01f, frustumCenterUV.y - .01f, 0));
            void DrawLine(Vector2 uv1, Vector2 uv2)
            {
                GL.Vertex(new Vector3(uv1.x, uv1.y, 0));
                GL.Vertex(new Vector3(uv2.x, uv2.y, 0));
                GL.Vertex(new Vector3(uv2.x + CrossVector2.x, uv2.y + CrossVector2.y, 0));
                GL.Vertex(new Vector3(uv1.x + CrossVector1.x, uv1.y + CrossVector1.y, 0));
            }
            void NormalizeVectors()
            {
                //CrossVector1.Normalize();
                //CrossVector2.Normalize();
                //CrossVector2 = CrossVector2.normalized;
                //return;
                CrossVector1 = CrossVector1 * LineWidth;
                CrossVector2 = CrossVector2 * LineWidth;
            }

            CrossVector1 = (frustumCenterUV - UVs[0]);
            CrossVector2 = (frustumCenterUV - UVs[1]);
            NormalizeVectors();
            DrawLine(UVs[0], UVs[1]);

            //GL.Color(Color.blue);
            CrossVector1 = (frustumCenterUV - UVs[1]);
            CrossVector2 = (frustumCenterUV - UVs[2]);
            NormalizeVectors();
            DrawLine(UVs[1], UVs[2]);

            //GL.Color(Color.red);
            CrossVector1 = (frustumCenterUV - UVs[2]);
            CrossVector2 = (frustumCenterUV - UVs[3]);
            NormalizeVectors();
            DrawLine(UVs[2], UVs[3]);

            //GL.Color(Color.cyan);
            CrossVector1 = (frustumCenterUV - UVs[3]);
            CrossVector2 = (frustumCenterUV - UVs[0]);
            NormalizeVectors();
            DrawLine(UVs[3], UVs[0]);

            GL.End();
            GL.PopMatrix();
        }
    }
}