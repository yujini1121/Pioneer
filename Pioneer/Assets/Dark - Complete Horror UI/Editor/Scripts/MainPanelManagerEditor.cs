using UnityEngine;
using UnityEditor;
using static UnityEngine.GraphicsBuffer;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(MainPanelManager))]
    public class MainPanelManagerEditor : Editor
    {
        private GUISkin customSkin;
        private MainPanelManager mpmTarget;
        private int currentTab;

        private void OnEnable()
        {
            mpmTarget = (MainPanelManager)target; 
            
            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "MPM Top Header");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Settings");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var panels = serializedObject.FindProperty("panels");
            var currentPanelIndex = serializedObject.FindProperty("currentPanelIndex");
            var disablePanelAfter = serializedObject.FindProperty("disablePanelAfter");
            var animationSmoothness = serializedObject.FindProperty("animationSmoothness");
            var animationSpeed = serializedObject.FindProperty("animationSpeed");
            var instantInOnEnable = serializedObject.FindProperty("instantInOnEnable");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Content Header", 6);
                   
                    if (mpmTarget.currentPanelIndex > mpmTarget.panels.Count - 1) { mpmTarget.currentPanelIndex = 0; }
                    if (mpmTarget.panels.Count != 0)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();

                        GUI.enabled = false;
                        EditorGUILayout.LabelField(new GUIContent("Current Panel:"), customSkin.FindStyle("Text"), GUILayout.Width(82));
                        GUI.enabled = true;
                        EditorGUILayout.LabelField(new GUIContent(mpmTarget.panels[currentPanelIndex.intValue].panelName), customSkin.FindStyle("Text"));

                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);

                        if (Application.isPlaying == true) { GUI.enabled = false; }

                        currentPanelIndex.intValue = EditorGUILayout.IntSlider(currentPanelIndex.intValue, 0, mpmTarget.panels.Count - 1);

                        if (Application.isPlaying == false && mpmTarget.panels[currentPanelIndex.intValue].panelObject != null)
                        {
                            for (int i = 0; i < mpmTarget.panels.Count; i++)
                            {
                                if (i == currentPanelIndex.intValue)
                                {
                                    var tempCG = mpmTarget.panels[currentPanelIndex.intValue].panelObject.GetComponent<CanvasGroup>();
                                    if (tempCG != null) { tempCG.alpha = 1; }
                                }

                                else if (mpmTarget.panels[i].panelObject != null)
                                {
                                    var tempCG = mpmTarget.panels[i].panelObject.GetComponent<CanvasGroup>();
                                    if (tempCG != null) { tempCG.alpha = 0; }
                                }
                            }
                        }
                        if (mpmTarget.panels[mpmTarget.currentPanelIndex].panelObject != null && GUILayout.Button("Select Current Panel", customSkin.button)) { Selection.activeObject = mpmTarget.panels[mpmTarget.currentPanelIndex].panelObject; }
                        GUI.enabled = true;
                        GUILayout.EndVertical();
                    }

                    else { EditorGUILayout.HelpBox("Panel List is empty. Create a new item to see more options.", MessageType.Info); }

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(12);
                    EditorGUILayout.PropertyField(panels, new GUIContent("Panel Items"), true);
                    panels.isExpanded = true;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);

                    if (GUILayout.Button("+  Add a new item", customSkin.button))
                        mpmTarget.AddNewItem();

                    GUILayout.EndVertical();
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    instantInOnEnable.boolValue = DarkUIEditorHandler.DrawToggle(instantInOnEnable.boolValue, customSkin, "Instant In On Enable");
                    DarkUIEditorHandler.DrawProperty(animationSpeed, customSkin, "Anim Speed");
                    DarkUIEditorHandler.DrawProperty(animationSmoothness, customSkin, "Anim Smoothness");
                    DarkUIEditorHandler.DrawProperty(disablePanelAfter, customSkin, "Disable Panel After");           
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}