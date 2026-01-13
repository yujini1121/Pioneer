using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(QualityManager))]
    public class QualityManagerEditor : Editor
    {
        private GUISkin customSkin;
        private QualityManager qmTarget;
        private int currentTab;

        private void OnEnable()
        {
            qmTarget = (QualityManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "QM Top Header");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Resources");
            toolbarTabs[1] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab Resources")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var mixer = serializedObject.FindProperty("mixer");
            var masterSlider = serializedObject.FindProperty("masterSlider");
            var musicSlider = serializedObject.FindProperty("musicSlider");
            var sfxSlider = serializedObject.FindProperty("sfxSlider");
            var customDropdown = serializedObject.FindProperty("customDropdown");
            var defaultDropdown = serializedObject.FindProperty("defaultDropdown");
            var preferCustomDropdown = serializedObject.FindProperty("preferCustomDropdown");
            var clickEvent = serializedObject.FindProperty("clickEvent");
            var isMobile = serializedObject.FindProperty("isMobile");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Core Header", 6);
                    DarkUIEditorHandler.DrawProperty(mixer, customSkin, "Audio Mixer");
                    DarkUIEditorHandler.DrawProperty(masterSlider, customSkin, "Master Slider");
                    DarkUIEditorHandler.DrawProperty(musicSlider, customSkin, "Music Slider");
                    DarkUIEditorHandler.DrawProperty(sfxSlider, customSkin, "SFX Slider");
                    DarkUIEditorHandler.DrawProperty(defaultDropdown, customSkin, "Default Dropdown");
                    GUI.enabled = false;
                    DarkUIEditorHandler.DrawProperty(customDropdown, customSkin, "Custom Dropdown");
                    GUI.enabled = true;
                    break;
               
                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    GUI.enabled = false;
                    preferCustomDropdown.boolValue = DarkUIEditorHandler.DrawToggle(preferCustomDropdown.boolValue, customSkin, "Prefer Custom Dropdown");
                    GUI.enabled = true;
                    isMobile.boolValue = DarkUIEditorHandler.DrawToggle(isMobile.boolValue, customSkin, "Is Mobile Scene");

                    DarkUIEditorHandler.DrawHeader(customSkin, "Events Header", 10);
                    EditorGUILayout.PropertyField(clickEvent, new GUIContent("Dynamic Res Event"), true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}