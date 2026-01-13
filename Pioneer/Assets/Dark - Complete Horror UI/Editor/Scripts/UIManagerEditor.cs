using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace Michsky.UI.Dark
{
    [CustomEditor(typeof(UIManager))]
    [System.Serializable]
    public class UIManagerEditor : Editor
    {
        GUISkin customSkin;
        protected static string buildID = "B16-20220221";
        protected static float foldoutItemSpace = 2;
        protected static float foldoutTopSpace = 5;
        protected static float foldoutBottomSpace = 2;

        protected static bool showBackground = false;
        protected static bool showColors = false;
        protected static bool showFonts = false;
        protected static bool showLogo = false;

        void OnEnable()
        {
            if (EditorGUIUtility.isProSkin == true) { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Dark"); }
            else { customSkin = (GUISkin)Resources.Load("Editor\\DUI Skin Light"); }
        }

        public override void OnInspectorGUI()
        {
            if (customSkin == null)
            {
                EditorGUILayout.HelpBox("Editor variables are missing. You can manually fix this by deleting " +
                    "Dark UI > Resources folder and then re-import the package. \n\nIf you're still seeing this " +
                    "dialog even after the re-import, contact me with this ID: " + buildID, MessageType.Error);

                if (GUILayout.Button("Contact")) { Email(); }
                return;
            }

            // Foldout style
            GUIStyle foldoutStyle = customSkin.FindStyle("UIM Foldout");

            // UIM Header
            DarkUIEditorHandler.DrawHeader(customSkin, "UIM Header", 8);
            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Background
            var backgroundType = serializedObject.FindProperty("backgroundType");
            var backgroundImage = serializedObject.FindProperty("backgroundImage");
            var backgroundPreserveAspect = serializedObject.FindProperty("backgroundPreserveAspect");
            var backgroundVideo = serializedObject.FindProperty("backgroundVideo");
            var backgroundSpeed = serializedObject.FindProperty("backgroundSpeed");
            var backgroundColorTint = serializedObject.FindProperty("backgroundColorTint");

            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showBackground = EditorGUILayout.Foldout(showBackground, "Background", true, foldoutStyle);
            showBackground = GUILayout.Toggle(showBackground, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showBackground && backgroundType.enumValueIndex == 0)
            {
                DarkUIEditorHandler.DrawProperty(backgroundImage, customSkin, "Background Image");
                DarkUIEditorHandler.DrawProperty(backgroundColorTint, customSkin, "Color Tint");
            }

            else if (showBackground && backgroundType.enumValueIndex == 1)
            {
                DarkUIEditorHandler.DrawProperty(backgroundType, customSkin, "Background Type");
                DarkUIEditorHandler.DrawProperty(backgroundVideo, customSkin, "Background Video");
                DarkUIEditorHandler.DrawProperty(backgroundColorTint, customSkin, "Color Tint");
                DarkUIEditorHandler.DrawProperty(backgroundSpeed, customSkin, "Animation Speed");
                EditorGUILayout.HelpBox("Video Player will be used for background on Advanced mode.", MessageType.Info);
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Colors       
            var primaryColor = serializedObject.FindProperty("primaryColor");
            var secondaryColor = serializedObject.FindProperty("secondaryColor");
            var primaryReversed = serializedObject.FindProperty("primaryReversed");
            var negativeColor = serializedObject.FindProperty("negativeColor");
            var backgroundColor = serializedObject.FindProperty("backgroundColor");
            var backgroundColorAlt = serializedObject.FindProperty("backgroundColorAlt");

            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showColors = EditorGUILayout.Foldout(showColors, "Colors", true, foldoutStyle);
            showColors = GUILayout.Toggle(showColors, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showColors)
            {
                DarkUIEditorHandler.DrawProperty(primaryColor, customSkin, "Primary Color");
                DarkUIEditorHandler.DrawProperty(primaryReversed, customSkin, "Primary Inverted");
                DarkUIEditorHandler.DrawProperty(secondaryColor, customSkin, "Secondary Color");
                DarkUIEditorHandler.DrawProperty(negativeColor, customSkin, "Negative Color");
                DarkUIEditorHandler.DrawProperty(backgroundColor, customSkin, "Background Color");
                DarkUIEditorHandler.DrawProperty(backgroundColorAlt, customSkin, "Background Alt");
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Fonts
            var lightFont = serializedObject.FindProperty("lightFont");
            var mediumFont = serializedObject.FindProperty("mediumFont");
            var boldFont = serializedObject.FindProperty("boldFont");
            var altFont = serializedObject.FindProperty("altFont");
            var alt2Font = serializedObject.FindProperty("alt2Font");

            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showFonts = EditorGUILayout.Foldout(showFonts, "Fonts", true, foldoutStyle);
            showFonts = GUILayout.Toggle(showFonts, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showFonts)
            {
                DarkUIEditorHandler.DrawProperty(lightFont, customSkin, "Light Font");
                DarkUIEditorHandler.DrawProperty(mediumFont, customSkin, "Medium Font");
                DarkUIEditorHandler.DrawProperty(boldFont, customSkin, "Bold Font");
                DarkUIEditorHandler.DrawProperty(altFont, customSkin, "Alternative Font");
                DarkUIEditorHandler.DrawProperty(alt2Font, customSkin, "Alternative 2 Font");
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Logo
            var brandLogo = serializedObject.FindProperty("brandLogo");
            var gameLogo = serializedObject.FindProperty("gameLogo");
            var logoColor = serializedObject.FindProperty("logoColor");

            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showLogo = EditorGUILayout.Foldout(showLogo, "Logo", true, foldoutStyle);
            showLogo = GUILayout.Toggle(showLogo, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showLogo)
            {
                DarkUIEditorHandler.DrawProperty(brandLogo, customSkin, "Brand Logo");
                DarkUIEditorHandler.DrawProperty(gameLogo, customSkin, "Game Logo");
                DarkUIEditorHandler.DrawProperty(logoColor, customSkin, "Logo Color");
            }

            // Settings
            GUILayout.EndVertical();
            DarkUIEditorHandler.DrawHeader(customSkin, "Options Header", 14);

            var enableDynamicUpdate = serializedObject.FindProperty("enableDynamicUpdate");
            enableDynamicUpdate.boolValue = DarkUIEditorHandler.DrawToggle(enableDynamicUpdate.boolValue, customSkin, "Update Values");

            var enableExtendedColorPicker = serializedObject.FindProperty("enableExtendedColorPicker");
            enableExtendedColorPicker.boolValue = DarkUIEditorHandler.DrawToggle(enableExtendedColorPicker.boolValue, customSkin, "Extended Color Picker");

            if (enableExtendedColorPicker.boolValue == true) { EditorPrefs.SetInt("DarkUIManager.EnableExtendedColorPicker", 1); }
            else { EditorPrefs.SetInt("DarkUIManager.EnableExtendedColorPicker", 0); }

            var editorHints = serializedObject.FindProperty("editorHints");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(-3);
            editorHints.boolValue = DarkUIEditorHandler.DrawTogglePlain(editorHints.boolValue, customSkin, "UI Manager Hints");
            GUILayout.Space(3);

            if (editorHints.boolValue == true)
            {
                EditorGUILayout.HelpBox("These values are universal and affect all objects containing 'UI Manager' component.", MessageType.Info);
                EditorGUILayout.HelpBox("If want to assign unique values, remove 'UI Manager' component from the object ", MessageType.Info);
            }

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            Repaint();

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset to defaults", customSkin.button))
                ResetToDefaults();

            GUILayout.EndHorizontal();

            // Support
            DarkUIEditorHandler.DrawHeader(customSkin, "Support Header", 14);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Need help?", customSkin.FindStyle("Text"));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Contact for support", customSkin.button)) { Email(); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("ID: " + buildID);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        void Email() { Application.OpenURL("https://www.michsky.com/contact"); }

        void ResetToDefaults()
        {
            if (EditorUtility.DisplayDialog("Reset to defaults", "Are you sure you want to reset Dark UI Manager values to default?", "Yes", "Cancel"))
            {
                try
                {
                    Preset defaultPreset = Resources.Load<Preset>("DUIM Presets/Default");
                    defaultPreset.ApplyTo(Resources.Load("Dark UI Manager"));
                    Selection.activeObject = null;
                    Debug.Log("<b>[Dark UI Manager]</b> Resetting successful.");
                }

                catch { Debug.LogWarning("<b>[Dark UI Manager]</b> Resetting failed. Default preset is probably missing."); }
            }
        }
    }
}