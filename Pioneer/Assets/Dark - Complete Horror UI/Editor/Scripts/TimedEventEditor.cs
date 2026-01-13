using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(TimedEvent))]
    public class TimedEventEditor : Editor
    {
        private TimedEvent teTarget;
        private int currentTab;

        private void OnEnable()
        {
            teTarget = (TimedEvent)target;
        }

        public override void OnInspectorGUI()
        {
            GUISkin customSkin;
            Color defaultColor = GUI.color;

            if (EditorGUIUtility.isProSkin == true)
                customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark");
            else
                customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light");

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = defaultColor;

            // Top Header
            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("TE Top Header"));

            GUILayout.EndHorizontal();
            GUILayout.Space(-42);

            // Toolbar content
            GUIContent[] toolbarTabs = new GUIContent[1];
            toolbarTabs[0] = new GUIContent("Settings");

            GUILayout.BeginHorizontal();
            GUILayout.Space(17);

            currentTab = GUILayout.Toolbar(currentTab, toolbarTabs, customSkin.FindStyle("Tab Indicator"));

            GUILayout.EndHorizontal();
            GUILayout.Space(-40);
            GUILayout.BeginHorizontal();
            GUILayout.Space(17);

            // Draw toolbar tabs as a button
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 0;

            GUILayout.EndHorizontal();

            var timer = serializedObject.FindProperty("timer");
            var enableAtStart = serializedObject.FindProperty("enableAtStart");
            var timerEvents = serializedObject.FindProperty("timerEvents");

            // Draw content depending on tab index
            switch (currentTab)
            {
                case 0:
                    GUILayout.Space(6);
                    GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Options Header"));
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Timer"), customSkin.FindStyle("Text"), GUILayout.Width(100));
                    EditorGUILayout.PropertyField(timer, new GUIContent(""), true);

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    enableAtStart.boolValue = GUILayout.Toggle(enableAtStart.boolValue, new GUIContent("Enable At Start"), customSkin.FindStyle("Toggle"));
                    enableAtStart.boolValue = GUILayout.Toggle(enableAtStart.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.Space(6);
                    GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Events Header"));
                    EditorGUILayout.PropertyField(timerEvents, new GUIContent("Timer Events"), true);
                    break;
            }

            // Apply the changes
            serializedObject.ApplyModifiedProperties();
        }
    }
}