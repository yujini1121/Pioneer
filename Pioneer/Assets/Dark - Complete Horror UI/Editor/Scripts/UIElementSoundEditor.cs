using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(UIElementSound))]
    public class UIElementSoundEditor : Editor
    {
        private GUISkin customSkin;
        private UIElementSound uisTarget;
        private int currentTab;

        private void OnEnable()
        {
            uisTarget = (UIElementSound)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "UIS Top Header");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Settings");
         
            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var audioSource = serializedObject.FindProperty("audioSource");
            var hoverSound = serializedObject.FindProperty("hoverSound");
            var clickSound = serializedObject.FindProperty("clickSound");
            var enableHoverSound = serializedObject.FindProperty("enableHoverSound");
            var enableClickSound = serializedObject.FindProperty("enableClickSound");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Content Header", 6);
                    DarkUIEditorHandler.DrawProperty(audioSource, customSkin, "Audio Source");
                    if (enableHoverSound.boolValue == true) { DarkUIEditorHandler.DrawProperty(hoverSound, customSkin, "Hover Sound"); }
                    if (enableClickSound.boolValue == true) { DarkUIEditorHandler.DrawProperty(clickSound, customSkin, "Click Sound"); }
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    enableHoverSound.boolValue = DarkUIEditorHandler.DrawToggle(enableHoverSound.boolValue, customSkin, "Enable Hover Sound");
                    enableClickSound.boolValue = DarkUIEditorHandler.DrawToggle(enableClickSound.boolValue, customSkin, "Enable Click Sound");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}