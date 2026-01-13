using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(HorizontalSelector))]
    public class HorizontalSelectorEditor : Editor
    {
        private GUISkin customSkin;
        private HorizontalSelector hsTarget;
        private int currentTab;

        private void OnEnable()
        {
            hsTarget = (HorizontalSelector)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            DarkUIEditorHandler.DrawComponentHeader(customSkin, "HS Top Header");

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

            var itemList = serializedObject.FindProperty("itemList");
            var onValueChanged = serializedObject.FindProperty("onValueChanged");
            var label = serializedObject.FindProperty("label");
            var selectorAnimator = serializedObject.FindProperty("selectorAnimator");
            var labelHelper = serializedObject.FindProperty("labelHelper");
            var labelIcon = serializedObject.FindProperty("labelIcon");
            var labelIconHelper = serializedObject.FindProperty("labelIconHelper");
            var indicatorParent = serializedObject.FindProperty("indicatorParent");
            var indicatorObject = serializedObject.FindProperty("indicatorObject");
            var enableIcon = serializedObject.FindProperty("enableIcon");
            var saveValue = serializedObject.FindProperty("saveValue");
            var selectorTag = serializedObject.FindProperty("selectorTag");
            var enableIndicators = serializedObject.FindProperty("enableIndicators");
            var invokeAtStart = serializedObject.FindProperty("invokeAtStart");
            var invertAnimation = serializedObject.FindProperty("invertAnimation");
            var loopSelection = serializedObject.FindProperty("loopSelection");
            var defaultIndex = serializedObject.FindProperty("defaultIndex");
            var iconScale = serializedObject.FindProperty("iconScale");
            var contentSpacing = serializedObject.FindProperty("contentSpacing");
            var contentLayout = serializedObject.FindProperty("contentLayout");
            var contentLayoutHelper = serializedObject.FindProperty("contentLayoutHelper");
            var enableUIManager = serializedObject.FindProperty("enableUIManager");

            switch (currentTab)
            {
                case 0:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Content Header", 6);

                    if (hsTarget.itemList.Count != 0)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Selected Item Index"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        defaultIndex.intValue = EditorGUILayout.IntSlider(defaultIndex.intValue, 0, hsTarget.itemList.Count - 1);

                        GUILayout.Space(2);
                        GUILayout.EndHorizontal();
                    }

                    else { EditorGUILayout.HelpBox("There is no item in the list.", MessageType.Warning); }

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(itemList, new GUIContent("Selector Items"), true);
                    itemList.isExpanded = true;

                    EditorGUI.indentLevel = 1;

                    if (GUILayout.Button("+  Add a new item", customSkin.button))
                        hsTarget.AddNewItem();

                    GUILayout.EndVertical();

                    DarkUIEditorHandler.DrawHeader(customSkin, "Events Header", 10);
                    EditorGUILayout.PropertyField(onValueChanged, new GUIContent("On Value Changed"), true);
                    break;

                case 1:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Core Header", 6);
                    DarkUIEditorHandler.DrawProperty(selectorAnimator, customSkin, "Animator");
                    DarkUIEditorHandler.DrawProperty(label, customSkin, "Label");
                    DarkUIEditorHandler.DrawProperty(labelHelper, customSkin, "Label Helper");
                    DarkUIEditorHandler.DrawProperty(labelIcon, customSkin, "Label Icon");
                    DarkUIEditorHandler.DrawProperty(labelIconHelper, customSkin, "Label Icon Helper");
                    DarkUIEditorHandler.DrawProperty(indicatorParent, customSkin, "Indicator Parent");
                    DarkUIEditorHandler.DrawProperty(indicatorObject, customSkin, "Indicator Object");
                    DarkUIEditorHandler.DrawProperty(contentLayout, customSkin, "Content Layout");
                    DarkUIEditorHandler.DrawProperty(contentLayoutHelper, customSkin, "Content Layout Helper");
                    break;

                case 2:
                    DarkUIEditorHandler.DrawHeader(customSkin, "Customization Header", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    enableIcon.boolValue = DarkUIEditorHandler.DrawTogglePlain(enableIcon.boolValue, customSkin, "Enable Icon");
                    GUILayout.Space(3);

                    if (enableIcon.boolValue == true && hsTarget.labelIcon == null)
                        EditorGUILayout.HelpBox("'Enable Icon' is enabled but 'Label Icon' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);

                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    enableIndicators.boolValue = DarkUIEditorHandler.DrawTogglePlain(enableIndicators.boolValue, customSkin, "Enable Indicators");
                    GUILayout.Space(3);
                    GUILayout.BeginHorizontal();

                    if (enableIndicators.boolValue == true)
                    {
                        if (hsTarget.indicatorObject == null)
                            EditorGUILayout.HelpBox("'Enable Indicators' is enabled but 'Indicator Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);

                        if (hsTarget.indicatorParent == null)
                            EditorGUILayout.HelpBox("'Enable Indicators' is enabled but 'Indicator Parent' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                        else
                            hsTarget.indicatorParent.gameObject.SetActive(true);
                    }

                    else if (hsTarget.indicatorParent != null) { hsTarget.indicatorParent.gameObject.SetActive(false); }

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    DarkUIEditorHandler.DrawProperty(iconScale, customSkin, "Icon Scale");
                    DarkUIEditorHandler.DrawProperty(contentSpacing, customSkin, "Content Spacing");

                    hsTarget.UpdateContentLayout();

                    DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 10);
                    invertAnimation.boolValue = DarkUIEditorHandler.DrawToggle(invertAnimation.boolValue, customSkin, "Invert Animation");
                    loopSelection.boolValue = DarkUIEditorHandler.DrawToggle(loopSelection.boolValue, customSkin, "Loop Selection");
                    invokeAtStart.boolValue = DarkUIEditorHandler.DrawToggle(invokeAtStart.boolValue, customSkin, "Invoke At Start");
                    saveValue.boolValue = DarkUIEditorHandler.DrawToggle(saveValue.boolValue, customSkin, "Save Selection");
                    GUILayout.Space(3);

                    if (saveValue.boolValue == true)
                    {
                        GUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(new GUIContent("Selector Tag:"), customSkin.FindStyle("Text"), GUILayout.Width(90));
                        EditorGUILayout.PropertyField(selectorTag, new GUIContent(""));

                        GUILayout.EndHorizontal();
                        EditorGUILayout.HelpBox("Each selector should has its own unique tag.", MessageType.Info);
                    }

                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}