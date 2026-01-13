using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(PressKeyEvent))]
    public class PressKeyEventEditor : Editor
    {
        private GUISkin customSkin;
        private PressKeyEvent pkeTarget;
        private int currentTab;

        private void OnEnable()
        {
            pkeTarget = (PressKeyEvent)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "PKE Top Header");

            GUIContent[] toolbarTabs = new GUIContent[1];
            toolbarTabs[0] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 0;

            GUILayout.EndHorizontal();

            var hotkey = serializedObject.FindProperty("hotkey");
            var onPressEvent = serializedObject.FindProperty("onPressEvent");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(hotkey, new GUIContent(""), true);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.PropertyField(onPressEvent, new GUIContent("Press Key Events"), true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}