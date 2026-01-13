using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(BackgroundFilter))]
    public class BackgroundFilterEditor : Editor
    {
        private GUISkin customSkin;
        private int currentTab;
        private BackgroundFilter bfTarget;
        List<string> filterList = new List<string>();

        private void OnEnable()
        {
            bfTarget = (BackgroundFilter)target;

            foreach (var t in bfTarget.filterList)
                filterList.Add(t.name);

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "Filter Top Header");

            GUIContent[] toolbarTabs = new GUIContent[1];
            toolbarTabs[0] = new GUIContent("Options");

            currentTab = DarkUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Customize", "Customize"), customSkin.FindStyle("Tab Settings")))
                currentTab = 0;

            GUILayout.EndHorizontal();

            var selectedFilter = serializedObject.FindProperty("selectedFilter");
            var filterIntensity = serializedObject.FindProperty("filterIntensity");
            var filterImage = serializedObject.FindProperty("filterImage");
            var editMode = serializedObject.FindProperty("editMode");
            var filterListMain = serializedObject.FindProperty("filterList");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(new GUIContent("Selected Filter"), customSkin.FindStyle("Text"), GUILayout.Width(100));
                    selectedFilter.intValue = EditorGUILayout.Popup(selectedFilter.intValue, filterList.ToArray());
                    bfTarget.filterImage.sprite = bfTarget.filterList[bfTarget.selectedFilter];
                    bfTarget.filterImage.color = new Color(bfTarget.filterImage.color.r, bfTarget.filterImage.color.g, bfTarget.filterImage.color.b, bfTarget.filterIntensity);

                    GUILayout.EndHorizontal();
                    DarkUIEditorHandler.DrawProperty(filterIntensity, customSkin, "Filter Intensity");
                    if (GUILayout.Button("Update", customSkin.button))
                    {
                        bfTarget.gameObject.SetActive(false);
                        bfTarget.gameObject.SetActive(true);
                    }

                    GUILayout.EndVertical();
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    editMode.boolValue = DarkUIEditorHandler.DrawTogglePlain(editMode.boolValue, customSkin, "Edit Mode");
                    GUILayout.Space(3);

                    if (editMode.boolValue == true)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.PropertyField(filterImage, new GUIContent("Filter Source"), true);

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(12);

                        EditorGUILayout.PropertyField(filterListMain, new GUIContent("Filters"), true);

                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }

                    GUILayout.EndVertical();
                    break;
            }

            this.Repaint();
            serializedObject.ApplyModifiedProperties();
        }
    }
}