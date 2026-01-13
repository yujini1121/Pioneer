using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif

namespace Michsky.UI.Dark
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark UI/Rendering/UI Dissolve Effect")]
    public class UIDissolveEffect : BaseMeshEffect
    {
        public const string shaderName = "Dark UI/Dissolve Effect";

        public enum ColorMode
        {
            None = 0,
            Set,
            Add,
            Sub,
        }

        [SerializeField] [Range(0, 1)] float m_Location = 0.5f;
        [SerializeField] [Range(0, 1)] float m_Width = 0.5f;
        [SerializeField] [Range(0, 1)] float m_Softness = 0.5f;
        [SerializeField] [ColorUsage(false)] Color m_Color = new Color(0f, 0f, 0f);
        [SerializeField] [HideInInspector] ColorMode m_ColorMode = ColorMode.Add;
        [SerializeField] Material m_EffectMaterial;
        [Range(0.25f, 3)] public float animationSpeed = 25;
        public bool mainPanelMode;

        new public Graphic graphic { get { return base.graphic; } }
        public float location { get { return m_Location; } set { m_Location = Mathf.Clamp(value, 0, 1); _SetDirty(); } }
        public float width { get { return m_Width; } set { m_Width = Mathf.Clamp(value, 0, 1); _SetDirty(); } }
        public float softness { get { return m_Softness; } set { m_Softness = Mathf.Clamp(value, 0, 1); _SetDirty(); } }
        public Color color { get { return m_Color; } set { m_Color = value; _SetDirty(); } }
        public ColorMode colorMode { get { return m_ColorMode; } }
        public virtual Material effectMaterial { get { return m_EffectMaterial; } }

        protected override void OnEnable()
        {
            try
            {
                graphic.material = effectMaterial;
            }

            catch { }
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            try
            {
                graphic.material = null;
            }

            catch { }
            base.OnDisable();
        }

#if UNITY_EDITOR
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            var obj = this;
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying || !obj || !obj.graphic)
                    return;

                var mat = GetOrGenerateMaterialVariant(Shader.Find(shaderName), m_ColorMode);

                if (m_EffectMaterial == mat && graphic.material == mat)
                    return;

                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(graphic);
                EditorApplication.delayCall += AssetDatabase.SaveAssets;
            };
        }

        public static Material GetMaterial(Shader shader, ColorMode color)
        {
            string variantName = GetVariantName(shader, color);
            return AssetDatabase.FindAssets("t:Material " + Path.GetFileName(shader.name))
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .SelectMany(x => AssetDatabase.LoadAllAssetsAtPath(x))
                .OfType<Material>()
                .FirstOrDefault(x => x.name == variantName);
        }

        public static Material GetOrGenerateMaterialVariant(Shader shader, ColorMode color)
        {
            if (!shader)
                return null;

            Material mat = GetMaterial(shader, color);

            if (!mat)
            {
                Debug.Log("Generate material: " + GetVariantName(shader, color));
                mat = new Material(shader);

                if (0 < color)
                    mat.EnableKeyword("UI_COLOR_" + color.ToString().ToUpper());

                mat.name = GetVariantName(shader, color);
                mat.hideFlags |= HideFlags.NotEditable;

                #if UIEFFECT_SEPARATE
				bool isMainAsset = true;
				string dir = Path.GetDirectoryName(GetDefaultMaterialPath (shader));
				string materialPath = Path.Combine(Path.Combine(dir, "Separated"), mat.name + ".mat");
                #else
                bool isMainAsset = (0 == color);
                string materialPath = GetDefaultMaterialPath(shader);
                #endif

                if (isMainAsset)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(materialPath));
                    AssetDatabase.CreateAsset(mat, materialPath);
                    AssetDatabase.SaveAssets();
                }

                else
                {
                    mat.hideFlags |= HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(mat, materialPath);
                }
            }
            return mat;
        }

        public static string GetDefaultMaterialPath(Shader shader)
        {
            var name = Path.GetFileName(shader.name);
            return AssetDatabase.FindAssets("t:Material " + name)
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == name)
                ?? ("Assets/Dark UI/Materials/" + name + ".mat");
        }

        public static string GetVariantName(Shader shader, ColorMode color)
        {
            return
            #if UIEFFECT_SEPARATE
				"[Separated] " + Path.GetFileName(shader.name)
            #else
                Path.GetFileName(shader.name)
            #endif
                + (0 < color ? "-" + color : "");
        }
        #endif

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            Rect rect = graphic.rectTransform.rect;

            UIVertex vertex = default(UIVertex);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);

                var x = Mathf.Clamp01(vertex.position.x / rect.width + 0.5f);
                var y = Mathf.Clamp01(vertex.position.y / rect.height + 0.5f);
                vertex.uv1 = new Vector2(_PackToFloat(x, y, location, m_Width), _PackToFloat(m_Color.r, m_Color.g, m_Color.b, m_Softness));

                vh.SetUIVertex(vertex, i);
            }
        }

        void _SetDirty()
        {
            if (graphic)
                graphic.SetVerticesDirty();
        }

        static float _PackToFloat(float x, float y, float z)
        {
            const int PRECISION = (1 << 8) - 1;
            return (Mathf.FloorToInt(z * PRECISION) << 16)
            + (Mathf.FloorToInt(y * PRECISION) << 8)
            + Mathf.FloorToInt(x * PRECISION);
        }

        static float _PackToFloat(float x, float y, float z, float w)
        {
            const int PRECISION = (1 << 6) - 1;
            return (Mathf.FloorToInt(w * PRECISION) << 18)
            + (Mathf.FloorToInt(z * PRECISION) << 12)
            + (Mathf.FloorToInt(y * PRECISION) << 6)
            + Mathf.FloorToInt(x * PRECISION);
        }

        public void DissolveIn()
        {
            StopCoroutine("AnimateDissolveOut");
            StartCoroutine("AnimateDissolveIn");
        }

        public void DissolveOut()
        {
            StopCoroutine("AnimateDissolveIn");
            StartCoroutine("AnimateDissolveOut");
        }

        IEnumerator AnimateDissolveIn()
        {
            while (location >= 0)
            {
                location -= Time.unscaledDeltaTime * animationSpeed;

                if (location == 0 || location <= 0)
                    StopCoroutine("AnimateDissolveIn");

                yield return null;
            }
        }

        IEnumerator AnimateDissolveOut()
        {
            while (location <= 1)
            {
                location += Time.unscaledDeltaTime * animationSpeed;
                
                if (location == 1 || location >= 1)
                    StopCoroutine("AnimateDissolveIn");

                yield return null;
            }
        }
    }
}