using UnityEngine;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace FOW
{
    public class GenerateInvertedCollider : MonoBehaviour
    {
        public bool IncludeChildren = true;
        public bool DisableOldColliders = false;
        public LayerMask LayersToFlip;
#if UNITY_EDITOR
        public SingleUnityLayer FlippedColliderLayer;
#endif

        public Mesh GetFlippedMesh(Mesh mesh)
        {
            Mesh newMesh = new Mesh();

            newMesh.vertices = mesh.vertices;
            newMesh.triangles = mesh.triangles;

            newMesh.triangles = mesh.triangles.Reverse().ToArray();

            return newMesh;
            //GameObject flipped = new GameObject("flipped mesh");
            //flipped.AddComponent<MeshFilter>().mesh = newMesh;
        }

#if UNITY_EDITOR
        public IndexedFlippedCol flippedColliders = new IndexedFlippedCol();
        public void FlipColliders()
        {
            Collider[] colliders = new Collider[1];
            if (GetComponent<Collider>() != null)
                colliders[0] = GetComponent<Collider>();
            if (IncludeChildren)
                colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0 || colliders[0] == null)
            {
                Debug.LogError("no colliders found");
                return;
            }
            List<Collider> ToRemove = new List<Collider>();
            foreach(GameObject go in flippedColliders.gameObjects)
            {
                if (go == null)
                    ToRemove.Add(flippedColliders.colliders[flippedColliders.gameObjects.IndexOf(go)]);
            }
            foreach (Collider col in ToRemove)
            {
                flippedColliders.gameObjects.RemoveAt(flippedColliders.colliders.IndexOf(col));
                flippedColliders.colliders.Remove(col);
            }
            for (int i = 0; i < colliders.Length; i++)
            {
                if (LayersToFlip == (LayersToFlip | (1 << colliders[i].gameObject.layer)))
                {
                    if (flippedColliders.ContainsValue(colliders[i].gameObject))
                        continue;

                    if (!flippedColliders.ContainsKey(colliders[i]))
                    {
                        GameObject GO = new GameObject($"{colliders[i].gameObject.name} - Flipped");
                        GO.transform.SetParent(colliders[i].transform);
                        flippedColliders.Add(colliders[i], GO);
                    }
                    GameObject colliderObject = flippedColliders.gameObjects[flippedColliders.colliders.IndexOf(colliders[i])];
                    if (colliderObject.GetComponent<MeshCollider>() == null)
                        colliderObject.AddComponent<MeshCollider>();

                    Mesh meshToFlip;
                    GameObject createdObject = null;
                    if (colliders[i].GetComponent<BoxCollider>() != null)
                    {
                        createdObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        meshToFlip = createdObject.GetComponent<MeshFilter>().sharedMesh;
                    }
                    else if (colliders[i].GetComponent<CapsuleCollider>() != null)
                    {
                        createdObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        meshToFlip = createdObject.GetComponent<MeshFilter>().sharedMesh;
                    }
                    else if (colliders[i].GetComponent<SphereCollider>() != null)
                    {
                        createdObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        meshToFlip = createdObject.GetComponent<MeshFilter>().sharedMesh;
                    }
                    else
                        meshToFlip = colliders[i].GetComponent<MeshCollider>().sharedMesh;

                    colliderObject.layer = FlippedColliderLayer.LayerIndex;
                    colliderObject.GetComponent<MeshCollider>().sharedMesh = GetFlippedMesh(meshToFlip);
                    colliderObject.transform.localPosition = Vector3.zero;
                    colliderObject.transform.localRotation = Quaternion.identity;
                    colliderObject.transform.localScale = Vector3.one;

                    if (DisableOldColliders)
                        colliders[i].enabled = false;

                    if (createdObject != null)
                        DestroyImmediate(createdObject);
                }
            }
        }
#endif
    }
#if UNITY_EDITOR
    [System.Serializable]
    public class IndexedFlippedCol
    {
        public List<Collider> colliders = new List<Collider>();
        public List<GameObject> gameObjects = new List<GameObject>();
        public IndexedFlippedCol()
        {
            colliders = new List<Collider>();
            gameObjects = new List<GameObject>();
        }
        public void Add(Collider col, GameObject go)
        {
            colliders.Add(col);
            gameObjects.Add(go);
        }
        public bool ContainsKey(Collider col)
        {
            foreach (Collider _col in colliders)
            {
                if (col == _col)
                    return true;
            }
            return false;
        }
        public bool ContainsValue(GameObject go)
        {
            foreach(GameObject _go in gameObjects)
            {
                if (go == _go)
                    return true;
            }
            return false;
        }
    }
    [System.Serializable]
    public class SingleUnityLayer
    {
        [SerializeField]
        private int m_LayerIndex = 0;
        public int LayerIndex
        {
            get { return m_LayerIndex; }
        }

        public void Set(int _layerIndex)
        {
            if (_layerIndex > 0 && _layerIndex < 32)
            {
                m_LayerIndex = _layerIndex;
            }
        }

        public int Mask
        {
            get { return 1 << m_LayerIndex; }
        }
    }

    [CustomPropertyDrawer(typeof(SingleUnityLayer))]
    public class SingleUnityLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
        {
            EditorGUI.BeginProperty(_position, GUIContent.none, _property);
            SerializedProperty layerIndex = _property.FindPropertyRelative("m_LayerIndex");
            _position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
            if (layerIndex != null)
            {
                layerIndex.intValue = EditorGUI.LayerField(_position, layerIndex.intValue);
            }
            EditorGUI.EndProperty();
        }
    }
    [CustomEditor(typeof(GenerateInvertedCollider))]
    public class InvertedColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GenerateInvertedCollider gen = (GenerateInvertedCollider)target;
            if (GUILayout.Button("Generate Colliders"))
            {
                gen.FlipColliders();
                EditorUtility.SetDirty(gen);
            }
        }
    }
#endif
}