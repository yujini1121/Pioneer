using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(UIDissolveEffect))]
    public class UIDissolveEffectEditor : Editor
    {
        private GUISkin customSkin;
        private UIDissolveEffect uideTarget;
        private int currentTab;

        private void OnEnable()
        {
            uideTarget = (UIDissolveEffect)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "DE Top Header");

            GUIContent[] toolbarTabs = new GUIContent[1];
            toolbarTabs[0] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 0;

            GUILayout.EndHorizontal();

            var location = serializedObject.FindProperty("m_Location");
            var width = serializedObject.FindProperty("m_Width");
            var softness = serializedObject.FindProperty("m_Softness");
            var color = serializedObject.FindProperty("m_Color");
            var effectMaterial = serializedObject.FindProperty("m_EffectMaterial");
            var animationSpeed = serializedObject.FindProperty("animationSpeed");
            var mainPanelMode = serializedObject.FindProperty("mainPanelMode");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    mainPanelMode.boolValue = DarkUIEditorHandler.DrawToggle(mainPanelMode.boolValue, customSkin, "Enable Main Panel Mode");
                    GUILayout.Space(2);
                    DarkUIEditorHandler.DrawPropertyPlain(location, customSkin, "Location");
                    GUILayout.Space(2);
                    DarkUIEditorHandler.DrawPropertyPlain(width, customSkin, "Width");
                    GUILayout.Space(2);
                    DarkUIEditorHandler.DrawPropertyPlain(softness, customSkin, "Softness");

                    if (mainPanelMode.boolValue == false)
                    {
                        GUILayout.Space(2);
                        DarkUIEditorHandler.DrawPropertyPlain(animationSpeed, customSkin, "Anim Speed");
                    }

                    GUILayout.Space(2);
                    DarkUIEditorHandler.DrawPropertyPlain(color, customSkin, "Effect Color");
                    GUILayout.Space(2);
                    DarkUIEditorHandler.DrawPropertyPlain(effectMaterial, customSkin, "Effect Material");
                    GUILayout.EndVertical();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}