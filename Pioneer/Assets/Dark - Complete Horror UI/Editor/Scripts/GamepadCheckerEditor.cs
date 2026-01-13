using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(GamepadChecker))]
    public class GamepadCheckerEditor : Editor
    {
        private GUISkin customSkin;
        private GamepadChecker gcTarget;
        private int currentTab;

        private void OnEnable()
        {
            gcTarget = (GamepadChecker)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "GM Top Header");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var defaultPanelManager = serializedObject.FindProperty("defaultPanelManager");
            var panelManagers = serializedObject.FindProperty("panelManagers");
            var alwaysUpdate = serializedObject.FindProperty("alwaysUpdate");
            var affectCursor = serializedObject.FindProperty("affectCursor");
            var gamepadHotkey = serializedObject.FindProperty("gamepadHotkey");
            var keyboardObjects = serializedObject.FindProperty("keyboardObjects");
            var gamepadObjects = serializedObject.FindProperty("gamepadObjects");
            var buttons = serializedObject.FindProperty("buttons");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Content Header", 6);
                    DarkUIEditorHandler.DrawPropertyCW(defaultPanelManager, customSkin, "Default Panel Manager", 140);
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(12);
                    EditorGUILayout.PropertyField(panelManagers, new GUIContent("Panel Managers"), true);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Core Header"));
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(12);
                    EditorGUILayout.PropertyField(keyboardObjects, new GUIContent("Keyboard Objects"), true);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(12);
                    EditorGUILayout.PropertyField(gamepadObjects, new GUIContent("Gamepad Objects"), true);      
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(12);
                    EditorGUILayout.PropertyField(buttons, new GUIContent("Button Objects"), true);
                    GUILayout.EndHorizontal();
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    alwaysUpdate.boolValue = DarkUIEditorHandler.DrawToggle(alwaysUpdate.boolValue, customSkin, "Always Update");
                    affectCursor.boolValue = DarkUIEditorHandler.DrawToggle(affectCursor.boolValue, customSkin, "Affect Cursor");
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(gamepadHotkey, new GUIContent(""));
                    GUILayout.EndHorizontal();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}