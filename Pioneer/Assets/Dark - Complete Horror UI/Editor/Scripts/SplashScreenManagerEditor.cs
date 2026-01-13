using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(SplashScreenManager))]
    public class SplashScreenManagerEditor : Editor
    {
        private GUISkin customSkin;
        private SplashScreenManager ssmTarget;
        private int currentTab;

        private void OnEnable()
        {
            ssmTarget = (SplashScreenManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "SSM Top Header");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var splashScreenTitles = serializedObject.FindProperty("splashScreenTitles");
            var splashScreen = serializedObject.FindProperty("splashScreen");
            var mainPanelParent = serializedObject.FindProperty("mainPanelParent");
            var modalWindowParent = serializedObject.FindProperty("modalWindowParent");
            var transitionHelper = serializedObject.FindProperty("transitionHelper");
            var disableSplashScreen = serializedObject.FindProperty("disableSplashScreen");
            var startDelay = serializedObject.FindProperty("startDelay");
            var onSplashScreenEnd = serializedObject.FindProperty("onSplashScreenEnd");
            var mainPanelManager = serializedObject.FindProperty("mainPanelManager");
            var showOnlyOnce = serializedObject.FindProperty("showOnlyOnce");
            var skipOnAnyKeyPress = serializedObject.FindProperty("skipOnAnyKeyPress");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Content Header", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(12);
                    EditorGUILayout.PropertyField(splashScreenTitles, new GUIContent("Splash Screen Titles"), true);
                    splashScreenTitles.isExpanded = true;

                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);

                    if (ssmTarget.splashScreenTitles.Count != 0 && ssmTarget.splashScreenTitles[ssmTarget.splashScreenTitles.Count - 1] != null)
                    {
                        if (GUILayout.Button("+  Create a new title", customSkin.button))
                        {
                            GameObject go = Instantiate(ssmTarget.splashScreenTitles[1].gameObject, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                            go.transform.SetParent(ssmTarget.splashScreenTitles[1].transform.parent, false);
                            go.gameObject.name = "New Title";
                            ssmTarget.splashScreenTitles.Add(go.GetComponent<SplashScreenTitle>());
                        }
                    }

                    GUILayout.EndVertical();
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Core Header", 6);
                    DarkUIEditorHandler.DrawProperty(splashScreen, customSkin, "Splash Screen");
                    DarkUIEditorHandler.DrawProperty(mainPanelManager, customSkin, "Main Panel Manager");
                    DarkUIEditorHandler.DrawProperty(mainPanelParent, customSkin, "Main Panel Parent");
                    DarkUIEditorHandler.DrawProperty(modalWindowParent, customSkin, "Modal Window Parent");
                    DarkUIEditorHandler.DrawProperty(transitionHelper, customSkin, "Transition Helper");
                    break;

                case 2:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Content Header", 6);
                    disableSplashScreen.boolValue = DarkUIEditorHandler.DrawToggle(disableSplashScreen.boolValue, customSkin, "Disable Splash Screen");
                    showOnlyOnce.boolValue = DarkUIEditorHandler.DrawToggle(showOnlyOnce.boolValue, customSkin, "Show Only Once");
                    skipOnAnyKeyPress.boolValue = DarkUIEditorHandler.DrawToggle(skipOnAnyKeyPress.boolValue, customSkin, "Skip On Any Key Press");
                    DarkUIEditorHandler.DrawProperty(startDelay, customSkin, "Start Delay");
                    EditorGUILayout.PropertyField(onSplashScreenEnd, new GUIContent("On Splash Screen End"), true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}