using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(BlurManager))]
    public class BlurManagerEditor : Editor
    {
        private GUISkin customSkin;
        private BlurManager bmTarget;
        private int currentTab;

        private void OnEnable()
        {
            bmTarget = (BlurManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "BM Top Header");

            // Toolbar content
            GUIContent[] toolbarTabs = new GUIContent[1];
            toolbarTabs[0] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 0;

            GUILayout.EndHorizontal();

            var blurMaterial = serializedObject.FindProperty("blurMaterial");
            var blurValue = serializedObject.FindProperty("blurValue");
            var animationSpeed = serializedObject.FindProperty("animationSpeed");
            var customProperty = serializedObject.FindProperty("customProperty");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    DarkUIEditorHandler.DrawPropertyCW(blurMaterial, customSkin, "Blur Material", 100);
                    DarkUIEditorHandler.DrawPropertyCW(blurValue, customSkin, "Blur Value", 100);
                    DarkUIEditorHandler.DrawPropertyCW(animationSpeed, customSkin, "Anim Speed", 100);
                    DarkUIEditorHandler.DrawPropertyCW(customProperty, customSkin, "Property Name", 100);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}