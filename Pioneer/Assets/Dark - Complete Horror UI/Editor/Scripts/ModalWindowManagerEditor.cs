using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(ModalWindowManager))]
    public class ModalWindowManagerEditor : Editor
    {
        private GUISkin customSkin;
        private ModalWindowManager mwTarget;
        private int currentTab;

        private void OnEnable()
        {
            mwTarget = (ModalWindowManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "MW Top Header");

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

            var title = serializedObject.FindProperty("title");
            var description = serializedObject.FindProperty("description");
            var onEnable = serializedObject.FindProperty("onEnable");
            var onCancel = serializedObject.FindProperty("onCancel");
            var windowIcon = serializedObject.FindProperty("windowIcon");
            var titleObject = serializedObject.FindProperty("titleObject");
            var descriptionObject = serializedObject.FindProperty("descriptionObject");
            var windowAnimator = serializedObject.FindProperty("windowAnimator");
            var enableDissolve = serializedObject.FindProperty("enableDissolve");
            var dissolveEffect = serializedObject.FindProperty("dissolveEffect");
            var useCustomContent = serializedObject.FindProperty("useCustomContent");
            var disableOnOut = serializedObject.FindProperty("disableOnOut");
            var disableAfter = serializedObject.FindProperty("disableAfter");
            var blurManager = serializedObject.FindProperty("blurManager");
            var inputBlockDuration = serializedObject.FindProperty("inputBlockDuration");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Content Header", 6);
                    DarkUIEditorHandler.DrawProperty(title, customSkin, "Title");

                    if (mwTarget.titleObject != null && useCustomContent.boolValue == false)
                        mwTarget.titleObject.text = title.stringValue;

                    else if (mwTarget.titleObject == null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox("'Title Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(new GUIContent("Description"), customSkin.FindStyle("Text"), GUILayout.Width(-3));
                    EditorGUILayout.PropertyField(description, new GUIContent(""));
                    GUILayout.EndHorizontal();

                    if (mwTarget.descriptionObject != null && useCustomContent.boolValue == false)
                        mwTarget.descriptionObject.text = description.stringValue;

                    else if (mwTarget.descriptionObject == null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox("'Description Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                        GUILayout.EndHorizontal();
                    }

                    if (mwTarget.GetComponent<CanvasGroup>().alpha == 0)
                    {
                        if (GUILayout.Button("Set Visible", customSkin.button))
                        {
                            mwTarget.GetComponent<CanvasGroup>().alpha = 1;
                            Undo.RegisterCreatedObjectUndo(mwTarget, "Modal set visible");
                        }
                    }

                    else
                    {
                        if (GUILayout.Button("Set Invisible", customSkin.button))
                        {
                            mwTarget.GetComponent<CanvasGroup>().alpha = 0;
                            Undo.RegisterCreatedObjectUndo(mwTarget, "Modal set invisible");
                        }
                    }

                    DarkUIEditorHandler.DrawHeader(customSkin, "Events Header", 10);
                    EditorGUILayout.PropertyField(onEnable, new GUIContent("On Confirm"), true);
                    EditorGUILayout.PropertyField(onCancel, new GUIContent("On Cancel"), true);
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Core Header", 6);
                    DarkUIEditorHandler.DrawProperty(titleObject, customSkin, "Title Object");
                    DarkUIEditorHandler.DrawProperty(descriptionObject, customSkin, "Description Object");
                    DarkUIEditorHandler.DrawProperty(blurManager, customSkin, "Blur Manager");
                    DarkUIEditorHandler.DrawProperty(windowAnimator, customSkin, "Animator");
                    break;

                case 2:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 6);
                    useCustomContent.boolValue = DarkUIEditorHandler.DrawToggle(useCustomContent.boolValue, customSkin, "Use Custom Content");
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    enableDissolve.boolValue = DarkUIEditorHandler.DrawTogglePlain(enableDissolve.boolValue, customSkin, "Enable Dissolve Effect");
                    GUILayout.Space(4);
                    if (disableOnOut.boolValue == true) { DarkUIEditorHandler.DrawPropertyPlain(dissolveEffect, customSkin, "Dissolve Effect"); }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    disableOnOut.boolValue = DarkUIEditorHandler.DrawTogglePlain(disableOnOut.boolValue, customSkin, "Disable On Close");
                    GUILayout.Space(4);
                    if (disableOnOut.boolValue == true) { DarkUIEditorHandler.DrawPropertyPlain(disableAfter, customSkin, "Disable After"); }
                    GUILayout.EndVertical();
                    DarkUIEditorHandler.DrawProperty(inputBlockDuration, customSkin, "Input Block Duration");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}