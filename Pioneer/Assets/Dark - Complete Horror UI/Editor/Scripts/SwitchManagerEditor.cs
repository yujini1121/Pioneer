using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(SwitchManager))]
    public class SwitchManagerEditor : Editor
    {
        private GUISkin customSkin;
        private SwitchManager smTarget;
        private int currentTab;

        private void OnEnable()
        {
            smTarget = (SwitchManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "Switch Top Header");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Events");
            toolbarTabs[1] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Events", "Events"), customSkin.FindStyle("Tab Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var onEvents = serializedObject.FindProperty("onEvents");
            var offEvents = serializedObject.FindProperty("offEvents");
            var saveValue = serializedObject.FindProperty("saveValue");
            var switchTag = serializedObject.FindProperty("switchTag");
            var isOn = serializedObject.FindProperty("isOn");
            var invokeAtStart = serializedObject.FindProperty("invokeAtStart");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Events Header", 6);
                    EditorGUILayout.PropertyField(onEvents, new GUIContent("On Events"), true);
                    EditorGUILayout.PropertyField(offEvents, new GUIContent("Off Events"), true);
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    isOn.boolValue = DarkUIEditorHandler.DrawToggle(isOn.boolValue, customSkin, "Is On (on enable)");
                    invokeAtStart.boolValue = DarkUIEditorHandler.DrawToggle(invokeAtStart.boolValue, customSkin, "Invoke At Start");
                    saveValue.boolValue = DarkUIEditorHandler.DrawToggle(saveValue.boolValue, customSkin, "Save Value");

                    if (saveValue.boolValue == true)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(35);

                        EditorGUILayout.LabelField(new GUIContent("Switch Tag"), customSkin.FindStyle("Text"), GUILayout.Width(100));
                        EditorGUILayout.PropertyField(switchTag, new GUIContent(""), true);

                        GUILayout.EndHorizontal();
                        EditorGUILayout.HelpBox("Each switch should has its own unique tag.", MessageType.Info);
                    }

                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}