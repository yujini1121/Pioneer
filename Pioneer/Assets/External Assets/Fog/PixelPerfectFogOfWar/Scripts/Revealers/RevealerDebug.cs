using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FOW
{
    public class RevealerDebug : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool DrawDebugStats = true;
        public bool DrawSegments = false;
        public bool DrawOutline = false;
        [SerializeField] protected float DrawRayNoise = 0;

        private FogOfWarRevealer _revealer;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            if (_revealer == null)
            {
                if (!TryGetComponent<FogOfWarRevealer>(out _revealer))
                    return;
            }
            if (!DrawDebugStats)
                return;
            if (FogOfWarWorld.instance == null)
                return;

            for (int i = 0; i < _revealer.NumberOfPoints; i++)
            {
                DrawString(i.ToString(), GetSegmentEnd(i), Color.white);
                if (DrawDebugStats)
                {
                    //Debug.Log(deg);
                    //Debug.DrawRay(GetEyePosition(), (ViewPoints[i].point - GetEyePosition()) + UnityEngine.Random.insideUnitSphere * DrawRayNoise, Color.blue);
                    if (DrawSegments)
                        Debug.DrawRay(_revealer.GetEyePosition(), (GetSegmentEnd(i) - _revealer.GetEyePosition()) + UnityEngine.Random.insideUnitSphere * DrawRayNoise, Color.blue);
                    //drawString(i.ToString(), ViewPoints[i].point, Color.white);

                    if (i != 0 && DrawOutline)
                        Debug.DrawLine(GetSegmentEnd(i), GetSegmentEnd(i - 1), Color.yellow);
                    //Debug.DrawLine(ViewPoints[i].point, ViewPoints[i - 1].point, Color.yellow);
                }
            }
        }

        static void DrawString(string text, Vector3 worldPos, Color? colour = null)
        {
            UnityEditor.Handles.BeginGUI();
            if (colour.HasValue) GUI.color = colour.Value;
            var view = UnityEditor.SceneView.currentDrawingSceneView;
            if (!view)
                return;
            Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);
            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text, guiStyle);
            UnityEditor.Handles.EndGUI();

        }

        Vector3 GetSegmentEnd(int index)
        {
            return _revealer.GetEyePosition() + (_revealer.DirFromAngle(_revealer.ViewPoints[index].Angle, true) * (_revealer.ViewPoints[index].DidHit ? _revealer.ViewPoints[index].Radius : _revealer.GetRayDistance()));
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RevealerDebug))]
    public class RevealerDebugEditor : Editor
    {
        FogOfWarRevealer Revealer;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RevealerDebug stat = (RevealerDebug)target;

            //FogOfWarRevealer rev = stat.GetRevealerComponent();

            if (Revealer == null)
            {
                if (!stat.TryGetComponent<FogOfWarRevealer>(out Revealer))
                {
                    EditorGUILayout.LabelField($"Revealer component not found.");
                    return;
                }
            }

            if (!stat.DrawDebugStats)
                return;

            EditorGUILayout.LabelField(" ");
            EditorGUILayout.LabelField($"NUM SEGMENTS: {Revealer.NumberOfPoints}");
            for (int i = 0; i < Revealer.NumberOfPoints; i++)
            {
                EditorGUILayout.LabelField($"------------- Segment {i} -------------");
                EditorGUILayout.LabelField($"Angle: {Revealer.Angles[i]}");
                EditorGUILayout.LabelField($"Radius: {Revealer.Radii[i]}");
                EditorGUILayout.LabelField($"Did Hit?: {Revealer.AreHits[i]}");
            }
            if (Application.isPlaying)
            {
                if (GUILayout.Button("Debug Toggle Static"))
                {
                    Revealer.SetRevealerAsStatic(!Revealer.StaticRevealer);
                }
            }
        }
    }
#endif
}