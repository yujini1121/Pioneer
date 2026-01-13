using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Michsky.UI.Dark
{
    public class ContextMenu : Editor
    {
        static string objectPath;

        static void GetObjectPath()
        {
            objectPath = AssetDatabase.GetAssetPath(Resources.Load("Dark UI Manager"));
            objectPath = objectPath.Replace("Resources/Dark UI Manager.asset", "").Trim();
            objectPath = objectPath + "Prefabs/";
        }

        static void MakeSceneDirty(GameObject source, string sourceName)
        {
            if (Application.isPlaying == false)
            {
                Undo.RegisterCreatedObjectUndo(source, sourceName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        static void ShowErrorDialog()
        {
            EditorUtility.DisplayDialog("Dark UI", "Cannot create the object due to missing manager file. " +
                    "Make sure you have 'Dark UI Manager' file in Dark - Complete Horror UI > Resources folder.", "Okay");
        }

        static void UpdateCustomEditorPath()
        {
            string mainPath = AssetDatabase.GetAssetPath(Resources.Load("Dark UI Manager"));
            mainPath = mainPath.Replace("Resources/Dark UI Manager.asset", "").Trim();
            string darkPath = mainPath + "Editor/DUI Skin Dark.guiskin";
            string lightPath = mainPath + "Editor/DUI Skin Light.guiskin";

            EditorPrefs.SetString("DarkUI.CustomEditorDark", darkPath);
            EditorPrefs.SetString("DarkUI.CustomEditorLight", lightPath);
        }

        static void CreateObject(string resourcePath)
        {
            try
            {
                GetObjectPath();
                UpdateCustomEditorPath();
                GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + resourcePath + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;

                try
                {
                    if (Selection.activeGameObject == null)
                    {
#if UNITY_2023_2_OR_NEWER
                        var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
#else
                        var canvas = (Canvas)FindObjectsOfType(typeof(Canvas))[0];
#endif
                        clone.transform.SetParent(canvas.transform, false);
                    }

                    else { clone.transform.SetParent(Selection.activeGameObject.transform, false); }

                    clone.name = clone.name.Replace("(Clone)", "").Trim();
                    MakeSceneDirty(clone, clone.name);
                }

                catch
                {
                    CreateCanvas();
#if UNITY_2023_2_OR_NEWER
                    var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
#else
                    var canvas = (Canvas)FindObjectsOfType(typeof(Canvas))[0];
#endif
                    clone.transform.SetParent(canvas.transform, false);
                    clone.name = clone.name.Replace("(Clone)", "").Trim();
                    MakeSceneDirty(clone, clone.name);
                }

                Selection.activeObject = clone;
            }

            catch { ShowErrorDialog(); }
        }

        [MenuItem("GameObject/Dark UI/Canvas", false, -1)]
        static void CreateCanvas()
        {
            try
            {
                GetObjectPath();
                UpdateCustomEditorPath();
                GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + "UI Elements/Canvas" + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
                clone.name = clone.name.Replace("(Clone)", "").Trim();
                Selection.activeObject = clone;
                MakeSceneDirty(clone, clone.name);
            }

            catch { ShowErrorDialog(); }
        }

        [MenuItem("Tools/Dark UI/Show UI Manager")]
        static void ShowManager()
        {
            Selection.activeObject = Resources.Load("Dark UI Manager");

            if (Selection.activeObject == null)
                Debug.Log("<b>[Dark UI]</b>Can't find a file named 'Dark UI Manager'. Make sure you have 'Dark UI Manager' file in Resources folder.");
        }

        [MenuItem("GameObject/Dark UI/Chapter Button", false, 0)]
        static void UIECB()
        {
            CreateObject("UI Elements/Chapter Button");
        }

        [MenuItem("GameObject/Dark UI/Dropdown", false, 0)]
        static void UIEDR()
        {
            CreateObject("UI Elements/Dropdown");
        }

        [MenuItem("GameObject/Dark UI/Horizontal Selector", false, 0)]
        static void UIEHS()
        {
            CreateObject("UI Elements/Horizontal Selector");
        }

        [MenuItem("GameObject/Dark UI/Load Save Button", false, 0)]
        static void UIELSB()
        {
            CreateObject("UI Elements/Load Save Button");
        }

        [MenuItem("GameObject/Dark UI/Main Button", false, 0)]
        static void UIEMB()
        {
            CreateObject("UI Elements/Main Button");
        }

        [MenuItem("GameObject/Dark UI/Main Button (Free)", false, 0)]
        static void UIEMBF()
        {
            CreateObject("UI Elements/Main Button (Free)");
        }

        [MenuItem("GameObject/Dark UI/Modal Window", false, 0)]
        static void UIEMW()
        {
            CreateObject("UI Elements/Modal Window");
        }

        [MenuItem("GameObject/Dark UI/Radial Button", false, 0)]
        static void UIERB()
        {
            CreateObject("UI Elements/Radial Button");
        }

        [MenuItem("GameObject/Dark UI/Scrollbar", false, 0)]
        static void UIESCR()
        {
            CreateObject("UI Elements/Scrollbar");
        }

        [MenuItem("GameObject/Dark UI/Server Button", false, 0)]
        static void UIESB()
        {
            CreateObject("UI Elements/Server Button");
        }

        [MenuItem("GameObject/Dark UI/Shortcut Key", false, 0)]
        static void UIESK()
        {
            CreateObject("UI Elements/Shortcut Key");
        }

        [MenuItem("GameObject/Dark UI/Shortcut Key (Gamepad)", false, 0)]
        static void UIESKG()
        {
            CreateObject("UI Elements/Shortcut Key (Gamepad)");
        }

        [MenuItem("GameObject/Dark UI/Slider", false, 0)]
        static void UIESLI()
        {
            CreateObject("UI Elements/Slider");
        }

        [MenuItem("GameObject/Dark UI/Switch", false, 0)]
        static void UIESWI()
        {
            CreateObject("UI Elements/Switch");
        }

        [MenuItem("GameObject/Dark UI/Tab Button", false, 0)]
        static void UIETABB()
        {
            CreateObject("UI Elements/Tab Button");
        }
    }
}
#endif