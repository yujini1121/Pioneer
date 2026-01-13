using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(SliderManager))]
    public class SliderManagerEditor : Editor
    {
        private GUISkin customSkin;
        private SliderManager slmTarget;
        private int currentTab;

        private void OnEnable()
        {
            slmTarget = (SliderManager)target;
           
            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "Slider Top Header");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Resources");
            toolbarTabs[1] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab Resources")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var valueText = serializedObject.FindProperty("valueText");
            var popupValueText = serializedObject.FindProperty("popupValueText");
            var enableSaving = serializedObject.FindProperty("enableSaving");
            var sliderTag = serializedObject.FindProperty("sliderTag");
            var usePercent = serializedObject.FindProperty("usePercent");
            var showValue = serializedObject.FindProperty("showValue");
            var showPopupValue = serializedObject.FindProperty("showPopupValue");
            var useRoundValue = serializedObject.FindProperty("useRoundValue");
            var invokeOnAwake = serializedObject.FindProperty("invokeOnAwake");
            var valueMultiplier = serializedObject.FindProperty("valueMultiplier");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Core Header", 6);
                    DarkUIEditorHandler.DrawProperty(valueText, customSkin, "Value Text");
                    DarkUIEditorHandler.DrawProperty(popupValueText, customSkin, "Popup Value Text");
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    DarkUIEditorHandler.DrawProperty(valueMultiplier, customSkin, "Value Multiplier");
                    usePercent.boolValue = DarkUIEditorHandler.DrawToggle(usePercent.boolValue, customSkin, "Use Percent");
                    showValue.boolValue = DarkUIEditorHandler.DrawToggle(showValue.boolValue, customSkin, "Show Value");
                    showPopupValue.boolValue = DarkUIEditorHandler.DrawToggle(showPopupValue.boolValue, customSkin, "Show Popup Value");
                    useRoundValue.boolValue = DarkUIEditorHandler.DrawToggle(useRoundValue.boolValue, customSkin, "Use Round Value");
                    invokeOnAwake.boolValue = DarkUIEditorHandler.DrawToggle(invokeOnAwake.boolValue, customSkin, "Invoke On Awake");
                    enableSaving.boolValue = DarkUIEditorHandler.DrawToggle(enableSaving.boolValue, customSkin, "Save Value");

                    if (enableSaving.boolValue == true)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(35);

                        EditorGUILayout.LabelField(new GUIContent("Slider Tag"), customSkin.FindStyle("Text"), GUILayout.Width(100));
                        EditorGUILayout.PropertyField(sliderTag, new GUIContent(""), true);

                        GUILayout.EndHorizontal();
                        EditorGUILayout.HelpBox("Each slider should has its own unique tag.", MessageType.Info);
                    }

                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}